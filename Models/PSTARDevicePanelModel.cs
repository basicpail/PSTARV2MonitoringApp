using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PSTARV2MonitoringApp.Models
{
    public class PSTARDevicePanelModel : INotifyPropertyChanged
    {
        #region 필드 - 장치 기본 정보
        private string _deviceId;
        private string _deviceModel;
        #endregion

        #region 필드 - UI 표시용 상태 정보
        private bool _isSourceOn;
        private bool _isAbnormal;
        private bool _isRunning;
        private bool _isStopped;
        private bool _isHeating;
        private bool _isCommFailure;
        private bool _isLowPressure;
        private bool _isStandby;
        private bool _isHeatOn;
        private bool _isStandbyMode;
        private bool _isManualMode;
        #endregion

        #region 필드 - PSTPumpModel 상태 변수 (FW와 동일)
        // PSTAR 동작 상태 변수
        private bool _runStatus = false;       // 0: STOP, 1: RUN
        private bool _heatStatus = false;      // 0: HEAT_OFF, 1: HEAT_ON
        private bool _modeStatus = false;      // 0: MANUAL_MODE, 1: STBY_MODE

        // 출력 데이터 (CAN 송신)
        private bool _stby_Start = false;      // tx_data[0]
        private bool _runLamp = false;         // tx_data[1]
        private bool _overload = false;        // tx_data[2]
        private bool _run_req = false;         // tx_data[4]
        private bool _resetButton = false;     // tx_data[5]
        private bool _standByLamp = false;     // tx_data[6]
        private bool _txLowpress = false;      // tx_data[7]
        private bool _stopLamp = true;         // 정지 램프 상태

        // 입력 신호
        private bool _runFB_I = false;         // 실행 피드백 입력
        private bool _runRemote_I = false;     // 원격 실행 입력
        private bool _stopRemote_I = false;    // 원격 정지 입력
        private bool _overload_I = false;      // 과부하 입력
        private bool _lowpress_I = false;      // 저압 입력

        // 플래그 변수
        private bool _request_Flag = false;    // 실행 요청 플래그
        private bool _stby_Overload = false;   // STBY 과부하 플래그
        private bool _stop_Overload = false;   // 정지 과부하 플래그
        private bool _txLowpressInternal = false; // 내부 저압 상태
        private bool _error_Flag1 = false;     // ID 1 오류 플래그
        private bool _error_Flag2 = false;     // ID 2 오류 플래그
        private bool _error_Flag3 = false;     // ID 3 오류 플래그
        private bool _initFlag = false;        // 초기화 플래그
        private bool _comStatus_Flag = false;  // 연결 상태 플래그

        // 타이머 변수
        private int _countBuildUpTime_S = 0;   // BuildUp 시간 카운터
        private int _countParallelTime_S = 0;  // Parallel 시간 카운터
        private int _countBuildUpStart = 0;    // BuildUp 시작 플래그
        private int _countParaStart = 0;       // Parallel 시작 플래그
        private int _buildUpTime = 5;          // BuildUp 시간 (초)
        private int _parallelTime = 10;        // Parallel 시간 (초)

        // 연결 상태
        private int _comStatus = 0;            // 0: NoConnection, 1: StandBy_3to2, 2: StandBy_2, 3: StandBy_3
                                               // 4: Manual, 5: StandBy_3_1RUN, 6: StandBy_3to2_1RUN

        // 수신 데이터 (다른 펌프로부터)
        private byte[] _rx_data1 = new byte[8];  // ID 1 수신 데이터
        private byte[] _rx_data2 = new byte[8];  // ID 2 수신 데이터
        private byte[] _rx_data3 = new byte[8];  // ID 3 수신 데이터
        #endregion

        #region 속성 - 장치 기본 정보
        public string DeviceId
        {
            get => _deviceId;
            set => SetProperty(ref _deviceId, value);
        }

        public string DeviceModel
        {
            get => _deviceModel;
            set => SetProperty(ref _deviceModel, value);
        }
        #endregion

        #region 속성 - UI 표시용 상태 정보
        public bool IsSourceOn
        {
            get => _isSourceOn;
            set => SetProperty(ref _isSourceOn, value);
        }

        public bool IsAbnormal
        {
            get => _isAbnormal;
            set => SetProperty(ref _isAbnormal, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    // 연동 속성 업데이트
                    if (value && !_runStatus)
                        RunStatus = true;
                }
            }
        }

        public bool IsStopped
        {
            get => _isStopped;
            set
            {
                if (SetProperty(ref _isStopped, value))
                {
                    // 연동 속성 업데이트
                    if (value && _runStatus)
                        RunStatus = false;
                }
            }
        }

        public bool IsHeating
        {
            get => _isHeating;
            set
            {
                if (SetProperty(ref _isHeating, value))
                {
                    // 연동 속성 업데이트
                    if (value != _heatStatus)
                        HeatStatus = value;
                }
            }
        }

        public bool IsCommFailure
        {
            get => _isCommFailure;
            set => SetProperty(ref _isCommFailure, value);
        }

        public bool IsLowPressure
        {
            get => _isLowPressure;
            set
            {
                if (SetProperty(ref _isLowPressure, value))
                {
                    // 연동 속성 업데이트
                    if (value != _lowpress_I)
                        Lowpress_I = value;
                }
            }
        }

        public bool IsStandby
        {
            get => _isStandby;
            set
            {
                if (SetProperty(ref _isStandby, value))
                {
                    // 연동 속성 업데이트
                    if (value != _standByLamp)
                        StandByLamp = value;
                }
            }
        }

        public bool IsHeatOn
        {
            get => _isHeatOn;
            set => SetProperty(ref _isHeatOn, value);
        }
        public bool IsStandbyMode
        {
            get => _isStandbyMode;
            set
            {
                if (SetProperty(ref _isStandbyMode, value))
                {
                    // 연동 속성 업데이트
                    if (value == _modeStatus) // ManualMode는 ModeStatus가 false일 때
                        ModeStatus = !value;
                }
            }
        }

        public bool IsManualMode
        {
            get => _isManualMode;
            set
            {
                if (SetProperty(ref _isManualMode, value))
                {
                    // 연동 속성 업데이트
                    if (value == _modeStatus) // 역관계 (ManualMode는 ModeStatus가 false일 때)
                        ModeStatus = !value;
                }
            }
        }
        #endregion

        #region 속성 - PSTPumpModel 상태 변수
        // PSTAR 동작 상태 변수
        public bool RunStatus
        {
            get => _runStatus;
            set
            {
                if (SetProperty(ref _runStatus, value))
                {
                    // UI 상태도 함께 업데이트
                    _isRunning = value;
                    _isStopped = !value;
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(IsStopped));

                    // 상태 변경 이벤트 발생
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool HeatStatus
        {
            get => _heatStatus;
            set
            {
                if (SetProperty(ref _heatStatus, value))
                {
                    // UI 상태도 함께 업데이트
                    _isHeating = value;
                    OnPropertyChanged(nameof(IsHeating));

                    // 상태 변경 이벤트 발생
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ModeStatus
        {
            get => _modeStatus;
            set
            {
                if (SetProperty(ref _modeStatus, value))
                {
                    // UI 상태도 함께 업데이트
                    _isManualMode = !value; // ModeStatus=true이면 STBY모드, false이면 MANUAL모드
                    OnPropertyChanged(nameof(IsManualMode));

                    // 상태 변경 이벤트 발생
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // 출력 데이터 (CAN 송신)
        public bool STBY_Start
        {
            get => _stby_Start;
            set
            {
                if (SetProperty(ref _stby_Start, value))
                {
                    STBY_Start_String = value ? "ON" : "OFF";
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool RunLamp
        {
            get => _runLamp;
            set
            {
                if (SetProperty(ref _runLamp, value))
                {
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool Overload
        {
            get => _overload;
            set
            {
                if (SetProperty(ref _overload, value))
                {
                    // UI 상태도 함께 업데이트
                    _isAbnormal = value;
                    OnPropertyChanged(nameof(IsAbnormal));
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool RUN_req
        {
            get => _run_req;
            set
            {
                if (SetProperty(ref _run_req, value))
                {
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool ResetButton
        {
            get => _resetButton;
            set
            {
                if (SetProperty(ref _resetButton, value))
                {
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool StandByLamp
        {
            get => _standByLamp;
            set
            {
                if (SetProperty(ref _standByLamp, value))
                {
                    // UI 상태도 함께 업데이트
                    _isStandby = value;
                    OnPropertyChanged(nameof(IsStandby));
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool TXLowpress
        {
            get => _txLowpress;
            set
            {
                if (SetProperty(ref _txLowpress, value))
                {
                    // UI 상태도 함께 업데이트
                    _isLowPressure = value;
                    OnPropertyChanged(nameof(IsLowPressure));
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool StopLamp
        {
            get => _stopLamp;
            set
            {
                if (SetProperty(ref _stopLamp, value))
                {
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // 입력 신호
        public bool RunFB_I
        {
            get => _runFB_I;
            set
            {
                if (SetProperty(ref _runFB_I, value))
                {
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool RunRemote_I
        {
            get => _runRemote_I;
            set
            {
                if (SetProperty(ref _runRemote_I, value))
                {
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool StopRemote_I
        {
            get => _stopRemote_I;
            set
            {
                if (SetProperty(ref _stopRemote_I, value))
                {
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool Overload_I
        {
            get => _overload_I;
            set
            {
                if (SetProperty(ref _overload_I, value))
                {
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool Lowpress_I
        {
            get => _lowpress_I;
            set
            {
                if (SetProperty(ref _lowpress_I, value))
                {
                    StateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // 플래그 변수
        public bool Request_Flag
        {
            get => _request_Flag;
            set => SetProperty(ref _request_Flag, value);
        }

        public bool STBY_Overload
        {
            get => _stby_Overload;
            set => SetProperty(ref _stby_Overload, value);
        }

        public bool Stop_Overload
        {
            get => _stop_Overload;
            set => SetProperty(ref _stop_Overload, value);
        }

        public bool TxLowpressInternal
        {
            get => _txLowpressInternal;
            set => SetProperty(ref _txLowpressInternal, value);
        }

        public bool Error_Flag1
        {
            get => _error_Flag1;
            set => SetProperty(ref _error_Flag1, value);
        }

        public bool Error_Flag2
        {
            get => _error_Flag2;
            set => SetProperty(ref _error_Flag2, value);
        }

        public bool Error_Flag3
        {
            get => _error_Flag3;
            set => SetProperty(ref _error_Flag3, value);
        }

        public bool InitFlag
        {
            get => _initFlag;
            set => SetProperty(ref _initFlag, value);
        }

        public bool ComStatus_Flag
        {
            get => _comStatus_Flag;
            set => SetProperty(ref _comStatus_Flag, value);
        }

        // 타이머 변수
        public int CountBuildUpTime_S
        {
            get => _countBuildUpTime_S;
            set => SetProperty(ref _countBuildUpTime_S, value);
        }

        public int CountParallelTime_S
        {
            get => _countParallelTime_S;
            set => SetProperty(ref _countParallelTime_S, value);
        }

        public int CountBuildUpStart
        {
            get => _countBuildUpStart;
            set => SetProperty(ref _countBuildUpStart, value);
        }

        public int CountParaStart
        {
            get => _countParaStart;
            set => SetProperty(ref _countParaStart, value);
        }

        public int BuildUpTime
        {
            get => _buildUpTime;
            set => SetProperty(ref _buildUpTime, value);
        }

        public int ParallelTime
        {
            get => _parallelTime;
            set => SetProperty(ref _parallelTime, value);
        }

        // 연결 상태
        public int ComStatus
        {
            get => _comStatus;
            set => SetProperty(ref _comStatus, value);
        }

        // 수신 데이터 (다른 펌프로부터)
        public byte[] RX_Data1
        {
            get => _rx_data1;
            set => SetProperty(ref _rx_data1, value);
        }

        public byte[] RX_Data2
        {
            get => _rx_data2;
            set => SetProperty(ref _rx_data2, value);
        }

        public byte[] RX_Data3
        {
            get => _rx_data3;
            set => SetProperty(ref _rx_data3, value);
        }
        #endregion

        #region 추가 속성 - UI 표시용
        // UI 표시용 문자열 속성
        private string _stby_Start_String = "OFF";
        public string STBY_Start_String
        {
            get => _stby_Start_String;
            set => SetProperty(ref _stby_Start_String, value);
        }
        #endregion

        #region 이벤트
        // 상태 변경 이벤트 - 로직 처리가 필요할 때 발생
        public event EventHandler StateChanged;

        // PropertyChanged 이벤트
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region 생성자
        // 초기값 설정을 위한 생성자
        public PSTARDevicePanelModel()
        {
            // 기본값 설정
            DeviceId = "Unknown";
            DeviceModel = "Unknown";
            InitializeDefaultValues();
        }

        // 특정 ID와 모델로 초기화하는 생성자
        public PSTARDevicePanelModel(string deviceId, string deviceModel)
        {
            DeviceId = deviceId;
            DeviceModel = deviceModel;
            InitializeDefaultValues();
        }

        // 기본값 초기화 메서드
        private void InitializeDefaultValues()
        {
            // UI 상태 초기화
            IsSourceOn = true;
            IsAbnormal = false;
            IsRunning = false;
            IsStopped = true;
            IsHeating = false;
            IsCommFailure = false;
            IsLowPressure = false;
            IsStandby = false;
            IsHeatOn = false;
            IsStandbyMode = false;
            IsManualMode = true;

            // PSTPump 상태 초기화
            RunStatus = false;
            HeatStatus = false;
            ModeStatus = false;
            STBY_Start = false;
            RunLamp = false;
            Overload = false;
            RUN_req = false;
            ResetButton = false;
            StandByLamp = false;
            TXLowpress = false;
            StopLamp = true;

            // 입력 신호 초기화
            RunFB_I = false;
            RunRemote_I = false;
            StopRemote_I = false;
            Overload_I = false;
            Lowpress_I = false;

            // 플래그 변수 초기화
            Request_Flag = false;
            STBY_Overload = false;
            Stop_Overload = false;
            TxLowpressInternal = false;
            Error_Flag1 = false;
            Error_Flag2 = false;
            Error_Flag3 = false;
            InitFlag = false;
            ComStatus_Flag = false;

            // 타이머 변수 초기화
            CountBuildUpTime_S = 0;
            CountParallelTime_S = 0;
            CountBuildUpStart = 0;
            CountParaStart = 0;
            BuildUpTime = 5;
            ParallelTime = 10;

            // 연결 상태 초기화
            ComStatus = 0;

            // 기타 초기화
            STBY_Start_String = "OFF";
        }
        #endregion

        #region 메서드
        // DeviceStatusCardModel로부터 상태를 업데이트하는 메서드
        public void UpdateFromDeviceStatusCard(DeviceStatusCardModel cardModel)
        {
            if (cardModel == null) return;

            DeviceId = cardModel.DeviceId;

            // CommStatus에 따른 상태 업데이트
            IsCommFailure = cardModel.CommStatus != "Connected";

            // RunStatus에 따른 상태 업데이트
            bool isRunning = cardModel.RunStatus == "Running";
            RunStatus = isRunning; // 자동으로 IsRunning, IsStopped가 업데이트됨

            // RunMode에 따른 상태 업데이트
            bool isManual = cardModel.RunMode == "Manual";
            ModeStatus = !isManual; // ModeStatus=true이면 STBY모드, false이면 MANUAL모드

            // StandByStatus에 따른 상태 업데이트
            bool isStandby = cardModel.StandByStatus == "Standby";
            StandByLamp = isStandby; // 자동으로 IsStandby가 업데이트됨

            // OverloadStatus에 따른 상태 업데이트
            bool isAbnormal = cardModel.OverloadStatus != "Normal";
            Overload = isAbnormal; // 자동으로 IsAbnormal이 업데이트됨
            Overload_I = isAbnormal;

            // LowPressureStatus에 따른 상태 업데이트
            bool isLowPressure = cardModel.LowPressureStatus != "Normal";
            TXLowpress = isLowPressure; // 자동으로 IsLowPressure가 업데이트됨
            Lowpress_I = isLowPressure;
        }

        // CAN 프레임 데이터 처리
        public void ProcessReceivedCANFrame(CANFrame frame)
        {
            uint id = frame.Id;

            // CAN ID 기반으로 수신 데이터 저장
            if (id == 0x100 && DeviceId != "1")
            {
                Array.Copy(frame.Data, RX_Data1, Math.Min(frame.Data.Length, 8));
                Error_Flag1 = false;  // COM Fault 카운터 리셋
                StateChanged?.Invoke(this, EventArgs.Empty); // 로직 처리 필요
            }
            else if (id == 0x200 && DeviceId != "2")
            {
                Array.Copy(frame.Data, RX_Data2, Math.Min(frame.Data.Length, 8));
                Error_Flag2 = false;  // COM Fault 카운터 리셋
                StateChanged?.Invoke(this, EventArgs.Empty); // 로직 처리 필요
            }
            else if (id == 0x300 && DeviceId != "3")
            {
                Array.Copy(frame.Data, RX_Data3, Math.Min(frame.Data.Length, 8));
                Error_Flag3 = false;  // COM Fault 카운터 리셋
                StateChanged?.Invoke(this, EventArgs.Empty); // 로직 처리 필요
            }
        }

        // 상태 변경 이벤트 처리
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        #endregion
    }
}