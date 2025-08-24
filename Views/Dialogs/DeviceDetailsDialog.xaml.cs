using PSTARV2MonitoringApp.Models;
using System.Windows.Controls;

namespace PSTARV2MonitoringApp.Views.Dialogs
{
    public partial class DeviceDetailsDialog : UserControl
    {
        public DeviceDetailsDialog(PSTARDeviceModel deviceModel)
        {
            InitializeComponent();
            DataContext = deviceModel;

            // COM Status 상태 설정
            txtComStatus.Text = deviceModel.COMM_FAULT_LAMP ? "Disconnected" : "Connected";

            // 현재 시퀀스 타임 설정 (실제 값을 추가적으로 계산하거나 가져올 수 있습니다)
            txtCurrentSequenceTime.Text = deviceModel.CountSeqTime_S.ToString();
        }
    }
}