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

        /// <summary>
        /// 선택된 이상 유형을 반환합니다.
        /// </summary>
        public string GetSelectedAbnormalType()
        {
            if (cmbAbnormalType.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content.ToString();
            }
            return "비정상 상태 (Abnormal)"; // 기본값
        }

        /// <summary>
        /// 이상 유형 식별자를 반환합니다. (PSTARDeviceService 메서드와 매핑)
        /// </summary>
        public AbnormalType GetSelectedAbnormalTypeEnum()
        {
            if (cmbAbnormalType.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedText = selectedItem.Content.ToString();

                if (selectedText.Contains("OVERLOAD"))
                    return AbnormalType.OVERLOAD;
                else if (selectedText.Contains("POWER FAIL"))
                    return AbnormalType.POWERFAIL;
                else if (selectedText.Contains("BLACKOUT"))
                    return AbnormalType.BLACKOUT;
                else if (selectedText.Contains("LOW PRESSURE"))
                    return AbnormalType.LowPressure;
                else if (selectedText.Contains("COMM FAILURE"))
                    return AbnormalType.CommFailure;
            }

            return AbnormalType.OVERLOAD; // 기본값
        }
    }

    /// <summary>
    /// 이상 유형 열거형 - PSTARDeviceService 메서드와 매핑
    /// </summary>
    public enum AbnormalType
    {
        OVERLOAD,      // 과부하
        POWERFAIL,     // 전원 이상
        BLACKOUT,     // 블랙 아웃
        LowPressure,    // 저압 상태
        CommFailure,   // 통신 오류
    }
}