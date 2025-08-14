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
    /// AddDeviceDialog.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AddDeviceDialog : UserControl
    {
        public AddDeviceDialog()
        {
            InitializeComponent();
        }

        public string GetSelectedDeviceId()
        {
            if(DeviceIdComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                //"ID" 접두사 제거하고 숫자만 반환
                string content = selectedItem.Content.ToString();
                return content;
            }
            return null;
        }

        public string GetSelectedDeviceModel()
        {
            if(DeviceTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content.ToString();
            }
            return null;
        }
    }
}
