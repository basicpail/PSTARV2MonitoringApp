using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PSTARV2MonitoringApp.Models
{
    /// <summary>
    /// PSTAR 장치 모델 클래스 - 데이터만 보관하는 역할
    /// </summary>
    public class PSTARDeviceModel : INotifyPropertyChanged
    {
        #region 필드 - 장치 기본 정보
        private string _deviceId;
        private string _deviceModel;
        #endregion

        #region 필드 - UI 표시용 상태 정보
        private bool _sourceLamp;
        private bool _abnormalLamp;
        private bool _standbyLamp;
        private bool _stopLamp; // StopLamp는 UI 표시용과 CAN 송신용이 동일
        private bool _lowPressLamp;
        private bool _commFaultLamp;
        private bool _heatOnLamp;
        private bool _heatingLamp;
        private bool _modeManualLamp;
        private bool _modeStbyLamp;
        private bool _runLamp; // RunLamp는 UI 표시용과 CAN 송신용이 동일

        #endregion

        #region 필드 - PSTAR 상태 변수
        // PSTAR 동작 상태 변수
        private bool _runStatus;
        private bool _heatStatus;
        private bool _modeStatus;

        // 출력 데이터 (CAN 송신)
        private bool _stby_Start;
        //private bool _runLamp;
        private bool _overload;
        private bool _run_req;
        private bool _resetButton;
        private bool _standByLamp;
        private bool _txLowpress;
        //private bool _stopLamp = true;

        // RunStopProc 필요 변수
        private bool _firstRunStatus;
        private bool _runSig;
        private bool _stopSig;

        // 입력 신호
        private bool _startPB_I;
        private bool _stopPB_I;
        private bool _modePB_I;
        private bool _heatPB_I;
        private bool _runRemote_I;
        private bool _stopRemote_I;
        private bool _overload_I;
        private bool _lowpress_I;

        // 플래그 변수
        private bool _initFlag;
        private bool _request_Flag;
        private bool _stby_Overload;
        private bool _stop_Overload;
        private bool _countOverload_Flag;
        private bool _lowpress;
        private bool _error_Flag1;
        private bool _error_Flag2;
        private bool _error_Flag3;
        private bool _comStatus_Flag;
        private bool _comFailLamp_Flag;
        private bool _standBy_3_1RUN_Flag;
        private bool _standBy_2_Flag;

        // 타이머 변수
        private int _countBuildUpTime_S;
        private int _countParallelTime_S;
        private bool _countBuildUpStart;
        private bool _countParaStart;
        private int _buildUpTime;
        private int _parallelTime;
        private int _countSeqTime_mS;
        private int _countBuildUpTime;
        private int _countHeatingOnTime_S;
        private int _heatingOnTime;
        private int _countRunReq_S;
        private int _runReq_S;
        private int _comFault_S;
        private int _countComFault1_S;
        private int _countComFault2_S;
        private int _countComFault3_S;
        private int _countComFailLamp_mS;
        private int _countResetButton_S;
        private int _comInit_S;
        private int _countStandByCheck_mS;
        private int _countStandBy_2_mS;
        private int _countComInit;
        private int _countOverload_S;

        //CAN 전송 주기
        private int _canTransmitInterval;

        // 연결 상태
        private int _comStatus;

        // 수신 데이터 (다른 펌프로부터)
        private byte[] _rx_data1 = new byte[8];
        private byte[] _rx_data2 = new byte[8];
        private byte[] _rx_data3 = new byte[8];

        // 이전 상태 저장 변수
        private bool _oldLowpress;
        private bool _oldRunStatus;
        private bool _oldStartPB;
        private bool _oldStopPB;
        private bool _oldHeatPB;
        private bool _oldModePB;
        
        // UI 표시용 문자열 속성
        private string _stby_Start_String = "OFF";
        #endregion

        #region 속성 - 모든 속성은 단순 getter/setter로 구현
        // 장치 기본 정보
        public string DeviceId
        {
            get => _deviceId;
            set { SetProperty(ref _deviceId, value); }
        }

        public string DeviceModel
        {
            get => _deviceModel;
            set { SetProperty(ref _deviceModel, value); }
        }

        // UI 표시용 상태 정보
        public bool SourceLamp
        {
            get => _sourceLamp;
            set { SetProperty(ref _sourceLamp, value); }
        }

        public bool ABN_LAMP
        {
            get => _abnormalLamp;
            set { SetProperty(ref _abnormalLamp, value); }
        }

        public bool STAND_BY_LAMP
        {
                       get => _standbyLamp;
            set { SetProperty(ref _standbyLamp, value); }
        }

        public bool STOP_LAMP
        {
            get => _stopLamp;
            set { SetProperty(ref _stopLamp, value); }
        }

        public bool LOW_PRESS_LAMP
        {
            get => _lowPressLamp;
            set { SetProperty(ref _lowPressLamp, value); }
        }

        public bool COMM_FAULT_LAMP
        {
            get => _commFaultLamp;
            set { SetProperty(ref _commFaultLamp, value); }
        }

        public bool HEAT_ON_LAMP
        {
            get => _heatOnLamp;
            set { SetProperty(ref _heatOnLamp, value); }
        }

        public bool HEATING_LAMP
        {
            get => _heatingLamp;
            set { SetProperty(ref _heatingLamp, value); }
        }

        public bool MODE_MANUAL_LAMP
        {
            get => _modeManualLamp;
            set { SetProperty(ref _modeManualLamp, value); }
        }

        public bool MODE_STBY_LAMP
        {
            get => _modeStbyLamp;
            set { SetProperty(ref _modeStbyLamp, value); }
        }

        public bool RUN_LAMP
        {
            get => _runLamp;
            set { SetProperty(ref _runLamp, value); }
        }


        // PSTAR 동작 상태 변수
        public bool RunStatus
        {
            get => _runStatus;
            set { SetProperty(ref _runStatus, value); }
        }

        public bool HeatStatus
        {
            get => _heatStatus;
            set { SetProperty(ref _heatStatus, value); }
        }

        public bool ModeStatus
        {
            get => _modeStatus;
            set { SetProperty(ref _modeStatus, value); }
        }

        // 출력 데이터 (CAN 송신)
        public bool STBY_Start
        {
            get => _stby_Start;
            set { SetProperty(ref _stby_Start, value); }
        }

        public bool RunLamp
        {
            get => _runLamp;
            set { SetProperty(ref _runLamp, value); }
        }

        public bool Overload
        {
            get => _overload;
            set { SetProperty(ref _overload, value); }
        }

        public bool RUN_req
        {
            get => _run_req;
            set { SetProperty(ref _run_req, value); }
        }

        public bool ResetButton
        {
            get => _resetButton;
            set { SetProperty(ref _resetButton, value); }
        }

        public bool StandByLamp
        {
            get => _standByLamp;
            set { SetProperty(ref _standByLamp, value); }
        }

        public bool TXLowpress
        {
            get => _txLowpress;
            set { SetProperty(ref _txLowpress, value); }
        }

        public bool StopLamp
        {
            get => _stopLamp;
            set { SetProperty(ref _stopLamp, value); }
        }

        // RunStopProc 필요 변수
        public bool FirstRunStatus
        {
            get => _firstRunStatus;
            set { SetProperty(ref _firstRunStatus, value); }
        }

        public bool RunSig
        {
            get => _runSig;
            set { SetProperty(ref _runSig, value); }
        }

        public bool StopSig
        {
            get => _stopSig;
            set { SetProperty(ref _stopSig, value); }
        }


        // 입력 신호
        public bool START_PB_I
        {
            get => _startPB_I;
            set { SetProperty(ref _startPB_I, value); }
        }
        public bool STOP_PB_I
        {
            get => _stopPB_I;
            set { SetProperty(ref _stopPB_I, value); }
        }
        public bool MODE_PB_I
        {
            get => _modePB_I;
            set { SetProperty(ref _modePB_I, value); }
        }
        public bool HEAT_PB_I
        {
            get => _heatPB_I;
            set { SetProperty(ref _heatPB_I, value); }
        }
        public bool RunRemote_I
        {
            get => _runRemote_I;
            set { SetProperty(ref _runRemote_I, value); }
        }

        public bool StopRemote_I
        {
            get => _stopRemote_I;
            set { SetProperty(ref _stopRemote_I, value); }
        }

        public bool Overload_I
        {
            get => _overload_I;
            set { SetProperty(ref _overload_I, value); }
        }

        public bool Lowpress_I
        {
            get => _lowpress_I;
            set { SetProperty(ref _lowpress_I, value); }
        }

        // 플래그 변수
        public bool Request_Flag
        {
            get => _request_Flag;
            set { SetProperty(ref _request_Flag, value); }
        }

        public bool STBY_Overload
        {
            get => _stby_Overload;
            set { SetProperty(ref _stby_Overload, value); }
        }

        public bool Stop_Overload
        {
            get => _stop_Overload;
            set { SetProperty(ref _stop_Overload, value); }
        }

        public bool Lowpress
        {
            get => _lowpress;
            set { SetProperty(ref _lowpress, value); }
        }

        public bool Error_Flag1
        {
            get => _error_Flag1;
            set { SetProperty(ref _error_Flag1, value); }
        }

        public bool Error_Flag2
        {
            get => _error_Flag2;
            set { SetProperty(ref _error_Flag2, value); }
        }

        public bool Error_Flag3
        {
            get => _error_Flag3;
            set { SetProperty(ref _error_Flag3, value); }
        }

        public bool InitFlag
        {
            get => _initFlag;
            set { SetProperty(ref _initFlag, value); }
        }
        
        public bool ComStatus_Flag
        {
            get => _comStatus_Flag;
            set { SetProperty(ref _comStatus_Flag, value); }
        }
        public bool ComFailLamp_Flag
        {
            get => _comFailLamp_Flag;
            set { SetProperty(ref _comFailLamp_Flag, value); }
        }

        // 타이머 변수
        public int CountBuildUpTime_S
        {
            get => _countBuildUpTime_S;
            set { SetProperty(ref _countBuildUpTime_S, value); }
        }

        public int CountParallelTime_S
        {
            get => _countParallelTime_S;
            set { SetProperty(ref _countParallelTime_S, value); }
        }

        public bool CountBuildUpStart
        {
            get => _countBuildUpStart;
            set { SetProperty(ref _countBuildUpStart, value); }
        }

        public bool CountParaStart
        {
            get => _countParaStart;
            set { SetProperty(ref _countParaStart, value); }
        }

        public int BuildUpTime
        {
            get => _buildUpTime;
            set { SetProperty(ref _buildUpTime, value); }
        }

        public int ParallelTime
        {
            get => _parallelTime;
            set { SetProperty(ref _parallelTime, value); }
        }

        public int CountSeqTime_mS
        {
            get => _countSeqTime_mS;
            set { SetProperty(ref _countSeqTime_mS, value); }
        }

        // 연결 상태
        public int ComStatus
        {
            get => _comStatus;
            set { SetProperty(ref _comStatus, value); }
        }

        // 수신 데이터 (다른 펌프로부터)
        public byte[] RX_Data1
        {
            get => _rx_data1;
            set { SetProperty(ref _rx_data1, value); }
        }

        public byte[] RX_Data2
        {
            get => _rx_data2;
            set { SetProperty(ref _rx_data2, value); }
        }

        public byte[] RX_Data3
        {
            get => _rx_data3;
            set { SetProperty(ref _rx_data3, value); }
        }

        // 추가된 속성 (PSTARFW.c 기반)
        public bool OldLowpress
        {
            get => _oldLowpress;
            set { SetProperty(ref _oldLowpress, value); }
        }

        public bool OldRunStatus
        {
            get => _oldRunStatus;
            set { SetProperty(ref _oldRunStatus, value); }
        }

        public bool OldStartPB
        {
            get => _oldStartPB;
            set { SetProperty(ref _oldStartPB, value); }
        }

        public bool OldStopPB
        {
            get => _oldStopPB;
            set { SetProperty(ref _oldStopPB, value); }
        }
        public bool OldHeatPB
        {
            get => _oldHeatPB;
            set { SetProperty(ref _oldHeatPB, value); }
        }
        public bool OldModePB
        {
            get => _oldModePB;
            set { SetProperty(ref _oldModePB, value); }
        }

        public int CountBuildUpTime
        {
            get => _countBuildUpTime;
            set { SetProperty(ref _countBuildUpTime, value); }
        }

        public int CountHeatingOnTime_S
        {
            get => _countHeatingOnTime_S;
            set { SetProperty(ref _countHeatingOnTime_S, value); }
        }

        public int HeatingOnTime
        {
            get => _heatingOnTime;
            set { SetProperty(ref _heatingOnTime, value); }
        }
        
        public int CountRunReq_S
        {
            get => _countRunReq_S;
            set { SetProperty(ref _countRunReq_S, value); }
        }
        public int RunReq_S
        {
            get => _runReq_S;
            set { SetProperty(ref _runReq_S, value); }
        }
        public int ComFault_S
        {
            get => _comFault_S;
            set { SetProperty(ref _comFault_S, value); }
        }

        public int CountComFault1_S
        {
            get => _countComFault1_S;
            set { SetProperty(ref _countComFault1_S, value); }
        }

        public int CountComFault2_S
        {
            get => _countComFault2_S;
            set { SetProperty(ref _countComFault2_S, value); }
        }

        public int CountComFault3_S
        {
            get => _countComFault3_S;
            set { SetProperty(ref _countComFault3_S, value); }
        }
        
        public int CountComFailLamp_mS
        {
            get => _countComFailLamp_mS;
            set { SetProperty(ref _countComFailLamp_mS, value); }
        }
        
        public int CountResetButton_S
        {
            get => _countResetButton_S;
            set { SetProperty(ref _countResetButton_S, value); }
        }
        public int CountComInit
        {
            get => _countComInit; 
            set { SetProperty(ref _countComInit, value); }
        }
        public int CountOverload_S
        {
            get => _countOverload_S; 
            set { SetProperty(ref _countOverload_S, value); }
        }
        public int ComInit_S
        {
            get => _comInit_S;
            set { SetProperty(ref _comInit_S, value); }
            
        }
        
        public int CountStandByCheck_mS
        {
            get => _countStandByCheck_mS;
            set { SetProperty(ref _countStandByCheck_mS, value); }
        }
        public int CountStandBy_2_mS
        {
            get => _countStandBy_2_mS;
            set { SetProperty(ref _countStandBy_2_mS, value); }
        }
        public int CANTransmitInterval
        {
            get => _canTransmitInterval;
            set { SetProperty(ref _canTransmitInterval, value); }
        }
        
        public bool StandBy_3_1RUN_Flag
        {
            get => _standBy_3_1RUN_Flag;
            set { SetProperty(ref _standBy_3_1RUN_Flag, value); }
        }

        public bool StandBy_2_Flag
        {
            get => _standBy_2_Flag;
            set { SetProperty(ref _standBy_2_Flag, value); }
        }

        public bool CountOverload_Flag
        {
            get => _countOverload_Flag;
            set { SetProperty(ref _countOverload_Flag, value); }
        }

        // UI 표시용 문자열 속성
        public string STBY_Start_String
        {
            get => _stby_Start_String;
            set { SetProperty(ref _stby_Start_String, value); }
        }
        #endregion

        #region 이벤트
        // 모델 속성 변경 이벤트
        public event PropertyChangedEventHandler PropertyChanged;

        // 서비스 계층에서 상태 변경을 감지하기 위한 이벤트
        public event EventHandler StateChanged;
        #endregion

        #region 생성자
        // 초기값 설정을 위한 생성자
        public PSTARDeviceModel()
        {
            DeviceId = "Unknown";
            DeviceModel = "Unknown";
            InitializeDefaultValues();
        }

        // 특정 ID와 모델로 초기화하는 생성자
        public PSTARDeviceModel(string deviceId, string deviceModel)
        {
            DeviceId = deviceId;
            DeviceModel = deviceModel;
            InitializeDefaultValues();
        }

        // 기본값 초기화 메서드
        private void InitializeDefaultValues()
        {
            // UI 상태 초기화
            SourceLamp = true;
            ABN_LAMP = false;
            RUN_LAMP = false;
            STOP_LAMP = true;
            HEATING_LAMP = false;
            HEAT_ON_LAMP = false;
            COMM_FAULT_LAMP = false;
            LOW_PRESS_LAMP = false;
            STAND_BY_LAMP = false;
            MODE_MANUAL_LAMP = true;
            MODE_STBY_LAMP = false;

            // PSTAR 상태 초기화
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
            Lowpress = false;
            StopLamp = true;
            FirstRunStatus = false;
            RunSig = false;
            StopSig = false;

            // 입력 신호 초기화
            START_PB_I = false;
            OldStartPB = false;
            STOP_PB_I = false;
            MODE_PB_I = false;
            OldStopPB = false;
            OldHeatPB = false;
            OldModePB = false;
            RunRemote_I = false;
            StopRemote_I = false;
            Overload_I = false;
            Lowpress_I = false;

            // 플래그 변수 초기화
            Request_Flag = false;
            STBY_Overload = false;
            Stop_Overload = false;
            Error_Flag1 = false;
            Error_Flag2 = false;
            Error_Flag3 = false;
            InitFlag = false;
            ComStatus_Flag = false;
            CountOverload_Flag = false;
            ComFailLamp_Flag = false;
            StandBy_3_1RUN_Flag = false;
            StandBy_2_Flag = false;

            // 타이머 변수 초기화
            //TMR0 는 100ms 주기이다.
            CountBuildUpTime_S = 0;
            CountParallelTime_S = 0;
            CountBuildUpStart = false;
            CountParaStart = false;
            BuildUpTime = 5;
            ParallelTime = 10;
            CountSeqTime_mS = 0;
            CountBuildUpTime = 0;
            CountHeatingOnTime_S = 0;
            HeatingOnTime = 3;
            CountRunReq_S = 0;
            RunReq_S = 1; // Overload Run Req Count : 1sec
            ComFault_S = 1; // Com Fault(Power Fail) Count : 1sec
            CountComFault1_S = 0;
            CountComFault2_S = 0;
            CountComFault3_S = 0;
            CountComFailLamp_mS = 0;
            CountResetButton_S = 0;
            CountComInit = 0; // First StandBy Status Control : 0s
            ComInit_S = 0;
            CountStandByCheck_mS = 0;
            CountStandBy_2_mS = 0;
            CountOverload_S = 0;
            CANTransmitInterval = 300; // CAN 전송 주기 : 300ms

            // 연결 상태 초기화
            ComStatus = 0;

            // 기타 초기화
            STBY_Start_String = "OFF";
            OldLowpress = false;
            OldRunStatus = false;
        }
        #endregion

        #region 메서드
        // DeviceStatusCardModel로부터 상태를 업데이트하는 메서드
        public void UpdateFromDeviceStatusCard(DeviceStatusCardModel cardModel)
        {
            if (cardModel == null) return;

            DeviceId = cardModel.DeviceId;

            // CommStatus에 따른 상태 업데이트
            COMM_FAULT_LAMP = cardModel.CommStatus != "Connected";

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
        #endregion

        #region INotifyPropertyChanged 구현
        // 속성 변경 알림
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