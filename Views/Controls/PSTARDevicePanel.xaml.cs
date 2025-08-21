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
        // 램프 색상 정의
        private static readonly SolidColorBrush GreenBrush = new SolidColorBrush(Colors.Green);
        private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Colors.Red);
        private static readonly SolidColorBrush YellowBrush = new SolidColorBrush(Colors.Yellow);
        private static readonly SolidColorBrush OrangeBrush = new SolidColorBrush(Colors.Orange);
        private static readonly SolidColorBrush OffBrush = new SolidColorBrush(Colors.Gray);
        private static readonly SolidColorBrush WhiteBrush = new SolidColorBrush(Colors.White);

        // 각 패널마다 독립적인 ViewModel 인스턴스
        public PSTARDevicePanelViewModel ViewModel { get; private set; }

        // TestViewModel의 ViewModel을 사용하는 생성자 추가
        public PSTARDevicePanel(string deviceId, PSTARDevicePanelViewModel viewModel)
        {
            ViewModel = viewModel;  // 기존 ViewModel 사용
            DataContext = this;
            InitializeComponent();
        }

        // 특정 장치 ID로 초기화하는 생성자 
        public PSTARDevicePanel(string deviceId)
        {
            ViewModel = new PSTARDevicePanelViewModel(deviceId);
            DataContext = this;
            InitializeComponent();
        }

        // 기본 생성자
        public PSTARDevicePanel()
        {
            ViewModel = new PSTARDevicePanelViewModel();
            DataContext = this;
            InitializeComponent();
        }

        public void SetDeviceModel(PSTARDeviceModel deviceModel)
        {
            if (deviceModel == null) return;

            // ViewModel에 장치 모델 설정
            ViewModel.SetDeviceModel(deviceModel);

            // 모델 데이터 변경 이벤트 구독
            deviceModel.PropertyChanged += DeviceModel_PropertyChanged;

            // 램프 상태 초기 업데이트
            UpdateAllLamps();
        }

        private void DeviceModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // UI 스레드에서 실행되도록 보장
            Dispatcher.Invoke(() =>
            {
                var deviceModel = sender as PSTARDeviceModel;
                if (deviceModel == null) return;

                // 모델 속성이 변경되면 해당 램프만 업데이트
                switch (e.PropertyName)
                {
                    case nameof(PSTARDeviceModel.IsSourceOn):
                        UpdateLamp(SourceLamp, deviceModel.IsSourceOn ? WhiteBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.IsAbnormal):
                        UpdateLamp(AbnormalLamp, deviceModel.IsAbnormal ? RedBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.IsRunning):
                        UpdateLamp(RunLamp, deviceModel.IsRunning ? GreenBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.IsStopped):
                        UpdateLamp(StopLamp, deviceModel.IsStopped ? RedBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.IsHeating):
                        UpdateLamp(HeatingLamp, deviceModel.IsHeating ? OrangeBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.IsCommFailure):
                        UpdateLamp(CommFailureLamp, deviceModel.IsCommFailure ? RedBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.IsLowPressure):
                        UpdateLamp(LowPressureLamp, deviceModel.IsLowPressure ? YellowBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.IsStandby):
                        UpdateLamp(StandbyLamp, deviceModel.IsStandby ? YellowBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.IsHeatOn):
                        UpdateLamp(HeatOnLamp, deviceModel.IsHeatOn ? WhiteBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.IsStandbyMode):
                        UpdateLamp(ManualModeLamp, deviceModel.IsManualMode ? OffBrush : OffBrush);
                        UpdateLamp(StandbyModeLamp, deviceModel.IsStandbyMode ? WhiteBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.IsManualMode):
                        if (deviceModel.IsManualMode)
                        {
                            UpdateLamp(StandbyModeLamp, OffBrush);
                            UpdateLamp(ManualModeLamp, WhiteBrush);
                        }
                        else
                        {
                            UpdateLamp(ManualModeLamp, OffBrush);
                            UpdateLamp(StandbyModeLamp, WhiteBrush);
                        }
                        break;
                }
            });
        }

        private void UpdateLamp(Ellipse lamp, SolidColorBrush brush)
        {
            if (lamp != null)
            {
                lamp.Fill = brush;
            }
        }

        // 이 패널의 장치 ID를 반환하는 메서드
        public string GetDeviceId()
        {
            return ViewModel?.DeviceId;
        }

        private void UpdateAllLamps()
        {
            var deviceModel = ViewModel?.DeviceModel;
            if (deviceModel == null) return;

            // 모든 램프 상태 업데이트
            UpdateLamp(SourceLamp, deviceModel.IsSourceOn ? WhiteBrush : OffBrush);
            UpdateLamp(AbnormalLamp, deviceModel.IsAbnormal ? RedBrush : OffBrush);
            UpdateLamp(RunLamp, deviceModel.IsRunning ? GreenBrush : OffBrush);
            UpdateLamp(StopLamp, deviceModel.IsStopped ? RedBrush : OffBrush);
            UpdateLamp(HeatingLamp, deviceModel.IsHeating ? OrangeBrush : OffBrush);
            UpdateLamp(CommFailureLamp, deviceModel.IsCommFailure ? RedBrush : OffBrush);
            UpdateLamp(LowPressureLamp, deviceModel.IsLowPressure ? YellowBrush : OffBrush);
            UpdateLamp(StandbyLamp, deviceModel.IsStandby ? YellowBrush : OffBrush);
            UpdateLamp(HeatOnLamp, deviceModel.IsHeatOn ? WhiteBrush : OffBrush);
            UpdateLamp(ManualModeLamp, deviceModel.IsManualMode ? WhiteBrush : OffBrush);
            UpdateLamp(StandbyModeLamp, deviceModel.IsStandby ? WhiteBrush : OffBrush);
        }

        // LP Test 관련 간단한 이벤트 핸들러들 (button에 Command 에 바인딩이 안돼서 이렇게 구현)
        private void LPTestButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ViewModel.EndLPTestCommand.Execute(null);
        }

        private void LPTestButton_MouseLeave(object sender, MouseEventArgs e)
        {
            ViewModel.EndLPTestCommand.Execute(null);
        }
    }
}