using PSTARV2MonitoringApp.Models;
using System.Windows.Controls;

namespace PSTARV2MonitoringApp.Views.Dialogs
{
    public partial class CANSettingsDialog : UserControl
    {
        public CANSettingsDialog()
        {
            InitializeComponent();
        }

        public void LoadSettings(CANSettings settings)
        {
            // 인터페이스 타입 설정
            foreach (ComboBoxItem item in InterfaceTypeComboBox.Items)
            {
                if (item.Tag.ToString() == settings.InterfaceType)
                {
                    InterfaceTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // 채널 설정
            foreach (ComboBoxItem item in ChannelComboBox.Items)
            {
                if (item.Tag.ToString() == settings.Channel)
                {
                    ChannelComboBox.SelectedItem = item;
                    break;
                }
            }

            // 전송 속도 설정
            foreach (ComboBoxItem item in BaudRateComboBox.Items)
            {
                if (int.Parse(item.Tag.ToString()) == settings.BaudRate)
                {
                    BaudRateComboBox.SelectedItem = item;
                    break;
                }
            }

            // 기본 ID 설정
            BaseIdTextBox.Text = settings.DeviceBaseId.ToString("X");

            // 상태 표시
            StatusTextBlock.Text = settings.IsConnected ? "연결됨" : "연결 안됨";
            StatusTextBlock.Foreground = settings.IsConnected ? 
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green) :
                new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
        }

        public void ApplySettings(CANSettings settings)
        {
            if (InterfaceTypeComboBox.SelectedItem is ComboBoxItem interfaceItem)
            {
                settings.InterfaceType = interfaceItem.Tag.ToString();
            }

            if (ChannelComboBox.SelectedItem is ComboBoxItem channelItem)
            {
                settings.Channel = channelItem.Tag.ToString();
            }

            if (BaudRateComboBox.SelectedItem is ComboBoxItem baudItem)
            {
                settings.BaudRate = int.Parse(baudItem.Tag.ToString());
            }

            if (uint.TryParse(BaseIdTextBox.Text, System.Globalization.NumberStyles.HexNumber, null, out uint baseId))
            {
                settings.DeviceBaseId = baseId;
            }
        }
    }
}