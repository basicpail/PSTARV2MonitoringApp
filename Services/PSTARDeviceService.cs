using System;
using System.Timers;
using PSTARV2MonitoringApp.Models;
using Timer = System.Timers.Timer;

namespace PSTARV2MonitoringApp.Services
{
    /// <summary>
    /// 실제 펌웨어 로직을 구현한 PSTAR 펌프 모델
    /// </summary>
    public class PSTARDeviceService : IDisposable
    {
        #region 변수
        // 장치 ID와 CAN ID
        private readonly string _deviceId;
        private readonly uint _canId;

        // CAN 전송 타이머 (로직 타이머는 제거)
        private readonly Timer _transmitTimer;

        // 모델 레퍼런스 (장치 모델 공유)
        private PSTARDeviceModel _model;
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

            // CAN 전송 타이머 (300ms 주기 - PSTARFW.c의 CanDelay_mS)
            _transmitTimer = new Timer(300);
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
            // 모델 상태가 변경되면 펌프 로직 실행
            ExecutePumpLogic();

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
            ExecutePumpLogic();
        }

        /// <summary>
        /// 시뮬레이션 중지
        /// </summary>
        public void StopSimulation()
        {
            _transmitTimer.Stop();
        }

        /// <summary>
        /// 펌프 로직 실행 (이전 OnLogicTimerElapsed의 내용)
        /// </summary>
        private void ExecutePumpLogic()
        {
            if (_model == null) return;

            // PSTARFW.c의 메인 루프 로직 구현
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
            // KeyProc, OverloadProc, LowpressProc 등 입력 처리 로직 구현
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

            // LowpressProc 로직 구현
            _model.TxLowpressInternal = lowpressStatus;
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
            if (_model == null) return;

            if (!_model.Overload && _model.StandByLamp)
            {
                if (!_model.Request_Flag &&
                    (_model.RX_Data1[4] == 1 || _model.RX_Data2[4] == 1 || _model.RX_Data3[4] == 1))
                {
                    _model.Request_Flag = true;
                    _model.RunStatus = true;
                }
                else if (_model.RX_Data1[4] == 0 && _model.RX_Data2[4] == 0 && _model.RX_Data3[4] == 0)
                {
                    _model.Request_Flag = false;
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
            if (_model == null) return;

            // StandByLampProc 로직 구현 (매우 복잡한 로직)
            // 간소화된 버전 구현
            //if (modeStatus) // STBY_MODE
            //{
            //    _model.StandByLamp = true;
            //}
            //else // MANUAL_MODE
            //{
            //    _model.StandByLamp = false;
            //}
        }
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
                    if (!_model.ModeStatus) // MANUAL_MODE
                    {
                        _model.RunStatus = true;

                        // 상태 변경 시 로직 실행
                        ExecutePumpLogic();
                    }
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
            ExecutePumpLogic();
        }

        /// <summary>
        /// 모드 버튼 누름 처리
        /// </summary>
        public void PressModeButton()
        {
            if (_model == null) return;

            //_model.ModeStatus = !_model.ModeStatus;
            _model.IsManualMode = !_model.IsManualMode; // Manual 램프 toggle

            // 상태 변경 시 로직 실행
            ExecutePumpLogic();
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
            ExecutePumpLogic();
        }

        /// <summary>
        /// 과부하 상태 설정
        /// </summary>
        public void SetOverload(bool isOverload)
        {
            if (_model == null) return;

            _model.Overload_I = isOverload;

            // 상태 변경 시 로직 실행
            ExecutePumpLogic();
        }

        /// <summary>
        /// 저압 상태 설정
        /// </summary>
        public void SetLowPressure(bool isLowPressure)
        {
            if (_model == null) return;

            _model.Lowpress_I = isLowPressure;

            // 상태 변경 시 로직 실행
            ExecutePumpLogic();
        }

        /// <summary>
        /// 상태 변경 알림
        /// </summary>
        private void NotifyStateChanged()
        {
            if (_model == null) return;

            DeviceStateChanged?.Invoke(this, new DeviceStateChangedEventArgs
            {
                //TODO실제 장치 동작 로직에 맞춰서 수정해야한디
                //DeviceId = _deviceId,
                //IsRunning = _model.RunStatus,
                //IsStandByMode = _model.ModeStatus,
                //IsHeating = _model.HeatStatus,
                //IsStandByLamp = _model.StandByLamp
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