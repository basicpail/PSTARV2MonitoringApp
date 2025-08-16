using System.ComponentModel;

namespace PSTARV2MonitoringApp.Models
{
    /// <summary>
    /// CAN ��� ����
    /// </summary>
    public class CANSettings : INotifyPropertyChanged
    {
        private string _interfaceType = "PCAN";
        private string _channel = "PCAN_USBBUS1";
        private int _baudRate = 500000; // 500K
        private uint _deviceBaseId = 0x100;
        private bool _isConnected = false;

        /// <summary>
        /// CAN �������̽� Ÿ�� (PCAN, Vector, SocketCAN ��)
        /// </summary>
        public string InterfaceType
        {
            get => _interfaceType;
            set
            {
                _interfaceType = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// CAN ä��
        /// </summary>
        public string Channel
        {
            get => _channel;
            set
            {
                _channel = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ���� �ӵ� (bps)
        /// </summary>
        public int BaudRate
        {
            get => _baudRate;
            set
            {
                _baudRate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ��ġ �⺻ CAN ID (0x100, 0x101, 0x102...)
        /// </summary>
        public uint DeviceBaseId
        {
            get => _deviceBaseId;
            set
            {
                _deviceBaseId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ���� ����
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}