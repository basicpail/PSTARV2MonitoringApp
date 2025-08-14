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
        // 장치 기본 정보
        private string _deviceId;
        private string _deviceModel;

        // 장치 상태 정보
        private bool _isSourceOn;
        private bool _isAbnormal;
        private bool _isRunning;
        private bool _isStopped;
        private bool _isHeating;
        private bool _isCommFailure;
        private bool _isLowPressure;
        private bool _isStandby;
        private bool _isOn;
        private bool _isManualMode;

        // 장치 기본 정보 프로퍼티
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

        // 상태 램프 관련 프로퍼티
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
            set => SetProperty(ref _isRunning, value);
        }

        public bool IsStopped
        {
            get => _isStopped;
            set => SetProperty(ref _isStopped, value);
        }

        public bool IsHeating
        {
            get => _isHeating;
            set => SetProperty(ref _isHeating, value);
        }

        public bool IsCommFailure
        {
            get => _isCommFailure;
            set => SetProperty(ref _isCommFailure, value);
        }

        public bool IsLowPressure
        {
            get => _isLowPressure;
            set => SetProperty(ref _isLowPressure, value);
        }

        public bool IsStandby
        {
            get => _isStandby;
            set => SetProperty(ref _isStandby, value);
        }

        public bool IsOn
        {
            get => _isOn;
            set => SetProperty(ref _isOn, value);
        }

        public bool IsManualMode
        {
            get => _isManualMode;
            set => SetProperty(ref _isManualMode, value);
        }

        // 버튼 상태
        private string _stby_Start;
        public string STBY_Start
        {
            get => _stby_Start;
            set => SetProperty(ref _stby_Start, value);
        }

        // 초기값 설정을 위한 생성자
        public PSTARDevicePanelModel()
        {
            // 기본값 설정
            DeviceId = "Unknown";
            DeviceModel = "Unknown";
            IsSourceOn = false;
            IsAbnormal = false;
            IsRunning = false;
            IsStopped = true;
            IsHeating = false;
            IsCommFailure = false;
            IsLowPressure = false;
            IsStandby = false;
            IsOn = false;
            IsManualMode = false;
            STBY_Start = "OFF";
        }

        // 특정 ID와 모델로 초기화하는 생성자
        public PSTARDevicePanelModel(string deviceId, string deviceModel)
        {
            DeviceId = deviceId;
            DeviceModel = deviceModel;
            IsSourceOn = false;
            IsAbnormal = false;
            IsRunning = false;
            IsStopped = false;
            IsHeating = false;
            IsCommFailure = false;
            IsLowPressure = false;
            IsStandby = false;
            IsOn = false;
            IsManualMode = false;
            STBY_Start = "OFF";
        }

        // DeviceStatusCardModel로부터 상태를 업데이트하는 메서드
        public void UpdateFromDeviceStatusCard(DeviceStatusCardModel cardModel)
        {
            if (cardModel == null) return;

            DeviceId = cardModel.DeviceId;

            // CommStatus에 따른 상태 업데이트
            IsCommFailure = cardModel.CommStatus != "Connected";

            // RunStatus에 따른 상태 업데이트
            IsRunning = cardModel.RunStatus == "Running";
            IsStopped = cardModel.RunStatus == "Stopped";

            // RunMode에 따른 상태 업데이트
            IsManualMode = cardModel.RunMode == "Manual";

            // StandByStatus에 따른 상태 업데이트
            IsStandby = cardModel.StandByStatus == "Standby";

            // OverloadStatus에 따른 상태 업데이트
            IsAbnormal = cardModel.OverloadStatus != "Normal";

            // LowPressureStatus에 따른 상태 업데이트
            IsLowPressure = cardModel.LowPressureStatus != "Normal";
        }

        // 상태 변경 이벤트 처리
        public event PropertyChangedEventHandler PropertyChanged;

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
    }
}