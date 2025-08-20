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
        private readonly int _canTransmitInterval = 500; // CAN 전송 주기 (ms)

        // CAN 전송 타이머 (로직 타이머는 제거)
        private readonly Timer _transmitTimer;

        // 모델 레퍼런스 (장치 모델 공유)
        private PSTARDeviceModel _model;

        // PSTARFW 변수 (필요한 것만 추가)
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

        #region 프로퍼티
        // 상태 접근을 위한 읽기 전용 프로퍼티
        public bool RunStatus => _model?.RunStatus ?? false;
        public bool HeatStatus => _model?.HeatStatus ?? false;
        public bool ModeStatus => _model?.ModeStatus ?? false;
        public bool STBY_Start => _model?.STBY_Start ?? false;
        public bool RunLamp => _model?.RunLamp ?? false;
        public bool Overload => _model?.Overload ?? false;
        public bool RUN_req => _model?.RUN_req ?? false;
        public bool ResetButton => _model?.ResetButton ?? false;
        public bool StandByLamp => _model?.StandByLamp ?? false;
        public bool TXLowpress => _model?.TXLowpress ?? false;
        public bool StopLamp => _model?.StopLamp ?? true;
        public bool OldLowpress => _model?.OldLowpress ?? false;
        public bool OldRunStatus => _model?.OldRunStatus ?? false;
        public int CountBuildUpTime => _model?.CountBuildUpTime ?? 0;
        public int CountHeatingOnTime_S => _model?.CountHeatingOnTime_S ?? 0;
        public int HeatingOnTime => _model ?.HeatingOnTime ?? 3;
        public int CountRunReq_S => _model?.CountRunReq_S ?? 0;
        public int CountComFault1_S => _model?.CountComFault1_S ?? 0;
        public int CountComFault2_S => _model?.CountComFault2_S ?? 0;
        public int CountComFault3_S => _model?.CountComFault3_S ?? 0;
        public int CountComInit => _model?.CountComInit ?? 0;
        public bool StandBy_3_1RUN_Flag => _model?.StandBy_3_1RUN_Flag ?? false;
        public int CountSeqTime_S => _model?.CountSeqTime_S ?? 0;
        public bool CountOverload_Flag => _model?.CountOverload_Flag ?? false;
        #endregion


        #region CANDataIndex
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
                case "1": _canId = 0x100; break;
                case "2": _canId = 0x200; break;
                case "3": _canId = 0x300; break;
                default: _canId = 0x100; break;
            }

            // CAN 전송 타이머
            _transmitTimer = new Timer(_canTransmitInterval);
            _transmitTimer.Elapsed += OnTransmitTimerElapsed;
            _transmitTimer.AutoReset = true;
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
        private void ExecutePSTARLogic()
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
            if (_model == null) return;

            // 모델에서 데이터 읽어서 CAN 프레임 구성
            byte[] data = new byte[8];
            data[0] = (byte)(_model.STBY_Start ? 1 : 0);
            data[1] = (byte)(_model.RunLamp ? 1 : 0);
            data[2] = (byte)(_model.Overload ? 1 : 0);
            data[3] = (byte)(_model.ModeStatus ? 1 : 0);  // 0: MANUAL, 1: STBY
            data[4] = (byte)(_model.RUN_req ? 1 : 0);
            data[5] = (byte)(_model.ResetButton ? 1 : 0);
            data[6] = (byte)(_model.StandByLamp ? 1 : 0);
            data[7] = (byte)(_model.TXLowpress ? 1 : 0);

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
                        ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 1) ||
                         (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 1) ||
                         (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 1)))
                    {
                        _model.RunStatus = false;
                        // EEPROM 저장 부분은 C#에서는 제외
                        _model.InitFlag = false;
                    }
                    // After Power Fail (3 ST'BY(2 RUN) - MAIN 1 & MAIN2)
                    else if ((_deviceId == "1" && _model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 1 && _model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 1) ||
                             (_deviceId == "2" && _model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 1 && _model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 1) ||
                             (_deviceId == "3" && _model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 1 && _model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 1))
                    {
                        _model.RunStatus = false;
                        // EEPROM 저장 부분은 C#에서는 제외
                        _model.InitFlag = false;
                    }
                    // After Power Fail (3 ST'BY(1 RUN) - ST'BY)
                    else if (_model.ComStatus == 5 && // StandBy_3_1RUN
                            ((_model.RX_Data1[1] == 0 && _model.RX_Data1[5] == 1 && _model.RX_Data1[6] == 1) ||
                             (_model.RX_Data2[1] == 0 && _model.RX_Data2[5] == 1 && _model.RX_Data2[6] == 1) ||
                             (_model.RX_Data3[1] == 0 && _model.RX_Data3[5] == 1 && _model.RX_Data3[6] == 1)))
                    {
                        _model.RunStatus = false;
                        // EEPROM 저장 부분은 C#에서는 제외
                        _model.InitFlag = false;
                    }
                    // Sequential Time (MANUAL RUN / 2 STAND BY RUN & RUN / Power Recovery Before 1s (MAIN -> MAIN))
                    else
                    {
                        // 정지 램프 및 신호 끄기
                        _model.StopLamp = false;

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
            if (_model.RX_Data1[2] == 1 || _model.RX_Data2[2] == 1 || _model.RX_Data3[2] == 1)
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
            _model.TXLowpress = _model.TxLowpressInternal;
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

        /// <summary>
        /// 실행/정지 상태 처리
        /// </summary>
        private void RunStopProc(bool runStatus)
        {
            if (_model == null) return;

            if (runStatus) // RUN
            {
                _model.ResetButton = false;
                _model.StopLamp = false;
            }
            else // STOP
            {
                _model.StopLamp = true;
                _model.RunLamp = false;
            }
        }

        /// <summary>
        /// 실행 입력 처리
        /// </summary>
        private void RunInput()
        {
            if (_model == null) return;

            if (_model.RunFB_I) // Run Signal ON & Run Input ON -> Run Lamp ON
            {
                _model.RunLamp = true;
                _model.StopLamp = false;
            }
        }

        /// <summary>
        /// 히팅 처리
        /// </summary>
        private void HeatProc(bool heatStatus)
        {
            if (_model == null) return;

            if (!heatStatus) // HEAT OFF
            {
                _model.IsHeatOn = false;
                _model.IsHeating = false;
                _model.CountHeatingOnTime_S = 0;
            }
            else // HEAT ON
            {
                _model.IsHeatOn = true;

                if (!_model.RunStatus) // STOP 상태에서만 가열 신호 활성화
                {
                    if (_model.CountHeatingOnTime_S >= _model.HeatingOnTime)
                    {
                        _model.IsHeating = true;
                    }
                }
                else // RUN 상태에서는 가열 신호 비활성화
                {
                    _model.IsHeating = false;
                    _model.CountHeatingOnTime_S = 0;
                }
            }
        }

        /// <summary>
        /// 모드 처리
        /// </summary>
        private void ModeProc(bool modeStatus)
        {
            if (_model == null) return;

            if (!modeStatus) // MANUAL_MODE
            {
                _model.IsManualMode = true;
                _model.IsStandbyMode = false;
            }
            else // STBY_MODE
            {
                _model.IsManualMode = false;
                _model.IsStandbyMode = true;
            }
        }

        /// <summary>
        /// 과부하 처리
        /// </summary>
        private void OverloadProc(bool overloadStatus)
        {
            if (_model == null) return;

            if (overloadStatus)
            {
                if (_model.RunStatus)
                {
                    _model.Stop_Overload = true;
                    _model.RunStatus = false;
                }

                _model.Overload = true;
                _model.CountParallelTime_S = 0;

                _model.ResetButton = false;

                if (_model.StandByLamp)
                {
                    _model.StandByLamp = false; // Occur Overload -> StandBy x
                    _model.STBY_Overload = true; // STBY Overload -> Run Request x
                }
            }
            else
            {
                _model.Overload = false;

                _model.STBY_Overload = false;
                _model.Stop_Overload = false;
            }
        }

        /// <summary>
        /// 저압 처리
        /// </summary>
        private void LowpressProc(bool lowpressStatus)
        {
            if (_model == null) return;

            if (lowpressStatus)
            {
                _model.IsLowPressure = true;
                _model.TxLowpressInternal = true;

                // BuildUp 시간 설정 로직
                if (_model.OldRunStatus == true && _model.RunStatus == true)
                {
                    if (!_model.OldLowpress && _model.IsLowPressure)
                    {
                        _model.CountBuildUpTime = 3; // 3초 (RUN -> LowPressure)
                    }
                }

                // STOP -> LowPressure -> RUN
                if (_model.OldLowpress && _model.IsLowPressure)
                {
                    if (!_model.OldRunStatus && _model.RunStatus)
                    {
                        if (!_model.StandByLamp)
                        {
                            _model.CountBuildUpTime = _buildUpTime; // 설정값
                        }
                        else
                        {
                            _model.CountBuildUpTime = 3; // 3초
                        }
                    }
                }

                // BuildUp 시작 조건
                if (_model.IsLowPressure && _model.RunLamp &&
                    ((_model.RX_Data1[1] == 0 && _model.RX_Data1[6] == 1) ||
                     (_model.RX_Data2[1] == 0 && _model.RX_Data2[6] == 1) ||
                     (_model.RX_Data3[1] == 0 && _model.RX_Data3[6] == 1)))
                {
                    _model.CountBuildUpStart = 1;
                }
                else
                {
                    _model.CountBuildUpStart = 0;
                }

                // BuildUp 후 Parallel 시간 시작
                if (_model.RunLamp && _model.StandByLamp && _model.IsLowPressure)
                {
                    if (_model.CountBuildUpTime_S >= _model.CountBuildUpTime)
                    {
                        _model.CountParaStart = 0;
                        _model.CountParallelTime_S = 0;
                        _model.CountBuildUpStart = 0;
                        _model.CountBuildUpTime_S = 0;
                    }
                }
                else if (_model.IsLowPressure &&
                    ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 1) ||
                     (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 1) ||
                     (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 1)))
                {
                    if (_model.CountBuildUpTime_S >= _model.CountBuildUpTime)
                    {
                        _model.CountParaStart = 1;
                        _model.CountBuildUpStart = 0;
                        _model.CountBuildUpTime_S = 0;
                    }
                }

                // Parallel 시간 초과 처리
                if (_model.CountParaStart == 1)
                {
                    if (_model.CountParallelTime_S >= _parallelTime)
                    {
                        _model.RunStatus = false;
                        _model.CountParaStart = 0;
                        _model.CountParallelTime_S = 0;
                    }
                }
            }
            else
            {
                _model.IsLowPressure = false;
                _model.TxLowpressInternal = false;

                // 카운터 초기화
                _model.CountBuildUpTime_S = 0;
                _model.CountBuildUpStart = 0;
            }

            // 이전 상태 저장
            _model.OldLowpress = _model.IsLowPressure;
            _model.OldRunStatus = _model.RunStatus;
        }

        /// <summary>
        /// 통신 오류 플래그 처리
        /// </summary>
        private void ComFailErrorFlag()
        {
            if (_model == null) return;

            // CAN ID에 따라 다른 장치들의 통신 상태 확인
            string deviceId = _deviceId;

            // CAN ID 1번 장치의 통신 상태 확인
            if (_model.CountComFault1_S > 1 && _model.Error_Flag1 == false && deviceId != "1")
            {
                _model.Error_Flag1 = true;

                // 자동 전환 (StandBy에서 작동 중이던 장치 실패 시 실행)
                if (_model.StandByLamp && _model.RX_Data1[1] == 1)
                {
                    _model.RunStatus = true;
                }

                // 수신 데이터 초기화
                _model.RX_Data1 = new byte[8];
            }
            else if (_model.CountComInit >= 0)
            {
                if (_model.CountComFault1_S <= 1 && _model.Error_Flag1 && deviceId != "1")
                {
                    _model.Error_Flag1 = false;
                }
            }

            // 수동 모드는 연결되지 않은 것으로 간주
            if (_model.RX_Data1[3] == 0 && deviceId != "1") // MANUAL_MODE
            {
                _model.Error_Flag1 = true;
            }
            else if (_model.RX_Data1[3] == 1 && deviceId != "1") // STBY_MODE
            {
                _model.Error_Flag1 = false;
            }

            // CAN ID 2번 장치의 통신 상태 확인 (위와 동일한 로직)
            if (_model.CountComFault2_S > 1 && _model.Error_Flag2 == false && deviceId != "2")
            {
                _model.Error_Flag2 = true;

                if (_model.StandByLamp && _model.RX_Data2[1] == 1)
                {
                    _model.RunStatus = true;
                }

                _model.RX_Data2 = new byte[8];
            }
            else if (_model.CountComInit >= 0)
            {
                if (_model.CountComFault2_S <= 1 && _model.Error_Flag2 && deviceId != "2")
                {
                    _model.Error_Flag2 = false;
                }
            }

            if (_model.RX_Data2[3] == 0 && deviceId != "2")
            {
                _model.Error_Flag2 = true;
            }
            else if (_model.RX_Data2[3] == 1 && deviceId != "2")
            {
                _model.Error_Flag2 = false;
            }

            // CAN ID 3번 장치의 통신 상태 확인
            if (_model.CountComFault3_S > 1 && _model.Error_Flag3 == false && deviceId != "3")
            {
                _model.Error_Flag3 = true;

                if (_model.StandByLamp && _model.RX_Data3[1] == 1)
                {
                    _model.RunStatus = true;
                }

                _model.RX_Data3 = new byte[8];
            }
            else if (_model.CountComInit >= 0)
            {
                if (_model.CountComFault3_S <= 1 && _model.Error_Flag3 && deviceId != "3")
                {
                    _model.Error_Flag3 = false;
                }
            }

            if (_model.RX_Data3[3] == 0 && deviceId != "3")
            {
                _model.Error_Flag3 = true;
            }
            else if (_model.RX_Data3[3] == 1 && deviceId != "3")
            {
                _model.Error_Flag3 = false;
            }
        }

        /// <summary>
        /// Run 요청 수신 처리 (StandBy 펌프)
        /// </summary>
        private void ReceiveRunReq()
        {
            if (_model == null) return;

            if (!_model.Overload && _model.StandByLamp)
            {
                if (!_requestFlag &&
                    (_model.RX_Data1[4] == 1 || _model.RX_Data2[4] == 1 || _model.RX_Data3[4] == 1))
                {
                    _requestFlag = true;
                    _model.RunStatus = true;
                }
                else if (_model.RX_Data1[4] == 0 && _model.RX_Data2[4] == 0 && _model.RX_Data3[4] == 0)
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
                if (_model.IsLowPressure && _model.RunLamp &&
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
                else if (!_model.Overload && !_model.IsLowPressure && !_model.RunLamp &&
                        (_model.RX_Data1[1] == 1 || _model.RX_Data2[1] == 1 || _model.RX_Data3[1] == 1) &&
                        _model.RUN_req)
                {
                    _model.RUN_req = false;
                }
                // 저압 상태에서 다른 펌프가 실행 중인 경우 Run 요청 취소
                else if (_model.IsLowPressure &&
                        (_model.RX_Data1[1] == 1 || _model.RX_Data2[1] == 1 || _model.RX_Data3[1] == 1) &&
                        !_model.RunLamp && _model.RUN_req)
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

            if (_model.RunLamp && _model.StandByLamp)
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
                if ((_model.RunLamp && _model.RX_Data2[1] == 1 && _model.RX_Data3[1] == 0) ||
                    (_model.RunLamp && _model.RX_Data2[1] == 0 && _model.RX_Data3[1] == 1) ||
                    (!_model.RunLamp && _model.RX_Data2[1] == 1 && _model.RX_Data3[1] == 1))
                {
                    _model.ComStatus = 3; // StandBy_3
                }
                else if ((_model.RunLamp && _model.RX_Data2[1] == 0 && _model.RX_Data3[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data2[1] == 1 && _model.RX_Data3[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data2[1] == 0 && _model.RX_Data3[1] == 1))
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
                if ((_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data3[1] == 0) ||
                    (_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data3[1] == 1) ||
                    (!_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data3[1] == 1))
                {
                    _model.ComStatus = 3; // StandBy_3
                }
                else if ((_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data3[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data3[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data3[1] == 1))
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
                if ((_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data2[1] == 0) ||
                    (_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data2[1] == 1) ||
                    (!_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data2[1] == 1))
                {
                    _model.ComStatus = 3; // StandBy_3
                }
                else if ((_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data2[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data1[1] == 1 && _model.RX_Data2[1] == 0) ||
                        (!_model.RunLamp && _model.RX_Data1[1] == 0 && _model.RX_Data2[1] == 1))
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
                    case 3: // StandBy_3
                            // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                        if (!_model.Overload &&
                            ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 1) ||
                             (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 1) ||
                             (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 1)))
                        {
                            if (_model.ResetButton)
                            {
                                _model.StandByLamp = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                            }
                        }
                        // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                        else if (_model.RunLamp && _model.StandByLamp)
                        {
                            if ((_model.RX_Data1[1] == 0 && _model.RX_Data1[5] == 1) ||
                                (_model.RX_Data2[1] == 0 && _model.RX_Data2[5] == 1) ||
                                (_model.RX_Data3[1] == 0 && _model.RX_Data3[5] == 1))
                            {
                                _model.StandByLamp = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                            }
                        }
                        // AFTER POWER RECOVERY
                        else if ((_deviceId == "1" &&
                                  ((_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 0 && _model.RX_Data3[1] == 0 && _model.RX_Data3[6] == 1) ||
                                   (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 0 && _model.RX_Data2[1] == 0 && _model.RX_Data2[6] == 1))) ||
                                 (_deviceId == "2" &&
                                  ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 0 && _model.RX_Data3[1] == 0 && _model.RX_Data3[6] == 1) ||
                                   (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 0 && _model.RX_Data1[1] == 0 && _model.RX_Data1[6] == 1))) ||
                                 (_deviceId == "3" &&
                                  ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 0 && _model.RX_Data2[1] == 0 && _model.RX_Data2[6] == 1) ||
                                   (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 0 && _model.RX_Data1[1] == 0 && _model.RX_Data1[6] == 1))))
                        {
                            _model.StandByLamp = false;
                        }
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        else if (((_model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 1 && _model.RX_Data1[6] == 0) ||
                                  (_model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 1 && _model.RX_Data2[6] == 0) ||
                                  (_model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 1 && _model.RX_Data3[6] == 0)) &&
                                 !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                        {
                            _model.StandByLamp = true;
                        }
                        // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                        else if ((_model.RX_Data1[1] == 0 && _model.RX_Data1[2] == 0 && _model.RX_Data1[3] == 1) ||
                                 (_model.RX_Data2[1] == 0 && _model.RX_Data2[2] == 0 && _model.RX_Data2[3] == 1) ||
                                 (_model.RX_Data3[1] == 0 && _model.RX_Data3[2] == 0 && _model.RX_Data3[3] == 1))
                        {
                            _model.StandByLamp = false;
                        }
                        // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                        else if ((_model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 0) ||
                                 (_model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 0) ||
                                 (_model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 0))
                        {
                            _model.StandByLamp = false;
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
                _model.StandByLamp = false;
            }

            // StandBy 신호 출력
            _model.IsStandby = _model.StandByLamp;
        }

        /// <summary>
        /// StandBy_3_1RUN 상태 처리 (코드 구조화를 위한 분리 메서드)
        /// </summary>
        private void HandleStandBy_3_1RUN()
        {
            // 장치 ID별 분기 처리
            if (_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 0)
            {
                if (_deviceId == "2")
                {
                    if (_model.RX_Data3[6] == 0)
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data1[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                        {
                            _model.StandByLamp = true;
                        }
                    }
                    if (_model.RX_Data3[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }
                else if (_deviceId == "3")
                {
                    if (_model.Overload && _model.RX_Data2[2] == 1)
                    {
                        // 카운터 리셋
                    }
                    else if (!_model.Overload && (_model.RX_Data2[2] == 1 || _model.RX_Data2[6] == 0))
                    {
                        if (_model.RX_Data2[6] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data1[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                            {
                                _model.StandByLamp = true;
                            }
                        }
                    }

                    if (_model.RX_Data2[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }

                if (_model.RX_Data1[3] == 0) // MANUAL_MODE
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 0)
            {
                if (_deviceId == "3")
                {
                    if (_model.RX_Data1[6] == 0)
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data2[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                        {
                            _model.StandByLamp = true;
                        }
                    }
                    if (_model.RX_Data1[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }
                else if (_deviceId == "1")
                {
                    if (_model.Overload && _model.RX_Data3[2] == 1)
                    {
                        // 카운터 리셋
                    }
                    else if (!_model.Overload && (_model.RX_Data3[2] == 1 || _model.RX_Data3[6] == 0))
                    {
                        if (_model.RX_Data3[6] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data2[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                            {
                                _model.StandByLamp = true;
                            }
                        }
                    }

                    if (_model.RX_Data3[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }

                if (_model.RX_Data2[3] == 0) // MANUAL_MODE
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 0)
            {
                if (_deviceId == "1")
                {
                    if (_model.RX_Data2[6] == 0)
                    {
                        // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                        if (_model.RX_Data3[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                        {
                            _model.StandByLamp = true;
                        }
                    }
                    else if (_model.RX_Data2[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }
                else if (_deviceId == "2")
                {
                    if (_model.Overload && _model.RX_Data1[2] == 1)
                    {
                        // 카운터 리셋
                    }
                    else if (!_model.Overload && (_model.RX_Data1[2] == 1 || _model.RX_Data1[6] == 0))
                    {
                        if (_model.RX_Data1[6] == 0)
                        {
                            // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                            if (_model.RX_Data3[3] == 1 && !_model.Overload && !_model.RunLamp && _model.StopLamp && !_model.InitFlag)
                            {
                                _model.StandByLamp = true;
                            }
                        }
                    }

                    if (_model.RX_Data1[6] == 1)
                    {
                        _model.StandByLamp = false;
                    }
                }

                if (_model.RX_Data3[3] == 0) // MANUAL_MODE
                {
                    _model.StandByLamp = false;
                }
            }
            else
            {
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                if (!_model.Overload && ((_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 1) ||
                                        (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 1) ||
                                        (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 1)))
                {
                    if (_model.ResetButton)
                    {
                        _model.StandByLamp = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                else if (_model.RunLamp && _model.StandByLamp)
                {
                    if ((_model.RX_Data1[1] == 0 && _model.RX_Data1[5] == 1) ||
                        (_model.RX_Data2[1] == 0 && _model.RX_Data2[5] == 1) ||
                        (_model.RX_Data3[1] == 0 && _model.RX_Data3[5] == 1))
                    {
                        _model.StandByLamp = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }

                // 장치 ID별 추가 처리
                HandleDeviceSpecificConditions();
            }
        }

        /// <summary>
        /// 장치 ID에 따른 특별 조건 처리
        /// </summary>
        private void HandleDeviceSpecificConditions()
        {
            if (_deviceId == "1" && _model.StandByLamp)
            {
                if (_model.RX_Data2[2] == 0 && _model.RX_Data3[2] == 0)
                {
                    if (_model.RX_Data2[1] == 0 && _model.RX_Data2[3] == 1 && !_model.RunLamp)
                        _model.StandByLamp = false;
                    else if (_model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 0)
                        _model.StandByLamp = false;
                }
                else if (_model.RX_Data2[1] == 0 && _model.RX_Data2[2] == 0 && _model.RX_Data2[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data3[1] == 0 && _model.RX_Data3[2] == 0 && _model.RX_Data3[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data2[2] == 1 && _model.RX_Data3[2] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_deviceId == "2" && _model.StandByLamp)
            {
                if (_model.RX_Data3[2] == 0 && _model.RX_Data1[2] == 0)
                {
                    if (_model.RX_Data3[1] == 0 && _model.RX_Data3[3] == 1 && !_model.RunLamp)
                        _model.StandByLamp = false;
                    else if (_model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 0)
                        _model.StandByLamp = false;
                }
                else if (_model.RX_Data1[1] == 0 && _model.RX_Data1[2] == 0 && _model.RX_Data1[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data3[1] == 0 && _model.RX_Data3[2] == 0 && _model.RX_Data3[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data3[2] == 1 && _model.RX_Data1[2] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_deviceId == "3" && _model.StandByLamp)
            {
                if (_model.RX_Data1[2] == 0 && _model.RX_Data2[2] == 0)
                {
                    if (_model.RX_Data1[1] == 0 && _model.RX_Data1[3] == 1 && !_model.RunLamp)
                        _model.StandByLamp = false;
                    else if (_model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 0)
                        _model.StandByLamp = false;
                }
                else if (_model.RX_Data1[1] == 0 && _model.RX_Data1[2] == 0 && _model.RX_Data1[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data2[1] == 0 && _model.RX_Data2[2] == 0 && _model.RX_Data2[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                else if (_model.RX_Data1[2] == 1 && _model.RX_Data2[2] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
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
                if (!_model.Overload && (_model.RX_Data1[1] == 1 && _model.RX_Data1[6] == 1))
                {
                    if (_model.ResetButton)
                    {
                        _model.StandByLamp = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                else if (_model.RunLamp && _model.StandByLamp)
                {
                    if (_model.RX_Data1[1] == 0 && _model.RX_Data1[5] == 1)
                    {
                        _model.StandByLamp = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                else if (_model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 1 && !_model.Overload && !_model.RunLamp && !_model.InitFlag && _model.StopLamp)
                {
                    _model.StandByLamp = true;
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data1[1] == 0 && _model.RX_Data1[2] == 0 && _model.RX_Data1[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                else if (_model.RX_Data1[1] == 1 && _model.RX_Data1[3] == 0)
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_model.Error_Flag2 == false && _deviceId != "2")
            {
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                if (!_model.Overload && (_model.RX_Data2[1] == 1 && _model.RX_Data2[6] == 1))
                {
                    if (_model.ResetButton)
                    {
                        _model.StandByLamp = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                else if (_model.RunLamp && _model.StandByLamp)
                {
                    if (_model.RX_Data2[1] == 0 && _model.RX_Data2[5] == 1)
                    {
                        _model.StandByLamp = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                else if (_model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 1 && !_model.Overload && !_model.RunLamp && !_model.InitFlag && _model.StopLamp)
                {
                    _model.StandByLamp = true;
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data2[1] == 0 && _model.RX_Data2[2] == 0 && _model.RX_Data2[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                else if (_model.RX_Data2[1] == 1 && _model.RX_Data2[3] == 0)
                {
                    _model.StandByLamp = false;
                }
            }
            else if (_model.Error_Flag3 == false && _deviceId != "3")
            {
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - MAIN PUMP
                if (!_model.Overload && (_model.RX_Data3[1] == 1 && _model.RX_Data3[6] == 1))
                {
                    if (_model.ResetButton)
                    {
                        _model.StandByLamp = true; // CHANGE MAIN PUMP (MAIN -> STBY)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // AFTER LOW PRESSURE / OVERLOAD / POWER FAIL - ST'BY PUMP
                else if (_model.RunLamp && _model.StandByLamp)
                {
                    if (_model.RX_Data3[1] == 0 && _model.RX_Data3[5] == 1)
                    {
                        _model.StandByLamp = false; // CHANGE MAIN PUMP (STBY -> MAIN)
                        if (_model.StandBy_3_1RUN_Flag == true)
                        {
                            _model.StandBy_3_1RUN_Flag = false;
                        }
                    }
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN / ST'BY PUMP : STAND BY LAMP ON
                else if (_model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 1 && !_model.Overload && !_model.RunLamp && !_model.InitFlag && _model.StopLamp)
                {
                    _model.StandByLamp = true;
                }
                // NORMAL - MAIN PUMP : STBY MODE & RUN -> STOP / ST'BY PUMP : STAND BY LAMP ON -> OFF
                else if (_model.StandBy_3_1RUN_Flag == false && _model.RX_Data3[1] == 0 && _model.RX_Data3[2] == 0 && _model.RX_Data3[3] == 1 && !_model.RunLamp)
                {
                    _model.StandByLamp = false;
                }
                // NORMAL - MAIN PUMP : MANUAL MODE & RUN / ST'BY PUMP : STAND BY LAMP OFF
                else if (_model.RX_Data3[1] == 1 && _model.RX_Data3[3] == 0)
                {
                    _model.StandByLamp = false;
                }
            }
        }
        ////////////////////////////StandByLampProc 관련 로직////////////////////////////////////
        #endregion

        #region 외부 인터페이스 메서드
        /// <summary>
        /// 시작 버튼 누름 처리
        /// </summary>
        public void PressStartButton()
        {
            if (_model == null) return;

            if (!_model.Overload)
            {
                if (!_model.RunStatus)
                {
                    _model.RunStatus = true;
                    _model.ResetButton = false;

                    // 상태 변경 시 로직 실행
                    ExecutePSTARLogic();
                }
            }
        }

        /// <summary>
        /// 정지 버튼 누름 처리
        /// </summary>
        public void PressStopButton()
        {
            if (_model == null) return;

            _model.RunStatus = false;
            _model.ResetButton = true;

            // 상태 변경 시 로직 실행
            ExecutePSTARLogic();
        }

        /// <summary>
        /// 모드 버튼 누름 처리
        /// </summary>
        public void PressModeButton()
        {
            if (_model == null) return;

            _model.IsManualMode = !_model.IsManualMode; // Manual 램프 toggle

            // 상태 변경 시 로직 실행
            ExecutePSTARLogic();
        }

        /// <summary>
        /// 히트 버튼 누름 처리
        /// </summary>
        public void PressHeatButton()
        {
            if (_model == null) return;

            _model.HeatStatus = !_model.HeatStatus;
            _model.IsHeatOn = !_model.IsHeatOn; //Heat 램프 toggle

            // 상태 변경 시 로직 실행
            ExecutePSTARLogic();
        }

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
        /// </summary>
        private void NotifyStateChanged()
        {
            if (_model == null) return;

            DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs
            {
                DeviceId = _deviceId,
                IsRunning = _model.RunStatus,
                IsStandByMode = _model.ModeStatus,
                IsHeating = _model.HeatStatus,
                IsStandByLamp = _model.StandByLamp
            });
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