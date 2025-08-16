using PSTARV2MonitoringApp.Models;
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
            LoadDeviceIds();
            LoadDeviceModels();
        }

        private void LoadDeviceIds()
        {
            DeviceIdComboBox.Items.Clear();
            
            foreach (var deviceId in DeviceInfo.SupportedDeviceIds)
            {
                var item = new ComboBoxItem
                {
                    Content = deviceId.DisplayText,
                    Tag = deviceId.Id
                };
                DeviceIdComboBox.Items.Add(item);
            }
            
            if (DeviceIdComboBox.Items.Count > 0)
            {
                DeviceIdComboBox.SelectedIndex = 0;
            }
        }

        private void LoadDeviceModels()
        {
            DeviceTypeComboBox.Items.Clear();
            
            foreach (var model in DeviceInfo.SupportedDeviceModels)
            {
                var item = new ComboBoxItem
                {
                    Content = model.ModelName,
                    Tag = model
                };
                DeviceTypeComboBox.Items.Add(item);
            }
            
            if (DeviceTypeComboBox.Items.Count > 1)
            {
                DeviceTypeComboBox.SelectedIndex = 1; // 기본값을 두 번째 항목으로
            }
        }

        public string GetSelectedDeviceId()
        {
            if (DeviceIdComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Tag?.ToString();
            }
            return null;
        }

        public string GetSelectedDeviceModel()
        {
            if (DeviceTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content?.ToString();
            }
            return null;
        }

        /// <summary>
        /// 선택된 DeviceId 객체 반환
        /// </summary>
        public DeviceId GetSelectedDeviceIdObject()
        {
            var selectedId = GetSelectedDeviceId();
            return selectedId != null ? DeviceInfo.GetDeviceIdById(selectedId) : null;
        }

        /// <summary>
        /// 선택된 DeviceModel 객체 반환
        /// </summary>
        public DeviceModel GetSelectedDeviceModelObject()
        {
            var selectedModelName = GetSelectedDeviceModel();
            return selectedModelName != null ? DeviceInfo.GetDeviceModelByName(selectedModelName) : null;
        }
    }
}
