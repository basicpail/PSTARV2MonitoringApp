using System;
using System.Timers;
using PSTARV2MonitoringApp.Models;
using Timer = System.Timers.Timer;

namespace PSTARV2MonitoringApp.Services
{
    /// <summary>
    /// 실제 장치 로직을 구현
    /// </summary>
    public class PSTARDeviceService : IDisposable
    {
        #region 변수
        // 장치 ID와 CAN ID, interval
        private readonly string _deviceId;
        private readonly uint _canId;
        private readonly uint CAN_ID;
        private readonly int _canTransmitInterval = 1000; // CAN 전송 주기 (ms)

        // CAN 전송 타이머 (로직 타이머는 제거)
        private readonly Timer _transmitTimer;

        // PSTAR 장치 타이머
        private readonly Timer _deviceTimer;

        // 모델 레퍼런스 (장치 모델 공유)
        private PSTARDeviceModel _model;

        // PSTARFW 변수 필요한 것만 추가
        private int _buildUpTime = 5;   // BuildUp 시간 (초)
        private int _parallelTime = 10; // Parallel 시간 (초)
        private int _seqTime_mS = 10;   // Sequential Time (초)
        private bool _requestFlag = false;      // 실행 요청 플래그
        #endregion

        #region 이벤트
        // CAN 데이터 전송 이벤트
        public event EventHandler<CANTransmitEventArgs> CANDataTransmitted;

        // 상태 변경 이벤트
        public event EventHandler<DeviceStateChangedEventArgs> DeviceStateChanged;
        #endregion



        #region Index
        /// <summary>
        /// CAN 데이터 인덱스 정의
        /// </summary>
        public static class CANDataIndices
        {
            // CAN 데이터 인덱스 정의
            public const int STBY_START = 0;     // Standby 시작 (tx_data[0])
            public const int RUN_LAMP = 1;       // 실행 램프 상태 (tx_data[1])
            public const int OVERLOAD = 2;       // 과부하 상태 (tx_data[2])
            public const int MODE_STATUS = 3;    // 모드 상태 (tx_data[3]) - 0: MANUAL, 1: STBY
            public const int RUN_REQ = 4;        // 실행 요청 (tx_data[4])
            public const int RESET_BUTTON = 5;   // 리셋 버튼 상태 (tx_data[5])
            public const int STANDBY_LAMP = 6;   // Standby 램프 상태 (tx_data[6])
            public const int TX_LOWPRESS = 7;    // 저압 상태 (tx_data[7])
        }

        public static class COMStatusIndices
        {
            public const int NoConnection = 0;
            public const int StandBy_3to2 = 1;
            public const int StandBy_2 = 2;
            public const int StandBy_3 = 3;
            public const int Manual = 4;
            public const int StandBy_3_1RUN = 5;
            public const int StandBy_3to2_1RUN = 6;
        }
        #endregion

        /// <summary>
        /// 생성자
        /// </summary>
        public PSTARDeviceService(string deviceId)
        {
            _deviceId = deviceId;

            // 장치 ID에 따른 CAN ID 설정
            switch (deviceId)
            {
                case "1": _canId = 0x100; CAN_ID = 1; break;
                case "2": _canId = 0x200; CAN_ID = 2; break;
                case "3": _canId = 0x300; CAN_ID = 3; break;
                default: _canId = 0x100; CAN_ID = 1; break;
            }

            // CAN 전송 타이머
            //_transmitTimer = new Timer(_model.CANTransmitInterval); // 모델이 설정 후 사용, 
            _transmitTimer = new Timer(_canTransmitInterval);
            _transmitTimer.Elapsed += OnTransmitTimerElapsed;
            _transmitTimer.AutoReset = true;

            //PSTAR 타이머
            //이상상황 발생 부분에서 함수연결하고 인터벌 설정
            _deviceTimer = new Timer();


        }

        /// <summary>
        /// 장치 모델 설정
        /// </summary>
        public void SetModel(PSTARDeviceModel model)
        {
            // 기존 모델 이벤트 구독 해제
            if (_model != null)
            {
                _model.StateChanged -= OnModelStateChanged;
            }

            // 새 모델 설정
            _model = model;

            // 새 모델이 있으면 이벤트 구독
            if (_model != null)
            {
                _model.StateChanged += OnModelStateChanged;
            }
        }

        /// <summary>
        /// 모델 상태 변경 이벤트 처리
        /// </summary>
        private void OnModelStateChanged(object sender, EventArgs e)
        {
            // 모델 상태가 변경되면 로직 실행
            ExecutePSTARLogic();
            // 상태 변경 알림
            NotifyStateChanged();
        }

        /// <summary>
        /// 시뮬레이션 시작
        /// </summary>
        public void StartSimulation()
        {
            _transmitTimer.Start();

            // 초기 로직 실행
            ExecutePSTARLogic();
        }

        /// <summary>
        /// 시뮬레이션 중지
        /// </summary>
        public void StopSimulation()
        {
            _transmitTimer.Stop();
        }

        /// <summary>
        /// 로직 실행 (이전 OnLogicTimerElapsed의 내용)
        /// </summary>
        public void ExecutePSTARLogic()
        {
            if (_model == null) return;

            // PSTARFW.c의 메인 루프 로직 구현
            HandlePowerRecovery();
            UpdatePressureStatus();
            ProcessInputs();
            RunStopProc(_model.RunStatus);
            RunInput();
            HeatProc(_model.HeatStatus);
            ModeProc(_model.ModeStatus);
            KeyProc();
            OverloadProc(_model.Overload_I);
            LowpressProc(_model.Lowpress_I);
            ComFailErrorFlag();
            ReceiveRunReq();
            SendRunReq();
            ConnectFunction();
            StandByLampProc(_model.ModeStatus);
            StbyStartAlarm();
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
            if (_model == null) return;

            // 모델에 프레임 처리 위임
            _model.ProcessReceivedCANFrame(frame);
        }

        /// <summary>
        /// CAN 데이터 전송
        /// </summary>
        private void TransmitCANData()
        {
            //Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} [{_deviceId}] TransmitCANData 호출됨");

            if (_model == null) return;

            // 모델에서 데이터 읽어서 CAN 프레임 구성
            byte[] data = new byte[8];
            data[CANDataIndices.STBY_START] = (byte)(_model.STBY_Start ? 1 : 0);
            data[CANDataIndices.RUN_LAMP] = (byte)(_model.RUN_LAMP ? 1 : 0);
            data[CANDataIndices.OVERLOAD] = (byte)(_model.Overload ? 1 : 0);
            data[CANDataIndices.MODE_STATUS] = (byte)(_model.ModeStatus ? 1 : 0);  // 0: MANUAL, 1: STBY
            data[CANDataIndices.RUN_REQ] = (byte)(_model.RUN_req ? 1 : 0);
            data[CANDataIndices.RESET_BUTTON] = (byte)(_model.ResetButton ? 1 : 0);
            data[CANDataIndices.STANDBY_LAMP] = (byte)(_model.STAND_BY_LAMP ? 1 : 0);
            data[CANDataIndices.TX_LOWPRESS] = (byte)(_model.TXLowpress ? 1 : 0);

            // CAN 프레임 생성
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

        // SeqTime_mS 변수 추가

        /// <summary>
        /// 전원 복구 로직 (POWER RECOVERY AFTER POWER FAIL)
        /// </summary>
        private void HandlePowerRecovery()
        {
            if (_model == null) return;

            if (_model.InitFlag)
            {
                if (_seqTime_mS < 630) // 0~62s : UVR
                {
                    // Push the Reset Button -> STANDBY LAMP ON (Change MAIN Pump : MAIN -> STBY)
                    if (_model.ModeStatus &&
                        ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                         (_model.RX_Data2[1] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                         (_model.RX_Data3[1] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1)))
                    {
                        _model.RunStatus = false;
                        // EEPROM 저장 부분은 C#에서는 제외
                        _model.InitFlag = false;
                    }
                    // After Power Fail (3 ST'BY(2 RUN) - MAIN 1 & MAIN2)
                    else if ((_deviceId == "1" && _model.RX_Data2[1] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data3[1] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1) ||
                             (_deviceId == "2" && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data3[1] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1) ||
                             (_deviceId == "3" && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data2[1] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1))
                    {
                        _model.RunStatus = false;
                        // EEPROM 저장 부분은 C#에서는 제외
                        _model.InitFlag = false;
                    }
                    // After Power Fail (3 ST'BY(1 RUN) - ST'BY)
                    else if (_model.ComStatus == 5 && // StandBy_3_1RUN
                            ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                             (_model.RX_Data2[1] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                             (_model.RX_Data3[1] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1)))
                    {
                        _model.RunStatus = false;
                        // EEPROM 저장 부분은 C#에서는 제외
                        _model.InitFlag = false;
                    }
                    // Sequential Time (MANUAL RUN / 2 STAND BY RUN & RUN / Power Recovery Before 1s (MAIN -> MAIN))
                    else
                    {
                        // 정지 램프 및 신호 끄기
                        _model.STOP_LAMP = false;

                        // CountSeqTime_mS가 SeqTime_mS에 도달하면 RUN 상태로 변경
                        if (_model.CountSeqTime_S * 1000 >= _seqTime_mS) // CountSeqTime_S는 초 단위, _seqTime_mS는 밀리초 단위
                        {
                            _model.RunStatus = true;
                            _model.InitFlag = false;
                            _model.CountSeqTime_S = 0;
                        }
                    }
                }
                else if (_seqTime_mS == 630) // 63s : UVP
                {
                    _model.RunStatus = false;
                    // EEPROM 저장 부분은 C#에서는 제외
                    _model.InitFlag = false;
                }
            }
            else
            {
                _model.CountSeqTime_S = 0;
            }

            // 다른 장치의 과부하 상태 확인
            if (_model.RX_Data1[CANDataIndices.OVERLOAD] == 1 || _model.RX_Data2[CANDataIndices.OVERLOAD] == 1 || _model.RX_Data3[CANDataIndices.OVERLOAD] == 1)
            {
                _model.CountOverload_Flag = true;
            }
            else
            {
                _model.CountOverload_Flag = false;
            }
        }

        /// <summary>
        /// 압력 스위치 상태 업데이트 (Pressure Switch 1EA or 2EA)
        /// </summary>
        private void UpdatePressureStatus()
        {
            if (_model == null) return;

            // 펌웨어 main() 함수 상단부의 로직
            //_model.TXLowpress = _model.TxLowpressInternal;
        }

        /// <summary>
        /// 입력 처리 (KeyProc, OverloadProc, LowpressProc 함수 로직)
        /// </summary>
        private void ProcessInputs()
        {
            if (_model == null) return;

            // 펌웨어에서는 이 부분이 디지털 입력 처리를 담당하지만
            // 여기서는 입력은 UI나 명령에 의해서 제어되므로 단순 상태 체크만 함

            // 과부하 입력 신호가 있을 때 과부하 처리
            if (_model.Overload_I)
            {
                OverloadProc(true);
            }

            // 저압 입력 신호가 있을 때 저압 처리
            if (_model.Lowpress_I)
            {
                LowpressProc(true);
            }
        }

        private void RunStopCont(bool command) //command는 RunEE (0 은 STOP)
        {
            if (_model == null) return;

            if (command) // RUN
            {
            }
            else // STOP
            {
                _model.STOP_LAMP = true;

            }
        }

        /// <summary>
        /// 실행/정지 상태 처리
        /// </summary>
        private void RunStopProc(bool runStatus)
        {
            if (_model == null) return;

            if (runStatus) // RUN
            {
                if(_model.FirstRunStatus == false) _model.FirstRunStatus = true; //한 번이라도 RUN을 경험한 이후에만 STOP 펄스를 보내기 위함. 전원 투입 직후 이미 STOP 상태일 때 불필요한 STOP 펄스가 외부(인버터/PLC)에 나가 오동작/불필요 로그를 만드는 걸 막는 목적

                _model.ResetButton = false;
                _model.STOP_LAMP = false;
            }
            else // STOP
            {
                _model.STOP_LAMP = true;
                _model.RUN_LAMP = false;

                // 정지 시 첫 실행 상태 초기화
                //STOP_SIG를 그냥 계속 ON으로 두지 않고, 정해진 시간(StopPulse) 동안만 ON 이후 자동으로 OFF 하여 “한 번의 정지 트리거”만 전달
                //if (FirstRunStatus == 1 && CountStopPulse_mS < StopPulse) LampSigCont(STOP_SIG, ON);
                //else if (CountStopPulse_mS >= StopPulse)
                //{
                //    CountStopPulse_mS = StopPulse;
                //    LampSigCont(STOP_SIG, OFF);
                //}
            }
        }

        /// <summary>
        /// 실행 입력 처리
        /// </summary>
        private void RunInput()
        {
            if (_model == null) return;

            if (_model.START_PB_I) // Run Signal ON & Run Input ON -> Run Lamp ON
            {
                _model.RUN_LAMP = true;
                _model.STOP_LAMP = false;
            }
        }

        /// <summary>
        /// Heat_EE 값 처리
        /// command는 HeatEE (0 은 HEAT OFF)
        /// </summary>
        private void HeatCont(bool command)
        {
            if (_model == null) return;

            if (command == false) // HEAT OFF
            {
                _model.HEAT_ON_LAMP = false;
                _model.HEATING_LAMP = false; //HEAT는 SIG 처리 해줘야한다.
                _model.CountHeatingOnTime_S = 0;
            }
            else // HEAT ON
            {
                _model.HEAT_ON_LAMP = true;
            }
        }

        /// <summary>
        /// 히팅 처리
        /// </summary>
        private void HeatProc(bool heatStatus)
        {
            if (_model == null) return;

            if (heatStatus == false) // HEAT OFF
            {
                _model.HEAT_ON_LAMP = false;
                _model.HEATING_LAMP = false;
                _model.CountHeatingOnTime_S = 0;
            }
            else // HEAT ON
            {
                _model.HEAT_ON_LAMP = true;

                if (_model.RunStatus == false) // STOP
                {
                    if (_model.CountHeatingOnTime_S >= _model.HeatingOnTime)
                    {
                        _model.HEATING_LAMP = true;
                    }
                }
                else // RUN 상태에서는 가열 신호 비활성화
                {
                    _model.HEATING_LAMP = false;
                    _model.CountHeatingOnTime_S = 0;
                }
            }
        }

        /// <summary>
        /// 모드 처리
        /// </summary>
        private void ModeProc(bool modeStatus)
        {
            if(_model == null) return;

            if(modeStatus == false) // MANUAL_MODE
            {
                _model.MODE_MANUAL_LAMP = true;
                _model.MODE_STBY_LAMP = false;
            }
            else // STBY_MODE
            {
                _model.MODE_STBY_LAMP = true;
                _model.MODE_MANUAL_LAMP = false;
            }
        }

        /// <summary>
        /// 버튼 상태 처리
        /// </summary>
        private void KeyProc()
        {
            if(_model == null) return;

            //-------------------------RUN/STOP BUTTON-------------------------
            if (_model.Overload_I == false)
            {
                //-------------------------RUN-------------------------
                if (_model.RunStatus == false)
                {
                    //한 번만 동작 하게 만드는 래치(arm/disarm) 방식, 길게 누르고 있어도 반복 트리거가 안 됨
                    if (_model.OldStartPB == true && _model.START_PB_I == true)
                    {
                        _model.OldStartPB = false;
                        _model.RunStatus = true;
                    }
                    else if(_model.OldStartPB == false && _model.START_PB_I == false)
                    {
                        _model.OldStartPB = true;
                    }

                    if(_model.RunRemote_I == true) _model.RunStatus = true;
                }
                //-------------------------STOP-------------------------
                if(_model.OldStopPB == true && _model.STOP_PB_I == true)
                {
                    _model.OldStopPB = false;
                    _model.RunStatus = false;
                }
                else if(_model.OldStopPB == false && _model.STOP_PB_I == false)
                {
                    if (_model.RX_Data1[CANDataIndices.STBY_START] == 1 || _model.RX_Data2[CANDataIndices.STBY_START] == 1 || _model.RX_Data3[CANDataIndices.STBY_START] == 1)
                    {
                        _model.ResetButton = true; // StandBy Start 알람 발생 시 Reset 버튼 활성화
                    }
                    else
                    {
                        _model.ResetButton = false;
                    }
                    _model.OldStopPB = true;
                }
                if(_model.StopRemote_I == true) _model.RunStatus = false;
            }

            //-------------------------HEAT BUTTON-------------------------
            if(_model.OldHeatPB == true && _model.HEAT_PB_I == true)
            {
                _model.OldHeatPB = false;

                if(_model.HeatStatus == false) _model.HeatStatus = true;
                else if(_model.HeatStatus == true) _model.HeatStatus = false;
            }
            else if(_model.OldHeatPB == false && _model.MODE_PB_I == false)
            {
                _model.OldHeatPB = true;
            }
            //-------------------------MODE BUTTON-------------------------
            if(_model.OldModePB == true && _model.MODE_PB_I == true)
            {
                _model.OldModePB = false;

                if(_model.ModeStatus == false) _model.ModeStatus = true; // MANUAL -> STBY
                else if(_model.ModeStatus == true) _model.ModeStatus = false; // STBY -> MANUAL
            }
            else if(_model.OldModePB == false && _model.MODE_PB_I == false)
            {
                _model.OldModePB = true;
            }
        }

        /// <summary>
        /// 시작 버튼 누름 처리
        /// </summary>
        public void PressStartButton()
        {
            if (_model == null) return;

            _model.START_PB_I = true; // Start 버튼 누름 신호
            // 상태 변경 시 로직 실행
            ExecutePSTARLogic();
            _model.START_PB_I = false; // Start 버튼 누름 신호

        }

        /// <summary>
        /// 정지 버튼 누름 처리
        /// </summary>
        public void PressStopButton()
        {
            if (_model == null) return;

            _model.STOP_PB_I = true; // Stop 버튼 누름 신호
            // 상태 변경 시 로직 실행
            ExecutePSTARLogic();
            _model.STOP_PB_I = false; // Stop 버튼 누름 신호

        }

        /// <summary>
        /// 모드 버튼 누름 처리
        /// </summary>
        public void PressModeButton()
        {
            if (_model == null) return;

            _model.MODE_PB_I = true; // Mode 버튼 누름 신호
            // 상태 변경 시 로직 실행
            ExecutePSTARLogic();
            _model.MODE_PB_I = false; // Mode 버튼 누름 신호

        }

        /// <summary>
        /// 히트 버튼 누름 처리
        /// </summary>
        public void PressHeatButton()
        {
            if (_model == null) return;

            _model.HEAT_PB_I = true; // Heat 버튼 누름 신호
            // 상태 변경 시 로직 실행
            ExecutePSTARLogic();
            _model.HEAT_PB_I = false; // Heat 버튼 누름 신호
        }

        /// <summary>
        /// 과부하 처리
        /// </summary>
        private void OverloadProc(bool Overload_I)
        {
            if (_model == null) return;

            if (Overload_I == true)
            {
                if (_model.RunStatus) //RUN 상태에서 과부하 발생 시 정지
                {
                    _model.Stop_Overload = true;
                    _model.RunStatus = false;
                }
                _model.ABN_LAMP = true;
                _model.Overload = true;
                _model.CountParallelTime_S = 0;

                _model.ResetButton = false;

                if (_model.STAND_BY_LAMP)
                {
                    _model.STAND_BY_LAMP = false; // Occur Overload -> StandBy x
                    _model.STBY_Overload = true; // STBY Overload -> Run Request x
                }
            }
            else
            {
                _model.ABN_LAMP = false;
                _model.Overload = false;
                _model.CountRunReq_S = 0;

                _model.STBY_Overload = false;
                _model.Stop_Overload = false; //원본 코드에도 false로 할당
            }
        }

        /// <summary>
        /// 저압 처리
        /// </summary>
        private void LowpressProc(bool Lowpress_I)
        {
            if (_model == null) return;

            if (Lowpress_I)
            {
                _model.LOW_PRESS_LAMP = true;
                _model.Lowpress = true;

                _model.TXLowpress = true; // 저압 신호
            }
            else if (_model.ComStatus == COMStatusIndices.StandBy_2 &&
                (_model.RX_Data1[CANDataIndices.TX_LOWPRESS] == 1 || _model.RX_Data2[CANDataIndices.TX_LOWPRESS] == 1 || _model.RX_Data3[CANDataIndices.TX_LOWPRESS] == 1))
                // 2대가 StandBy일 때 다른 장치에서 저압 신호 수신 시
            {
                _model.LOW_PRESS_LAMP = true;
                _model.Lowpress = true;
                _model.TXLowpress = true;
            }
            else if (Lowpress_I == false || 
                (_model.ComStatus == COMStatusIndices.StandBy_2 && 
                (_model.RX_Data1[CANDataIndices.TX_LOWPRESS] == 0 || _model.RX_Data2[CANDataIndices.TX_LOWPRESS] == 0 || _model.RX_Data3[CANDataIndices.TX_LOWPRESS] == 0 )))
                // 저압 신호가 없고 2대가 StandBy일 때 다른 장치에서 저압 신호가 없을 때
            {
                _model.LOW_PRESS_LAMP = false;
                _model.Lowpress = false;
                _model.TXLowpress = false;

                _model.CountBuildUpTime_S = 0;
                _model.CountBuildUpStart = false;
            }
            //--------------------------BUILD UP TIME SETTING--------------------------
            // RUN -> LowPressure
            if(_model.OldRunStatus == true && _model.RunStatus == true) //RUN 상태에서 저압 발생 시 (기동 단계와 정상 운전 단계를 구분하기 위한 것.)
            {
                if(_model.OldLowpress == false && _model.Lowpress == true) //저압 신호가 처음 들어왔을 때
                {
                    _model.CountBuildUpTime = 3; // 3초 (RUN -> LowPressure)
                }
            }
            // STOP -> LowPressure -> RUN
            if(_model.OldLowpress == true && _model.Lowpress == true) //저압 상태에서
            {
                if(_model.OldRunStatus == false && _model.RunStatus == true) //STOP -> RUN 상태로 변경 시
                {
                    if(_model.STAND_BY_LAMP == false) //StandBy가 아닐 때
                    {
                        _model.CountBuildUpTime = _model.BuildUpTime; // 설정값
                    }
                    else if(_model.STAND_BY_LAMP == true) //StandBy 모드일 때 (이때는 왜 3초 지?)
                    {
                        _model.CountBuildUpTime = 3; // 3초
                    }
                }
            }
            if(_model.Lowpress == true && _model.RUN_LAMP == true && //저압 상태에서 RUN 램프가 켜져 있고
                ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) || //다른 장치들이 StandBy 상태일 때
                 (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                 (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1)))
            {
                _model.CountBuildUpStart = true; //BuildUp 카운트 시작
            }
            else
            {
                _model.CountBuildUpStart = false;
            }
            //--------------------------PARALLEL TIME COUNT START (AFTER BUILD UP TIME OVER)--------------------------
            if(_model.RUN_LAMP == true && _model.STAND_BY_LAMP == true && _model.Lowpress == true) //RUN & StandBy & 저압 상태에서
                //내가 스탠바이 펌프인 상황, build up 후에도 저압 상태이면 스탠바이 펌프가 함께 기동해서 병령운전 시작.
            {
                if(_model.CountBuildUpTime_S >= _model.CountBuildUpTime) //BuildUp 시간이 지나면
                {
                    _model.CountParaStart = false; //???나는 스탠바이 펌프니까 평행운전 타이머를 끈다. 스탠바이 펌프가 병렬 카운트를 해버리면 메인과 함께 스탠바이도 펌프도 같이 멈춰 버리는 문제 생김
                    _model.CountParallelTime_S = 0;
                    _model.CountBuildUpStart = false; //BuildUp 카운트 중지
                    _model.CountBuildUpTime_S = 0; //BuildUp 시간 초기화
                }
            }
            else if(_model.Lowpress == true && //저압 상태, 내가 메인 펌프인 상황
                ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) || //다른 장치들이 StandBy 상태일 때
                 (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                 (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1)))
            {
                if(_model.CountBuildUpTime_S >= _model.CountBuildUpTime) //BuildUp 시간이 지나면
                {
                    _model.CountParaStart = true; //Parallel 시간 카운트 시작
                    _model.CountBuildUpStart = false; //BuildUp 카운트 중지
                    _model.CountBuildUpTime_S = 0; //BuildUp 시간 초기화
                }
            }
            //--------------------------PARALLEL TIME COUNT OVER--------------------------
            if(_model.CountParaStart == true) //Parallel 시간 카운트 시작 상태에서
            {
                if(_model.CountParallelTime_S >= _model.ParallelTime) //Parallel 시간이 지나면
                {
                    _model.RunStatus = false; //모터 정지
                    _model.CountParaStart = false; //Parallel 시간 카운트 중지
                    _model.CountParallelTime_S = 0; //Parallel 시간 초기화
                }
            }

            _model.OldLowpress = _model.Lowpress;
            _model.OldRunStatus = _model.RunStatus;
        }

        /// <summary>
        /// 통신 오류 플래그 처리
        /// </summary>
        private void ComFailErrorFlag()
        {
            if (_model == null) return;

            //--------------------------COM FAIL - CAN ID 1--------------------------
            //Error_Flag : False면 Com_Normal, True면 Com_Error
            if (_model.CountComFault1_S > _model.ComFault_S && _model.Error_Flag1 == false && CAN_ID != 1)
            //ID1로부터 일정 시간(현재 1초) 이상 프레임이 안 옴을 뜻한다. 자기 자신에 대한 오류는 세지 않음
            {
                _model.Error_Flag1 = true; //Com_Error

                if(_model.STAND_BY_LAMP == true && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1)
                //StandBy 모드에서 통신 오류 발생 시 자동 전환
                //내가 스탠바이 역할이고 마지막으로 본 ID1이 RUN 상태였는데, ID1과 통신이 끊겼다 -> ID1이 고장났다고 보고 내가 RUN으로 자동 전환
                {
                    _model.RunStatus = true; //auto change to RUN
                }

                _model.RX_Data1 = new byte[8]; //수신 데이터 초기화. 직전 수신 값들을 0으로 모두 초기화.
            }
            else if (_model.CountComInit == _model.ComInit_S || _model.CountComInit > _model.ComInit_S) //? 이상한 조건이다
                //초기화 구간(ComInit_S초)이 지나가기 전에는 에러 해제를 하지 않도록 하는 해제 지연.
            {
                //다시 정상적으로 수신되고 있다고 판단하고 Error_Flag를 False로 바꿈
                if (_model.CountComFault1_S <= _model.ComFault_S && _model.Error_Flag1 == true && CAN_ID != 1) _model.Error_Flag1 = false; //Com_Normal
            }

            //ID1이 MANUAL 모드(0) 라면, 스탠바이/병행 전환이 불가능하다고 보고 사실상 연결 불가(Com_Error)로 취급
            if (_model.RX_Data1[CANDataIndices.MODE_STATUS] == 0 && CAN_ID != 1) _model.Error_Flag1 = true; //MANUAL_MODE -> Com_Error
            //ID1이 STBY 모드(1) 라면 전환 상태이므로 Com_Normal로 설정
            else if (_model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && CAN_ID != 1) _model.Error_Flag1 = false; //STBY_MODE -> Com_Normal

            //--------------------------COM FAIL - CAN ID 2--------------------------
            if (_model.CountComFault2_S > _model.ComFault_S && _model.Error_Flag2 == false && CAN_ID != 2)
            {
                _model.Error_Flag2 = true; //Com_Error

                if (_model.STAND_BY_LAMP == true && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.RunStatus = true; //auto change to RUN
                }

                _model.RX_Data2 = new byte[8]; //수신 데이터 초기화. 직전 수신 값들을 0으로 모두 초기화.
            }
            else if (_model.CountComInit == _model.ComInit_S || _model.CountComInit > _model.ComInit_S) 
            {
                if (_model.CountComFault2_S <= _model.ComFault_S && _model.Error_Flag2 == true && CAN_ID != 2) _model.Error_Flag2 = false; //Com_Normal
            }
            if (_model.RX_Data2[CANDataIndices.MODE_STATUS] == 0 && CAN_ID != 2) _model.Error_Flag2 = true; //MANUAL_MODE -> Com_Error
            //ID1이 STBY 모드(1) 라면 전환 상태이므로 Com_Normal로 설정
            else if (_model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && CAN_ID != 2) _model.Error_Flag2 = false; //STBY_MODE -> Com_Normal

            //--------------------------COM FAIL - CAN ID 3--------------------------
            if (_model.CountComFault3_S > _model.ComFault_S && _model.Error_Flag3 == false && CAN_ID != 3)
            {
                _model.Error_Flag3 = true; //Com_Error

                if (_model.STAND_BY_LAMP == true && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 1)
                {
                    _model.RunStatus = true; //auto change to RUN
                }

                _model.RX_Data3 = new byte[8]; //수신 데이터 초기화. 직전 수신 값들을 0으로 모두 초기화.
            }
            else if (_model.CountComInit == _model.ComInit_S || _model.CountComInit > _model.ComInit_S)
            {
                if (_model.CountComFault3_S <= _model.ComFault_S && _model.Error_Flag3 == true && CAN_ID != 3) _model.Error_Flag3 = false; //Com_Normal
            }
            if (_model.RX_Data3[CANDataIndices.MODE_STATUS] == 0 && CAN_ID != 3) _model.Error_Flag3 = true; //MANUAL_MODE -> Com_Error
            //ID1이 STBY 모드(1) 라면 전환 상태이므로 Com_Normal로 설정
            else if (_model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && CAN_ID != 3) _model.Error_Flag3 = false; //STBY_MODE -> Com_Normal
        }

        /// <summary>
        /// CAN메시지 수신 처리
        /// </summary>
        private void CanRevMsg()
        {
            _model.CountComFault1_S = 0;
            _model.CountComFault2_S = 0;
            _model.CountComFault3_S = 0;
        }

        #region StandByLampProc
        ////////////////////////////StandByLampProc 관련 로직////////////////////////////////////
        /// <summary>
        /// StandBy 램프 처리
        /// </summary>
        private void StandByLampProc(bool modeStatus)
        {
            if (_model == null) return;

            if (modeStatus) // STBY_MODE
            {
                switch (_model.ComStatus)
                {
                    case 3: // StandBy_3(3대 연결) 상황에서 누가 메인 누가 스탠바이인지를 정하는 규칙
                        // 사고 후 메인 펌프 쪽 로직
                        // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                        if (_model.Overload == false &&
                            ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                             (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1) ||
                             (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1)))
                        {
                            if (_model.ResetButton == true)
                            {
                                //다른 장치 중 RUN이면서 STBY 램프가 켜진게 있다면, 내가 사고난 메인이라고 볼 수 있고, 리셋 버튼 까지 눌렀으니 내가 스탠바이로 전환
                                _model.STAND_BY_LAMP = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                            }
                        }
                        // 사고 후 스탠바이 펌프 쪽 로직
                        // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                        else if (_model.RUN_LAMP && _model.STAND_BY_LAMP) //내가 STBY고, RUN 중 (내가 헬퍼인 상황)
                            //이 때 다른 장치가 STOP 상태에서 Reset 버튼을 눌렀다면, STBY 램프를 끄고 내가 메인으로 전환된다.
                        {
                            if ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1) ||
                                (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.RESET_BUTTON] == 1) ||
                                (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RESET_BUTTON] == 1))
                            {
                                _model.STAND_BY_LAMP = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                            }
                        }
                        // AFTER POWER RECOVERY
                        // 전원 복구 직후 초기 셋팅
                        // 나를 제외한 두 유닛이 이미 “메인(RUN & STBY 램프 OFF)” + “스탠바이(STOP & STBY 램프 ON)”로 정상 짝을 이뤄 놓은 패턴이면, 나는 STBY 램프를 끔(OFF). 이미 갖춰진 메인-스탠바이 쌍에게 혼선이 가지 않도록?
                        // 전원 복구 경우, 이미 둘이 메인+스탠바이로 자리잡았으면 나(세 번째)는 STBY 램프 OFF로 빠져 혼선 방지.
                        else if ((CAN_ID == 1 &&
                                  ((_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1) ||
                                   (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1))) ||
                                 (CAN_ID == 2 &&
                                  ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1) ||
                                   (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1))) ||
                                 (CAN_ID == 3 &&
                                  ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1) ||
                                   (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1))))
                        {
                            _model.STAND_BY_LAMP = false;
                        }

                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        // 다른 장치 중 STBY 모드로 RUN 중이면서 STBY 램프가 꺼진 장치가 있다면 그 장치가 메인이다.
                        // 내가 정지 상태이고 오버로드 없고 초기화 플래그도 아니면, 나는 스탠바이로 전환. 메인 있으면 내가 스탠바이로 동작.
                        // 위의 조건문에 들어가지 못하고 여기로 왔으니까, 메인-스탠바이 쌍이 아직 없다는 뜻?
                        else if (((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0) ||
                                  (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0) ||
                                  (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0)) &&
                                 _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                        {
                            _model.STAND_BY_LAMP = true;
                        }
                        // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                        // 메인이 정상 종료(STOP & 오버로드 없음) 하면, 내 STBY 램프를 끈다. 메인이 내려가면 스탠바이도 내려가도록 하는 듯?
                        else if ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1) ||
                                 (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1) ||
                                 (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1))
                        {
                            _model.STAND_BY_LAMP = false;
                        }
                        // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                        // 메인이 수동 모드로 전환되어서 운전 중이면 내 STBY 램프를 끈다. 수동 운전일 때는 스탠바이 로직 비활성화.
                        else if ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 0) ||
                                 (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 0) ||
                                 (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 0))
                        {
                            _model.STAND_BY_LAMP = false;
                        }
                        break;

                    case 5: // StandBy_3_1RUN
                        HandleStandBy_3_1RUN();
                        break;

                    case 2: // StandBy_2
                    case 1: // StandBy_3to2
                    case 6: // StandBy_3to2_1RUN
                        HandleStandBy_2_or_3to2();
                        break;
                }
            }
            else // MANUAL_MODE
            {
                _model.STAND_BY_LAMP = false;
            }

            // StandBy 신호 출력
            _model.STAND_BY_LAMP = _model.MODE_STBY_LAMP;
        }

        /// <summary>
        /// StandBy_3_1RUN 상태 처리 (코드 구조화를 위한 분리 메서드)
        /// 3대 연결인데 1대만 RUN 중일 때 standby 역활을 누구에게 줄지 정하고, 중복 STBY 지정이나 오작동을 방지하는 로직
        /// </summary>
        private void HandleStandBy_3_1RUN()
        {
            // 장치 ID별 분기 처리
            // 1번이 메인인 경우
            if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0) //이 조건 만족하면 1번이 메인
            {
                if (CAN_ID == 2)
                    //1번이 메인인 경우 대기 1순위는 2번, 2순위는 3번으로 정해 놓은거 같다. 3번은 오버로드나 지연 조건을 따진다.
                    //두 대가 동시에 STBY를 켜는 경합 상황을 막기 위한 장치
                {
                    if (_model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0)
                    // 메인(1) + 다른 한쪽(3)도 STBY가 아님 아직 STBY가 아무도 없음
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                        {
                            _model.STAND_BY_LAMP = true;
                        }
                    }
                    if (_model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1) //이미 다른 장치가 스탠바이 중
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
                else if (CAN_ID == 3)
                {
                    // 오버로드가 끼어 있으면 결정을 미루고, 일정 시간(CountStandByCheck_mS) 지난 뒤 재판정
                    if (_model.Overload == true && _model.RX_Data2[CANDataIndices.OVERLOAD] == 1)
                    {
                        _model.CountStandByCheck_mS = 0; // 카운터 리셋
                    }
                    //나는 오버로드 아니고 다른 장치(2번)가 오버로드이거나, 다른 장치가 STBY 램프가 꺼진 상태라면
                    else if (_model.Overload == false && (_model.RX_Data2[CANDataIndices.OVERLOAD] == 1 || _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0) && _model.CountStandByCheck_mS >=5 )
                    {
                        if (_model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                            {
                                //스탠바이 비어 있고 내 상태가 멀쩡하면 내가 스탠바이하겠다.
                                _model.STAND_BY_LAMP = true;
                            }
                        }
                    }
                    //2번 장치가 스탠바이 중이면 나는 스탠바이 못함
                    if (_model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }

                if (_model.RX_Data1[CANDataIndices.MODE_STATUS] == 0) // MANUAL_MODE
                {
                    _model.STAND_BY_LAMP = false; // 메인이 MANUAL이면 나는 STBY 해제
                }
            }
            //2번이 메인인 경우
            else if (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0)
            {
                if (CAN_ID == 3)
                {
                    if (_model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0)
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP && _model.InitFlag == false)
                        {
                            _model.STAND_BY_LAMP = true;
                        }
                    }
                    if (_model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
                else if (CAN_ID == 1)
                {
                    if (_model.Overload == true && _model.RX_Data3[CANDataIndices.OVERLOAD] == 1)
                    {
                        _model.CountStandByCheck_mS = 0; // 카운터 리셋
                    }
                    else if (_model.Overload == false && (_model.RX_Data3[CANDataIndices.OVERLOAD] == 1 || _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0) && _model.CountStandByCheck_mS >= 5)
                    {
                        if (_model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                            {
                                _model.STAND_BY_LAMP = true;
                            }
                        }
                    }

                    if (_model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }

                if (_model.RX_Data2[CANDataIndices.MODE_STATUS] == 0) // MANUAL_MODE
                {
                    _model.STAND_BY_LAMP = false;
                }
            }
            //3번이 메인인 경우
            else if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0)
            {
                if (CAN_ID == 1)
                {
                    if (_model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 0)
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                        {
                            _model.STAND_BY_LAMP = true;
                        }
                    }
                    else if (_model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
                else if (CAN_ID == 2)
                {
                    if (_model.Overload == true && _model.RX_Data1[CANDataIndices.OVERLOAD] == 1)
                    {
                        _model.CountStandByCheck_mS = 0; // 카운터 리셋
                    }
                    else if (_model.Overload == false && (_model.RX_Data1[CANDataIndices.OVERLOAD] == 1 || _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0) && _model.CountStandByCheck_mS >= 5)
                    {
                        if (_model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.Overload == false && _model.RUN_LAMP == false && _model.STOP_LAMP == true && _model.InitFlag == false)
                            {
                                _model.STAND_BY_LAMP = true;
                            }
                        }
                    }

                    if (_model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }

                if (_model.RX_Data3[CANDataIndices.MODE_STATUS] == 0) // MANUAL_MODE
                {
                    _model.STAND_BY_LAMP = false;
                }
            }
            // 아무도 메인이 아닌 경우 (3대 모두 STOP 상태이거나, 2대 이상이 RUN 중이거나, RUN 중인 장치가 모두 STBY 램프 ON 상태이거나 등등)
            //StandBy_3_1RUN 상황에서 1대도 메인이 아닌 경우는, 3대 모두 STOP 상태이거나, 2대 이상이 RUN 중이거나, RUN 중인 장치가 모두 STBY 램프 ON 상태이거나 등등
            //StandBy_3_1RUN 분기 안에서 위의 메인 식별 케이스에 해당하지 않을 때 들어오는 부분, 혼선 구간에서 메인과 스탠바이를 정리하는 로직을 수행한다.
            else
            {
                _model.CountStandByCheck_mS = 0; // 카운터 리셋

                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                //사고 후 기존 메인 쪽의 롤 스왑 규칙
                // RUN이면서 STBY 램프가 켜진 장치가 있다면 그건 스탠바이가 붙어서 같이 운전 중인 상황 (사고 후 동시 운전)
                //이때 내가 Reset을 누르면 나를 STBY로 바꿔서 역할을 스왑
                if (_model.Overload == false && ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1) ||
                                        (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[CANDataIndices.STANDBY_LAMP] == 1) ||
                                        (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.STANDBY_LAMP] == 1)))
                {
                    if (_model.ResetButton == true)
                    {
                        _model.STAND_BY_LAMP = true; // CHANGE MAIN PUMP (MAIN -> STBY)

                        //if (_model.StandBy_3_1RUN_Flag == true)
                        //{
                        //    _model.StandBy_3_1RUN_Flag = false;
                        //}
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                // 내가 STBY로 붙어서 RUN 중인 상황 처리
                else if (_model.RUN_LAMP == true && _model.STAND_BY_LAMP == true)
                {
                    if ((_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1) ||
                        (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.RESET_BUTTON] == 1) ||
                        (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.RESET_BUTTON] == 1))
                    {
                        _model.STAND_BY_LAMP = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        //if (_model.StandBy_3_1RUN_Flag == true)
                        //{
                        //    _model.StandBy_3_1RUN_Flag = false;
                        //}
                    }
                }

                // 장치 ID별 추가 처리
                HandleDeviceSpecificConditions();
            }
        }

        /// <summary>
        /// 장치 ID에 따른 특별 조건 처리
        /// 내가 STBY 램프 켜진 상태일 때, 다른 장치들의 상태를 보고 내가 STBY 램프를 끄는 조건들, 중복 STBY/교착/깜빡임 방지
        /// </summary>
        private void HandleDeviceSpecificConditions()
        {
            if (CAN_ID == 1 && _model.STAND_BY_LAMP)
            {
                //양쪽 모두 오버로드가 없다 = 안정됐음.이때
                //상대(예: 2번)가 정상 대기 상태(STOP &STBY 모드)**로 이미 자리 잡았고 나는 RUN이 아님 내 STBY는 필요 없음(중복 대기 금지)
                //또는 상대가 MANUAL로 RUN 자동 협조 불가라서 내 STBY도 해제(자동 대기 기대치 제거)
                if (_model.RX_Data2[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0)
                {
                    if (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                        _model.STAND_BY_LAMP = false;
                    else if (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 0)
                        _model.STAND_BY_LAMP = false;
                }
                else if(_model.CountOverload_S > 1)
                {
                    if(_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    //과부하가 계속되면 플래핑이 쉽게 생김.그래서 기준을 더 보수적으로 잡아 STBY를 과감히 정리
                    //한쪽이 STOP &Overload OFF & STBY 모드로 복귀했고, 나는 RUN 아님 → 내 STBY OFF
                    //또는 양쪽 다 Overload ON인데 나는 RUN 아님 → 내 STBY OFF
                    //혼란기에 “중복 대기/ 가짜 대기”가 남아 시스템을 더 헷갈리게 만드는 걸 차단.
                    else if (_model.RX_Data2[CANDataIndices.OVERLOAD] == 1 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 1 && _model.RUN_LAMP == false) //  특이하게 2번과 3번을 비교하네
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
            }
            else if (CAN_ID == 2 && _model.STAND_BY_LAMP == true)
            {
                if (_model.RX_Data3[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0)
                {
                    if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                        _model.STAND_BY_LAMP = false;
                    else if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 0)
                        _model.STAND_BY_LAMP = false;
                }
                else if (_model.CountOverload_S > 1)
                {
                    if( _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data3[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data3[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data3[CANDataIndices.OVERLOAD] == 1 && _model.RX_Data1[CANDataIndices.OVERLOAD] == 1 && _model.RUN_LAMP == false) //  특이하게 1번과 3번을 비교하네
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
            }
            else if (CAN_ID == 3 && _model.STAND_BY_LAMP == true)
            {
                if (_model.RX_Data1[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 0)
                {
                    if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                        _model.STAND_BY_LAMP = false;
                    else if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 0)
                        _model.STAND_BY_LAMP = false;
                }
                else if (_model.CountOverload_S > 1)
                {
                    if( _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data2[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data2[CANDataIndices.MODE_STATUS] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                    else if (_model.RX_Data1[CANDataIndices.OVERLOAD] == 1 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 1 && _model.RUN_LAMP == false)
                    {
                        _model.STAND_BY_LAMP = false;
                    }
                }
            }
        }

        /// <summary>
        /// StandBy_2 또는 StandBy_3to2 상태 처리
        /// </summary>
        private void HandleStandBy_2_or_3to2()
        {
            // 장치 ID에 따른 조건 처리
            if (_model.Error_Flag1 == false && _deviceId != "1")
            {
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                if (!_model.Overload && (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1))
                {
                    if (_model.ResetButton)
                    {
                        _model.STAND_BY_LAMP = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                else if (_model.RUN_LAMP && _model.STAND_BY_LAMP)
                {
                    if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1)
                    {
                        _model.STAND_BY_LAMP = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                else if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && !_model.Overload && !_model.RUN_LAMP && !_model.InitFlag && _model.STOP_LAMP)
                {
                    _model.STAND_BY_LAMP = true;
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data1[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && !_model.RUN_LAMP)
                {
                    _model.STAND_BY_LAMP = false;
                }
                // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                else if (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 0)
                {
                    _model.STAND_BY_LAMP = false;
                }
            }
            else if (_model.Error_Flag2 == false && _deviceId != "2")
            {
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                if (!_model.Overload && (_model.RX_Data2[1] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1))
                {
                    if (_model.ResetButton)
                    {
                        _model.STAND_BY_LAMP = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                else if (_model.RUN_LAMP && _model.STAND_BY_LAMP)
                {
                    if (_model.RX_Data2[1] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1)
                    {
                        _model.STAND_BY_LAMP = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                else if (_model.RX_Data2[1] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && !_model.Overload && !_model.RUN_LAMP && !_model.InitFlag && _model.STOP_LAMP)
                {
                    _model.STAND_BY_LAMP = true;
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data2[1] == 0 && _model.RX_Data2[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && !_model.RUN_LAMP)
                {
                    _model.STAND_BY_LAMP = false;
                }
                // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                else if (_model.RX_Data2[1] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 0)
                {
                    _model.STAND_BY_LAMP = false;
                }
            }
            else if (_model.Error_Flag3 == false && _deviceId != "3")
            {
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                if (!_model.Overload && (_model.RX_Data3[1] == 1 && _model.RX_Data1[CANDataIndices.STANDBY_LAMP] == 1))
                {
                    if (_model.ResetButton)
                    {
                        _model.STAND_BY_LAMP = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                else if (_model.RUN_LAMP && _model.STAND_BY_LAMP)
                {
                    if (_model.RX_Data3[1] == 0 && _model.RX_Data1[CANDataIndices.RESET_BUTTON] == 1)
                    {
                        _model.STAND_BY_LAMP = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                else if (_model.RX_Data3[1] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && !_model.Overload && !_model.RUN_LAMP && !_model.InitFlag && _model.STOP_LAMP)
                {
                    _model.STAND_BY_LAMP = true;
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data3[1] == 0 && _model.RX_Data3[CANDataIndices.OVERLOAD] == 0 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 1 && !_model.RUN_LAMP)
                {
                    _model.STAND_BY_LAMP = false;
                }
                // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                else if (_model.RX_Data3[1] == 1 && _model.RX_Data1[CANDataIndices.MODE_STATUS] == 0)
                {
                    _model.STAND_BY_LAMP = false;
                }
            }
        }
        ////////////////////////////StandByLampProc 관련 로직////////////////////////////////////

        #endregion


        /// <summary>
        /// Run 요청 수신 처리 (StandBy 펌프)
        /// </summary>
        private void ReceiveRunReq()
        {
            if (_model == null) return;

            if (!_model.Overload && _model.STAND_BY_LAMP)
            {
                if (!_requestFlag &&
                    (_model.RX_Data1[CANDataIndices.RUN_REQ] == 1 || _model.RX_Data1[CANDataIndices.RUN_REQ] == 1 || _model.RX_Data1[CANDataIndices.RUN_REQ] == 1))
                {
                    _requestFlag = true;
                    _model.RunStatus = true;
                }
                else if (_model.RX_Data1[CANDataIndices.RUN_REQ] == 0 && _model.RX_Data1[CANDataIndices.RUN_REQ] == 0 && _model.RX_Data1[CANDataIndices.RUN_REQ] == 0)
                {
                    _requestFlag = false;
                }
            }
        }

        /// <summary>
        /// Run 요청 전송 처리 (메인 펌프)
        /// </summary>
        private void SendRunReq()
        {
            if (_model == null) return;

            if (_model.ModeStatus && _model.ComStatus != 0) // STBY_MODE & Connected
            {
                // 저압 상태에서 실행 중인 경우 Run 요청 전송
                if (_model.LOW_PRESS_LAMP && _model.RUN_LAMP &&
                    _model.CountBuildUpTime_S == _model.CountBuildUpTime &&
                    !_model.RUN_req)
                {
                    _model.RUN_req = true;
                }
                // 과부하 상태에서 Run 요청 전송
                else if (_model.Stop_Overload && _model.Overload &&
                        !_model.STBY_Overload &&
                        _model.CountRunReq_S == 1 &&
                        !_model.RUN_req)
                {
                    _model.RUN_req = true;
                }
                // 정상 상태로 복귀 시 Run 요청 취소
                else if (!_model.Overload && !_model.LOW_PRESS_LAMP && !_model.RUN_LAMP &&
                        (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 || _model.RX_Data2[1] == 1 || _model.RX_Data3[1] == 1) &&
                        _model.RUN_req)
                {
                    _model.RUN_req = false;
                }
                // 저압 상태에서 다른 펌프가 실행 중인 경우 Run 요청 취소
                else if (_model.LOW_PRESS_LAMP &&
                        (_model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 || _model.RX_Data2[1] == 1 || _model.RX_Data3[1] == 1) &&
                        !_model.RUN_LAMP && _model.RUN_req)
                {
                    _model.RUN_req = false;
                }
                // 과부하 상태가 지속될 경우 Run 요청 취소
                else if (_model.Overload && _model.CountRunReq_S > 1 && _model.RUN_req)
                {
                    _model.RUN_req = false;
                }
            }
        }

        /// <summary>
        /// StandBy 시작 알람 처리
        /// </summary>
        private void StbyStartAlarm()
        {
            if (_model == null) return;

            if (_model.RUN_LAMP && _model.STAND_BY_LAMP)
            {
                _model.STBY_Start = true;
            }
            else
            {
                _model.STBY_Start = false;
            }
        }

        ////////////////////////////ConnectFunction 관련 로직////////////////////////////////////
        /// <summary>
        /// 연결 상태 함수 - PSTARFW.c의 ConnectFunction 구현
        /// </summary>
        private void ConnectFunction()
        {
            if (_model == null) return;

            // 이 함수는 다른 CAN 통신 상태에 따라 현재 장치의 연결 상태를 결정
            // 연결 상태 코드 (펌웨어와 동일)
            // 0: NoConnection, 1: StandBy_3to2, 2: StandBy_2, 3: StandBy_3
            // 4: Manual, 5: StandBy_3_1RUN, 6: StandBy_3to2_1RUN

            switch (_deviceId)
            {
                case "1":
                    HandleConnectDevice1();
                    break;
                case "2":
                    HandleConnectDevice2();
                    break;
                case "3":
                    HandleConnectDevice3();
                    break;
            }
        }

        /// <summary>
        /// 장치 ID 1에 대한 연결 상태 처리
        /// </summary>
        private void HandleConnectDevice1()
        {
            // MANUAL 모드이면 연결 없음 (Manual)
            if (!_model.ModeStatus)
            {
                _model.ComStatus = 4; // Manual
                return;
            }

            // 연결 없음 (NoConnection)
            if (_model.Error_Flag2 && _model.Error_Flag3)
            {
                _model.ComStatus = 0; // NoConnection
                return;
            }

            // 3개 연결 (StandBy_3)
            if (!_model.Error_Flag2 && !_model.Error_Flag3)
            {
                _model.StandBy_3_1RUN_Flag = false;

                // 실행 상태에 따라 ComStatus 결정
                if ((_model.RUN_LAMP && _model.RX_Data2[1] == 1 && _model.RX_Data3[1] == 0) ||
                    (_model.RUN_LAMP && _model.RX_Data2[1] == 0 && _model.RX_Data3[1] == 1) ||
                    (!_model.RUN_LAMP && _model.RX_Data2[1] == 1 && _model.RX_Data3[1] == 1))
                {
                    _model.ComStatus = 3; // StandBy_3
                }
                else if ((_model.RUN_LAMP && _model.RX_Data2[1] == 0 && _model.RX_Data3[1] == 0) ||
                        (!_model.RUN_LAMP && _model.RX_Data2[1] == 1 && _model.RX_Data3[1] == 0) ||
                        (!_model.RUN_LAMP && _model.RX_Data2[1] == 0 && _model.RX_Data3[1] == 1))
                {
                    _model.ComStatus = 5; // StandBy_3_1RUN
                }
                return;
            }

            // 2개 연결 (StandBy_2 또는 StandBy_3to2)
            if ((!_model.Error_Flag2 && _model.Error_Flag3) || (_model.Error_Flag2 && !_model.Error_Flag3))
            {
                if (_model.ComStatus == 3) // 이전이 StandBy_3이었다면
                {
                    _model.ComStatus = 1; // StandBy_3to2
                }
                else if (_model.ComStatus == 2) // 이미 StandBy_2 상태라면
                {
                    // 유지
                }
                else if (_model.ComStatus == 0 || _model.ComStatus == 4) // 연결 없음 또는 Manual 상태였다면
                {
                    _model.ComStatus = _model.ComStatus_Flag ? 1 : 2; // 메모리 상태에 따라 결정
                }
                else if (_model.ComStatus == 5) // StandBy_3_1RUN 상태였다면
                {
                    _model.ComStatus = 6; // StandBy_3to2_1RUN
                    _model.StandBy_3_1RUN_Flag = true;
                }
            }
        }

        /// <summary>
        /// 장치 ID 2에 대한 연결 상태 처리
        /// </summary>
        private void HandleConnectDevice2()
        {
            // MANUAL 모드이면 연결 없음 (Manual)
            if (!_model.ModeStatus)
            {
                _model.ComStatus = 4; // Manual
                return;
            }

            // 연결 없음 (NoConnection)
            if (_model.Error_Flag1 && _model.Error_Flag3)
            {
                _model.ComStatus = 0; // NoConnection
                return;
            }

            // 3개 연결 (StandBy_3)
            if (!_model.Error_Flag1 && !_model.Error_Flag3)
            {
                _model.StandBy_3_1RUN_Flag = false;

                // 실행 상태에 따라 ComStatus 결정
                if ((_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[1] == 0) ||
                    (_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[1] == 1) ||
                    (!_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[1] == 1))
                {
                    _model.ComStatus = 3; // StandBy_3
                }
                else if ((_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[1] == 0) ||
                        (!_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data3[1] == 0) ||
                        (!_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data3[1] == 1))
                {
                    _model.ComStatus = 5; // StandBy_3_1RUN
                }
                return;
            }

            // 2개 연결 (StandBy_2 또는 StandBy_3to2)
            if ((!_model.Error_Flag1 && _model.Error_Flag3) || (_model.Error_Flag1 && !_model.Error_Flag3))
            {
                if (_model.ComStatus == 3) // 이전이 StandBy_3이었다면
                {
                    _model.ComStatus = 1; // StandBy_3to2
                }
                else if (_model.ComStatus == 2) // 이미 StandBy_2 상태라면
                {
                    // 유지
                }
                else if (_model.ComStatus == 0 || _model.ComStatus == 4) // 연결 없음 또는 Manual 상태였다면
                {
                    _model.ComStatus = _model.ComStatus_Flag ? 1 : 2; // 메모리 상태에 따라 결정
                }
                else if (_model.ComStatus == 5) // StandBy_3_1RUN 상태였다면
                {
                    _model.ComStatus = 6; // StandBy_3to2_1RUN
                    _model.StandBy_3_1RUN_Flag = true;
                }
            }
        }

        /// <summary>
        /// 장치 ID 3에 대한 연결 상태 처리
        /// </summary>
        private void HandleConnectDevice3()
        {
            // MANUAL 모드이면 연결 없음 (Manual)
            if (!_model.ModeStatus)
            {
                _model.ComStatus = 4; // Manual
                return;
            }

            // 연결 없음 (NoConnection)
            if (_model.Error_Flag1 && _model.Error_Flag2)
            {
                _model.ComStatus = 0; // NoConnection
                return;
            }

            // 3개 연결 (StandBy_3)
            if (!_model.Error_Flag1 && !_model.Error_Flag2)
            {
                _model.StandBy_3_1RUN_Flag = false;

                // 실행 상태에 따라 ComStatus 결정
                if ((_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[1] == 0) ||
                    (_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[1] == 1) ||
                    (!_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[1] == 1))
                {
                    _model.ComStatus = 3; // StandBy_3
                }
                else if ((_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[1] == 0) ||
                        (!_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 1 && _model.RX_Data2[1] == 0) ||
                        (!_model.RUN_LAMP && _model.RX_Data1[CANDataIndices.RUN_LAMP] == 0 && _model.RX_Data2[1] == 1))
                {
                    _model.ComStatus = 5; // StandBy_3_1RUN
                }
                return;
            }

            // 2개 연결 (StandBy_2 또는 StandBy_3to2)
            if ((!_model.Error_Flag1 && _model.Error_Flag2) || (_model.Error_Flag1 && !_model.Error_Flag2))
            {
                if (_model.ComStatus == 3) // 이전이 StandBy_3이었다면
                {
                    _model.ComStatus = 1; // StandBy_3to2
                }
                else if (_model.ComStatus == 2) // 이미 StandBy_2 상태라면
                {
                    // 유지
                }
                else if (_model.ComStatus == 0 || _model.ComStatus == 4) // 연결 없음 또는 Manual 상태였다면
                {
                    _model.ComStatus = _model.ComStatus_Flag ? 1 : 2; // 메모리 상태에 따라 결정
                }
                else if (_model.ComStatus == 5) // StandBy_3_1RUN 상태였다면
                {
                    _model.ComStatus = 6; // StandBy_3to2_1RUN
                    _model.StandBy_3_1RUN_Flag = true;
                }
            }
        }
        ////////////////////////////ConnectFunction 관련 로직////////////////////////////////////


        #endregion

        #region 외부 인터페이스 메서드
        

        /// <summary>
        /// 과부하 상태 설정
        /// </summary>
        public void SetOverload(bool isOverload)
        {
            if (_model == null) return;

            _model.Overload_I = isOverload;

            // 상태 변경 시 로직 실행
            ExecutePSTARLogic();
        }

        /// <summary>
        /// 저압 상태 설정
        /// </summary>
        public void SetLowPressure(bool isLowPressure)
        {
            if (_model == null) return;

            _model.Lowpress_I = isLowPressure;

            // 상태 변경 시 로직 실행
            ExecutePSTARLogic();
        }

        /// <summary>
        /// 상태 변경 알림
        /// desperate
        /// </summary>
        private void NotifyStateChanged()
        {
            //if (_model == null) return;

            //DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs
            //{
            //    DeviceId = _deviceId,
            //    IsRunning = _model.RunStatus,
            //    IsStandByMode = _model.ModeStatus,
            //    IsHeating = _model.HeatStatus,
            //    IsSTAND_BY_LAMP = _model.STAND_BY_LAMP,
            //    TXLowpress  = _model.TXLowpress
            //});
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            _transmitTimer?.Stop();
            _transmitTimer?.Dispose();

            // 모델 이벤트 구독 해제
            if (_model != null)
            {
                _model.StateChanged -= OnModelStateChanged;
            }
        }
        #endregion
    }

    /// <summary>
    /// 장치 상태 변경 이벤트 인수
    /// desperate
    /// </summary>
    public class DeviceStateChangedEventArgs : EventArgs
    {
        public string DeviceId { get; set; }
        public bool IsRunning { get; set; }
        public bool IsStandByMode { get; set; }
        public bool IsHeating { get; set; }
        public bool IsSTAND_BY_LAMP { get; set; }
        public bool TXLowpress { get; set; }
    }
}