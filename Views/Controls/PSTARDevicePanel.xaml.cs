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
                    case nameof(PSTARDeviceModel.SourceLamp):
                        UpdateLamp(SourceLamp, deviceModel.SourceLamp ? WhiteBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.ABN_LAMP):
                        UpdateLamp(AbnormalLamp, deviceModel.ABN_LAMP ? RedBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.RUN_LAMP):
                        UpdateLamp(RunLamp, deviceModel.RUN_LAMP ? GreenBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.STOP_LAMP):
                        UpdateLamp(StopLamp, deviceModel.STOP_LAMP ? RedBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.HEATING_LAMP):
                        UpdateLamp(HeatingLamp, deviceModel.HEATING_LAMP ? OrangeBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.COMM_FAULT_LAMP):
                        UpdateLamp(CommFailureLamp, deviceModel.COMM_FAULT_LAMP ? RedBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.LOW_PRESS_LAMP):
                        UpdateLamp(LowPressureLamp, deviceModel.LOW_PRESS_LAMP ? YellowBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.STAND_BY_LAMP):
                        UpdateLamp(StandbyLamp, deviceModel.STAND_BY_LAMP ? YellowBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.HEAT_ON_LAMP):
                        UpdateLamp(HeatOnLamp, deviceModel.HEAT_ON_LAMP ? WhiteBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.MODE_STBY_LAMP):
                        //UpdateLamp(ManualModeLamp, deviceModel.MODE_MANUAL_LAMP ? OffBrush : OffBrush);
                        UpdateLamp(StandbyModeLamp, deviceModel.MODE_STBY_LAMP ? WhiteBrush : OffBrush);
                        break;
                    case nameof(PSTARDeviceModel.MODE_MANUAL_LAMP):
                        UpdateLamp(ManualModeLamp, deviceModel.MODE_MANUAL_LAMP ? WhiteBrush : OffBrush);
                        //if (deviceModel.IsManualMode)
                        //{
                        //    UpdateLamp(StandbyModeLamp, OffBrush);
                        //    UpdateLamp(ManualModeLamp, WhiteBrush);
                        //}
                        //else
                        //{
                        //    UpdateLamp(ManualModeLamp, OffBrush);
                        //    UpdateLamp(StandbyModeLamp, WhiteBrush);
                        //}
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
            UpdateLamp(SourceLamp, deviceModel.SourceLamp ? WhiteBrush : OffBrush);
            UpdateLamp(AbnormalLamp, deviceModel.ABN_LAMP ? RedBrush : OffBrush);
            UpdateLamp(RunLamp, deviceModel.RUN_LAMP ? GreenBrush : OffBrush);
            UpdateLamp(StopLamp, deviceModel.STOP_LAMP ? RedBrush : OffBrush);
            UpdateLamp(HeatingLamp, deviceModel.HEATING_LAMP ? OrangeBrush : OffBrush);
            UpdateLamp(CommFailureLamp, deviceModel.COMM_FAULT_LAMP ? RedBrush : OffBrush);
            UpdateLamp(LowPressureLamp, deviceModel.LOW_PRESS_LAMP ? YellowBrush : OffBrush);
            UpdateLamp(StandbyLamp, deviceModel.STAND_BY_LAMP ? YellowBrush : OffBrush);
            UpdateLamp(HeatOnLamp, deviceModel.HEAT_ON_LAMP ? WhiteBrush : OffBrush);
            UpdateLamp(ManualModeLamp, deviceModel.MODE_MANUAL_LAMP ? WhiteBrush : OffBrush);
            UpdateLamp(StandbyModeLamp, deviceModel.MODE_STBY_LAMP ? WhiteBrush : OffBrush);
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