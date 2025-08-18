using System;
using System.Timers;
using PSTARV2MonitoringApp.Services;
using Timer = System.Timers.Timer;

namespace PSTARV2MonitoringApp.Models
{
    /// <summary>
    /// 실제 펌웨어 로직을 구현한 PSTAR 펌프 모델
    /// </summary>
    public class PSTPumpModel : IDisposable
    {
        #region 상태 변수 (FW와 동일한 변수명 사용)
        // PSTAR 동작 상태 변수
        public bool RunStatus { get; private set; } = false;       // 0: STOP, 1: RUN
        public bool HeatStatus { get; private set; } = false;      // 0: HEAT_OFF, 1: HEAT_ON
        public bool ModeStatus { get; private set; } = false;      // 0: MANUAL_MODE, 1: STBY_MODE

        // 출력 데이터 (CAN 송신)
        public bool STBY_Start { get; private set; } = false;      // tx_data[0]
        public bool RunLamp { get; private set; } = false;         // tx_data[1]
        public bool Overload { get; private set; } = false;        // tx_data[2]
        public bool RUN_req { get; private set; } = false;         // tx_data[4]
        public bool ResetButton { get; private set; } = false;     // tx_data[5]
        public bool StandByLamp { get; private set; } = false;     // tx_data[6]
        public bool TXLowpress { get; private set; } = false;      // tx_data[7]
        public bool StopLamp { get; private set; } = true;         // 정지 램프 상태

        // 입력 신호
        private bool RunFB_I = false;                              // 실행 피드백 입력
        private bool RunRemote_I = false;                          // 원격 실행 입력
        private bool StopRemote_I = false;                         // 원격 정지 입력
        private bool Overload_I = false;                           // 과부하 입력
        private bool Lowpress_I = false;                           // 저압 입력

        // 플래그 변수
        private bool Request_Flag = false;                         // 실행 요청 플래그
        private bool STBY_Overload = false;                        // STBY 과부하 플래그
        private bool Stop_Overload = false;                        // 정지 과부하 플래그
        private bool txLowpress = false;                           // 내부 저압 상태
        private bool Error_Flag1 = false;                          // ID 1 오류 플래그
        private bool Error_Flag2 = false;                          // ID 2 오류 플래그
        private bool Error_Flag3 = false;                          // ID 3 오류 플래그
        private bool InitFlag = false;                             // 초기화 플래그
        private bool ComStatus_Flag = false;                       // 연결 상태 플래그

        // 타이머 변수
        private int CountBuildUpTime_S = 0;                        // BuildUp 시간 카운터
        private int CountParallelTime_S = 0;                       // Parallel 시간 카운터
        private int CountBuildUpStart = 0;                         // BuildUp 시작 플래그
        private int CountParaStart = 0;                            // Parallel 시작 플래그
        private int BuildUpTime = 5;                               // BuildUp 시간 (초)
        private int ParallelTime = 10;                             // Parallel 시간 (초)

        // 연결 상태
        private int ComStatus = 0;                                 // 0: NoConnection, 1: StandBy_3to2, 2: StandBy_2, 3: StandBy_3
                                                                   // 4: Manual, 5: StandBy_3_1RUN, 6: StandBy_3to2_1RUN

        // 장치 ID와 CAN ID
        private readonly string _deviceId;
        private readonly uint _canId;

        // 타이머
        private readonly Timer _transmitTimer;                     // CAN 전송 타이머
        private readonly Timer _logicTimer;                        // 로직 처리 타이머

        // 수신 데이터 (다른 펌프로부터)
        private byte[] rx_data1 = new byte[8];                     // ID 1 수신 데이터
        private byte[] rx_data2 = new byte[8];                     // ID 2 수신 데이터
        private byte[] rx_data3 = new byte[8];                     // ID 3 수신 데이터
        #endregion

        #region 이벤트
        // CAN 데이터 전송 이벤트
        public event EventHandler<CANTransmitEventArgs> CANDataTransmitted;

        // 상태 변경 이벤트
        public event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;
        #endregion

        /// <summary>
        /// 생성자
        /// </summary>
        public PSTPumpModel(string deviceId)
        {
            _deviceId = deviceId;

            // 장치 ID에 따른 CAN ID 설정
            switch (deviceId)
            {
                case "1": _canId = 0x100; break;
                case "2": _canId = 0x200; break;
                case "3": _canId = 0x300; break;
                default: _canId = 0x100; break;
            }

            // 로직 처리 타이머 (100ms 주기)
            _logicTimer = new Timer(100);
            _logicTimer.Elapsed += OnLogicTimerElapsed;
            _logicTimer.AutoReset = true;

            // CAN 전송 타이머 (300ms 주기 - PSTARFW.c의 CanDelay_mS)
            _transmitTimer = new Timer(300);
            _transmitTimer.Elapsed += OnTransmitTimerElapsed;
            _transmitTimer.AutoReset = true;
        }

        /// <summary>
        /// 시뮬레이션 시작
        /// </summary>
        public void StartSimulation()
        {
            _logicTimer.Start();
            _transmitTimer.Start();
        }

        /// <summary>
        /// 시뮬레이션 중지
        /// </summary>
        public void StopSimulation()
        {
            _logicTimer.Stop();
            _transmitTimer.Stop();
        }

        /// <summary>
        /// 로직 타이머 이벤트 - 펌웨어의 메인 루프 역할
        /// </summary>
        private void OnLogicTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // PSTARFW.c의 메인 루프 로직 구현
            UpdatePressureStatus();
            ProcessInputs();
            RunStopProc(RunStatus);
            RunInput();
            HeatProc(HeatStatus);
            ModeProc(ModeStatus);
            OverloadProc(Overload_I);
            LowpressProc(Lowpress_I);
            ComFailErrorFlag();
            ReceiveRunReq();
            SendRunReq();
            ConnectFunction();
            StandByLampProc(ModeStatus);
            StbyStartAlarm();

            // 상태 변경 알림
            NotifyStateChanged();
        }

        /// <summary>
        /// CAN 데이터 전송 타이머 이벤트
        /// </summary>
        private void OnTransmitTimerElapsed(object sender, ElapsedEventArgs e)
        {
            TransmitCANData();
        }

        /// <summary>
        /// CAN 데이터 수신 처리
        /// </summary>
        public void ProcessReceivedCANFrame(CANFrame frame)
        {
            // CAN ID에 따라 적절한 수신 버퍼에 저장
            if (frame.Id == 0x100 && _canId != 0x100)
            {
                Array.Copy(frame.Data, rx_data1, Math.Min(frame.Data.Length, 8));
                Error_Flag1 = false;  // COM Fault 카운터 리셋
            }
            else if (frame.Id == 0x200 && _canId != 0x200)
            {
                Array.Copy(frame.Data, rx_data2, Math.Min(frame.Data.Length, 8));
                Error_Flag2 = false;  // COM Fault 카운터 리셋
            }
            else if (frame.Id == 0x300 && _canId != 0x300)
            {
                Array.Copy(frame.Data, rx_data3, Math.Min(frame.Data.Length, 8));
                Error_Flag3 = false;  // COM Fault 카운터 리셋
            }
        }

        /// <summary>
        /// CAN 데이터 전송 (tx_data)
        /// 타이머에 의해 주기적으로 OnTransmitTimerElapsed가 호출되고
        /// TransmitCANData가 호출되면 CANDataTransmitted 이벤트 핸들러가 호출되고, TestViewModel의 OnPumpCANDataTransmitted가 호출된다.
        /// </summary>
        private void TransmitCANData()
        {
            // PSTARFW.c의 tx_data 배열과 동일하게 구성
            byte[] data = new byte[8];
            data[0] = (byte)(STBY_Start ? 1 : 0);
            data[1] = (byte)(RunLamp ? 1 : 0);
            data[2] = (byte)(Overload ? 1 : 0);
            data[3] = (byte)(ModeStatus ? 1 : 0);  // 0: MANUAL, 1: STBY
            data[4] = (byte)(RUN_req ? 1 : 0);
            data[5] = (byte)(ResetButton ? 1 : 0);
            data[6] = (byte)(StandByLamp ? 1 : 0);
            data[7] = (byte)(TXLowpress ? 1 : 0);

            // CAN 프레임 생성
            // 250818TODO 전송할 CAN 프레임 형식 정의하고 CANFrame.cs 코드 정리
            var frame = new CANFrame
            {
                Id = _canId,
                Data = data,
                Timestamp = DateTime.Now
            };

            // 이벤트 발생
            CANDataTransmitted?.Invoke(this, new CANTransmitEventArgs(frame));
        }

        #region PSTARFW.c 함수 구현

        /// <summary>
        /// 압력 스위치 상태 업데이트 (Pressure Switch 1EA or 2EA)
        /// </summary>
        private void UpdatePressureStatus()
        {
            // 펌웨어 main() 함수 상단부의 로직
            TXLowpress = txLowpress;
        }

        /// <summary>
        /// 입력 처리 (KeyProc, OverloadProc, LowpressProc 함수 로직)
        /// </summary>
        private void ProcessInputs()
        {
            // KeyProc, OverloadProc, LowpressProc 등 입력 처리 로직 구현
        }

        /// <summary>
        /// 실행/정지 상태 처리
        /// </summary>
        private void RunStopProc(bool runStatus)
        {
            if (runStatus) // RUN
            {
                ResetButton = false;
                StopLamp = false;
            }
            else // STOP
            {
                StopLamp = true;
                RunLamp = false;
            }
        }

        /// <summary>
        /// 실행 입력 처리
        /// </summary>
        private void RunInput()
        {
            if (RunFB_I) // Run Signal ON & Run Input ON -> Run Lamp ON
            {
                RunLamp = true;
                StopLamp = false;
            }
        }

        /// <summary>
        /// 히팅 처리
        /// </summary>
        private void HeatProc(bool heatStatus)
        {
            // HeatProc 로직 구현
        }

        /// <summary>
        /// 모드 처리
        /// </summary>
        private void ModeProc(bool modeStatus)
        {
            // ModeProc 로직 구현
        }

        /// <summary>
        /// 과부하 처리
        /// </summary>
        private void OverloadProc(bool overloadStatus)
        {
            if (overloadStatus)
            {
                if (RunStatus)
                {
                    Stop_Overload = true;
                    RunStatus = false;
                }

                Overload = true;
                CountParallelTime_S = 0;

                ResetButton = false;

                if (StandByLamp)
                {
                    StandByLamp = false; // Occur Overload -> StandBy x
                    STBY_Overload = true; // STBY Overload -> Run Request x
                }
            }
            else
            {
                Overload = false;

                STBY_Overload = false;
                Stop_Overload = false;
            }
        }

        /// <summary>
        /// 저압 처리
        /// </summary>
        private void LowpressProc(bool lowpressStatus)
        {
            // LowpressProc 로직 구현
        }

        /// <summary>
        /// 통신 오류 플래그 처리
        /// </summary>
        private void ComFailErrorFlag()
        {
            // ComFailErrorFlag 로직 구현
        }

        /// <summary>
        /// Run 요청 수신 처리 (StandBy 펌프)
        /// </summary>
        private void ReceiveRunReq()
        {
            if (!Overload && StandByLamp)
            {
                if (!Request_Flag && (rx_data1[4] == 1 || rx_data2[4] == 1 || rx_data3[4] == 1))
                {
                    Request_Flag = true;
                    RunStatus = true;
                }
                else if (rx_data1[4] == 0 && rx_data2[4] == 0 && rx_data3[4] == 0)
                {
                    Request_Flag = false;
                }
            }
        }

        /// <summary>
        /// Run 요청 전송 처리 (메인 펌프)
        /// </summary>
        private void SendRunReq()
        {
            // SendRunReq 로직 구현
        }

        /// <summary>
        /// StandBy 시작 알람 처리
        /// </summary>
        private void StbyStartAlarm()
        {
            if (RunLamp && StandByLamp)
            {
                STBY_Start = true;
            }
            else
            {
                STBY_Start = false;
            }
        }

        /// <summary>
        /// 연결 상태 함수
        /// </summary>
        private void ConnectFunction()
        {
            // ConnectFunction 로직 구현
        }

        /// <summary>
        /// StandBy 램프 처리
        /// </summary>
        private void StandByLampProc(bool modeStatus)
        {
            // StandByLampProc 로직 구현 (매우 복잡한 로직)
        }
        #endregion

        #region 외부 인터페이스 메서드
        /// <summary>
        /// 시작 버튼 누름 처리
        /// </summary>
        public void PressStartButton()
        {
            if (!Overload)
            {
                if (!RunStatus)
                {
                    if (!ModeStatus) // MANUAL_MODE
                    {
                        RunStatus = true;
                    }
                }
            }
        }

        /// <summary>
        /// 정지 버튼 누름 처리
        /// </summary>
        public void PressStopButton()
        {
            RunStatus = false;
            ResetButton = true;
        }

        /// <summary>
        /// 모드 버튼 누름 처리
        /// </summary>
        public void PressModeButton()
        {
            ModeStatus = !ModeStatus;
        }

        /// <summary>
        /// 히트 버튼 누름 처리
        /// </summary>
        public void PressHeatButton()
        {
            HeatStatus = !HeatStatus;
        }

        /// <summary>
        /// 과부하 상태 설정
        /// </summary>
        public void SetOverload(bool isOverload)
        {
            Overload_I = isOverload;
        }

        /// <summary>
        /// 저압 상태 설정
        /// </summary>
        public void SetLowPressure(bool isLowPressure)
        {
            Lowpress_I = isLowPressure;
        }

        /// <summary>
        /// 상태 변경 알림
        /// </summary>
        private void NotifyStateChanged()
        {
            DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs
            {
                DeviceId = _deviceId,
                IsRunning = RunStatus,
                IsStandByMode = ModeStatus,
                IsHeating = HeatStatus,
                IsStandByLamp = StandByLamp
            });
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            _logicTimer?.Stop();
            _transmitTimer?.Stop();
            _logicTimer?.Dispose();
            _transmitTimer?.Dispose();
        }
        #endregion
    }

    /// <summary>
    /// 장치 상태 변경 이벤트 인수
    /// </summary>
    public class DeviceStateChangedEventArgs : EventArgs
    {
        public string DeviceId { get; set; }
        public bool IsRunning { get; set; }
        public bool IsStandByMode { get; set; }
        public bool IsHeating { get; set; }
        public bool IsStandByLamp { get; set; }
    }
}