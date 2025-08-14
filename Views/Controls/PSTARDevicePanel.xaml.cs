using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.ViewModels.Controls;
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

namespace PSTARV2MonitoringApp.Views.Controls
{
    /// <summary>
    /// PSTARDevicePanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PSTARDevicePanel : UserControl
    {
        // 이 패널에 표시할 장치 모델
        private PSTARDevicePanelModel _deviceModel;

        // 램프 색상 정의
        //private static readonly SolidColorBrush OrangeBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0x78, 0x3C));
        private static readonly SolidColorBrush GreenBrush = new SolidColorBrush(Colors.Green);
        private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Colors.Red);
        private static readonly SolidColorBrush YellowBrush = new SolidColorBrush(Colors.Yellow);
        private static readonly SolidColorBrush OrangeBrush = new SolidColorBrush(Colors.Orange);
        private static readonly SolidColorBrush OffBrush = new SolidColorBrush(Colors.Gray);
        private static readonly SolidColorBrush WhiteBrush = new SolidColorBrush(Colors.White);

        public PSTARDevicePanelViewModel ViewModel { get; private set; }

        public PSTARDevicePanel()
        {
            InitializeComponent();
        }

        // ViewModel을 받는 생성자
        public PSTARDevicePanel(PSTARDevicePanelViewModel viewModel)
        {
            ViewModel = viewModel;
            InitializeComponent();
            DataContext = ViewModel; // ViewModel을 DataContext로 설정
        }

        public void SetDeviceModel(PSTARDevicePanelModel deviceModel)
        {
            if (ViewModel != null)
            {
                // 현재 ViewModel이 있으면 해당 장치 ID를 현재 활성화 ID로 설정
                ViewModel.CurrentDeviceId = deviceModel.DeviceId;
            }

            _deviceModel = deviceModel;

            // 모델 데이터 변경 이벤트 구독
            _deviceModel.PropertyChanged += DeviceModel_PropertyChanged;

            // 램프 상태 초기 업데이트
            UpdateAllLamps();
        }

        private void DeviceModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 모델 속성이 변경되면 해당 램프만 업데이트
            switch (e.PropertyName)
            {
                case nameof(PSTARDevicePanelModel.IsSourceOn):
                    UpdateLamp(SourceLamp, _deviceModel.IsSourceOn ? WhiteBrush : OffBrush);
                    break;
                case nameof(PSTARDevicePanelModel.IsAbnormal):
                    UpdateLamp(AbnormalLamp, _deviceModel.IsAbnormal ? RedBrush : OffBrush);
                    break;
                case nameof(PSTARDevicePanelModel.IsRunning):
                    UpdateLamp(RunLamp, _deviceModel.IsRunning ? GreenBrush : OffBrush);
                    break;
                case nameof(PSTARDevicePanelModel.IsStopped):
                    UpdateLamp(StopLamp, _deviceModel.IsStopped ? RedBrush : OffBrush);
                    break;
                case nameof(PSTARDevicePanelModel.IsHeating):
                    UpdateLamp(HeatingLamp, _deviceModel.IsHeating ? OrangeBrush : OffBrush);
                    break;
                case nameof(PSTARDevicePanelModel.IsCommFailure):
                    UpdateLamp(CommFailureLamp, _deviceModel.IsCommFailure ? RedBrush : OffBrush);
                    break;
                case nameof(PSTARDevicePanelModel.IsLowPressure):
                    UpdateLamp(LowPressureLamp, _deviceModel.IsLowPressure ? YellowBrush : OffBrush);
                    break;
                case nameof(PSTARDevicePanelModel.IsStandby):
                    UpdateLamp(StandbyLamp, _deviceModel.IsStandby ? YellowBrush : OffBrush);
                    UpdateLamp(StbyLamp, _deviceModel.IsStandby ? WhiteBrush : OffBrush);
                    break;
                case nameof(PSTARDevicePanelModel.IsOn):
                    UpdateLamp(OnLamp, _deviceModel.IsOn ? WhiteBrush : OffBrush);
                    break;
                case nameof(PSTARDevicePanelModel.IsManualMode):
                    UpdateLamp(ManualLamp, _deviceModel.IsManualMode ? WhiteBrush : OffBrush);
                    break;
            }
        }

        private void UpdateLamp(Ellipse lamp, SolidColorBrush brush)
        {
            if (lamp != null)
            {
                lamp.Fill = brush;
            }
        }

        private void UpdateAllLamps()
        {
            if (_deviceModel == null) return;

            // 모든 램프 상태 업데이트
            UpdateLamp(SourceLamp, _deviceModel.IsSourceOn ? WhiteBrush : OffBrush);
            UpdateLamp(AbnormalLamp, _deviceModel.IsAbnormal ? RedBrush : OffBrush);
            UpdateLamp(RunLamp, _deviceModel.IsRunning ? GreenBrush : OffBrush);
            UpdateLamp(StopLamp, _deviceModel.IsStopped ? RedBrush : OffBrush);
            UpdateLamp(HeatingLamp, _deviceModel.IsHeating ? OrangeBrush : OffBrush);
            UpdateLamp(CommFailureLamp, _deviceModel.IsCommFailure ? RedBrush : OffBrush);
            UpdateLamp(LowPressureLamp, _deviceModel.IsLowPressure ? YellowBrush : OffBrush);
            UpdateLamp(StandbyLamp, _deviceModel.IsStandby ? YellowBrush : OffBrush);
            UpdateLamp(OnLamp, _deviceModel.IsOn ? WhiteBrush : OffBrush);
            UpdateLamp(ManualLamp, _deviceModel.IsManualMode ? WhiteBrush : OffBrush);
            UpdateLamp(StbyLamp, _deviceModel.IsStandby ? WhiteBrush : OffBrush);
        }
    }
}