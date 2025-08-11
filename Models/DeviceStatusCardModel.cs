using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PSTARV2MonitoringApp.Models
{
    public class DeviceStatusCardModel : INotifyPropertyChanged
    {
        private string _deviceId;
        private string _commStatus;
        private string _runStatus;
        private string _runMode;
        private string _standByStatus;
        private string _overloadStatus;
        private string _lowPressureStatus;

        public string DeviceId
        {
            get => _deviceId;
            set => SetProperty(ref _deviceId, value);
        }

        public string CommStatus
        {
            get => _commStatus;
            set => SetProperty(ref _commStatus, value);
        }

        public string RunStatus
        {
            get => _runStatus;
            set => SetProperty(ref _runStatus, value);
        }

        public string RunMode
        {
            get => _runMode;
            set => SetProperty(ref _runMode, value);
        }

        public string StandByStatus
        {
            get => _standByStatus;
            set => SetProperty(ref _standByStatus, value);
        }

        public string OverloadStatus
        {
            get => _overloadStatus;
            set => SetProperty(ref _overloadStatus, value);
        }

        public string LowPressureStatus
        {
            get => _lowPressureStatus;
            set => SetProperty(ref _lowPressureStatus, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
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