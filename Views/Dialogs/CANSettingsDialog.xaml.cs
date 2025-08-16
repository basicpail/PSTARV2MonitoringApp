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
            // �������̽� Ÿ�� ����
            foreach (ComboBoxItem item in InterfaceTypeComboBox.Items)
            {
                if (item.Tag.ToString() == settings.InterfaceType)
                {
                    InterfaceTypeComboBox.SelectedItem = item;
                    break;
                }
            }

            // ä�� ����
            foreach (ComboBoxItem item in ChannelComboBox.Items)
            {
                if (item.Tag.ToString() == settings.Channel)
                {
                    ChannelComboBox.SelectedItem = item;
                    break;
                }
            }

            // ���� �ӵ� ����
            foreach (ComboBoxItem item in BaudRateComboBox.Items)
            {
                if (int.Parse(item.Tag.ToString()) == settings.BaudRate)
                {
                    BaudRateComboBox.SelectedItem = item;
                    break;
                }
            }

            // �⺻ ID ����
            BaseIdTextBox.Text = settings.DeviceBaseId.ToString("X");

            // ���� ǥ��
            StatusTextBlock.Text = settings.IsConnected ? "�����" : "���� �ȵ�";
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