using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PSTARV2MonitoringApp.Views.Dialogs
{
    /// <summary>
    /// AbnormalSimulationDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AbnormalSimulationDialog : UserControl
    {
        private string _deviceId;
        
        public AbnormalSimulationDialog(string deviceId)
        {
            InitializeComponent();
            _deviceId = deviceId;
        }
        
        public string GetSelectedAbnormalType()
        {
            if (cmbAbnormalType.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content.ToString();
            }
            return "비정상 상태 (Abnormal)"; // 기본값
        }
    }
}
