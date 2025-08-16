using System;
using System.ComponentModel;

namespace PSTARV2MonitoringApp.Models
{
    /// <summary>
    /// ��ġ ���� �α� ��Ʈ��
    /// </summary>
    public class DeviceLogEntry : INotifyPropertyChanged
    {
        private DateTime _date;
        private string _id;
        private string _contents;

        public DateTime Date
        {
            get => _date;
            set
            {
                _date = value;
                OnPropertyChanged(nameof(Date));
            }
        }

        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public string Contents
        {
            get => _contents;
            set
            {
                _contents = value;
                OnPropertyChanged(nameof(Contents));
            }
        }

        public DeviceLogEntry(string deviceId, string action)
        {
            Date = DateTime.Now;
            Id = deviceId;
            Contents = action;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}