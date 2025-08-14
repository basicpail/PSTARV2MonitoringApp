using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.ViewModels.Controls;
using PSTARV2MonitoringApp.Views.Controls;
using PSTARV2MonitoringApp.Views.Dialogs;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace PSTARV2MonitoringApp.ViewModels.Pages
{
    public partial class TestViewModel : ObservableObject
    {
        // PSTARDevicePanelViewModel 인스턴스
        private readonly PSTARDevicePanelViewModel _devicePanelViewModel;

        // TestPage 참조를 저장하기 위한 속성
        private Views.Pages.TestPage _testPage;

        public TestViewModel(PSTARDevicePanelViewModel devicePanelViewModel)
        {
            _devicePanelViewModel = devicePanelViewModel;
        }

        // TestPage 참조를 설정하는 메소드
        public void SetTestPage(Views.Pages.TestPage testPage)
        {
            _testPage = testPage;
        }

        [RelayCommand]
        public async Task ShowAddDeviceDialog()
        {
            try
            {
                var addDeviceDialog = new AddDeviceDialog();

                addDeviceDialog.HorizontalAlignment = HorizontalAlignment.Stretch;
                addDeviceDialog.VerticalAlignment = VerticalAlignment.Stretch;
                addDeviceDialog.Width = double.NaN; // Auto
                addDeviceDialog.Height = double.NaN; // Auto

                var dialog = new ContentDialog
                {
                    Content = addDeviceDialog,
                    PrimaryButtonText = "추가",
                    CloseButtonText = "닫기",
                    DefaultButton = ContentDialogButton.Primary,
                    IsPrimaryButtonEnabled = true,
                    Effect = null,

                    //MinWidth = 380,
                    //MinHeight = 300,
                    //MaxWidth = 500,
                    //MaxHeight = 600,

                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch
                };

                if (Application.Current.MainWindow?.FindName("RootContentDialog") is ContentPresenter contentPresenter)
                {
                    dialog.DialogHost = contentPresenter;
                }

                // 다이얼로그 표시
                var result = await dialog.ShowAsync();

                // 다이얼로그에서 "추가" 버튼을 클릭했는지 확인
                if (result == ContentDialogResult.Primary)
                {
                    // 선택된 디바이스 ID와 모델 가져오기
                    string deviceId = addDeviceDialog.GetSelectedDeviceId();
                    string deviceModel = addDeviceDialog.GetSelectedDeviceModel();

                    // 유효한 디바이스 ID와 모델인지 확인
                    if (!string.IsNullOrEmpty(deviceId) && !string.IsNullOrEmpty(deviceModel))
                    {
                        // TestPage가 로드되어 있을 경우 UI 업데이트
                        if (_testPage != null)
                        {
                            // ID에 따라 해당 Card 위치에 PSTARDevicePanel 추가
                            AddDevicePanelToCard(deviceId, deviceModel);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"장치 추가 오류: {ex.Message}", "오류", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddDevicePanelToCard(string deviceId, string deviceModel)
        {
            // 이미 등록된 장치인지 확인
            var existingDevice = _devicePanelViewModel.GetDevice(deviceId);
            if (existingDevice != null)
            {
                // 이미 등록된 장치라면 사용자에게 확인
                System.Windows.MessageBoxResult result = MessageBox.Show(
                    $"이미 등록된 장치 ID입니다. 새로 등록하시겠습니까?\n\n장치 ID: {deviceId}",
                    "장치 등록 확인",
                    System.Windows.MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // 기존 장치 삭제 후 다시 등록
                    _devicePanelViewModel.RemoveDevice(deviceId);
                }
                else
                {
                    // 취소 선택 시 함수 종료
                    return;
                }
            }

            // 장치 모델 생성 및 설정
            var devicePanelModel = _devicePanelViewModel.AddDevice(deviceId, deviceModel);

            // deviceId에 따라 적절한 카드 위치 선택
            Grid targetGrid = null;

            switch (deviceId)
            {
                case "ID 1":
                    targetGrid = _testPage.FindName("DevicePanelCard1") as Grid;
                    break;
                case "ID 2":
                    targetGrid = _testPage.FindName("DevicePanelCard2") as Grid;
                    break;
                case "ID 3":
                    targetGrid = _testPage.FindName("DevicePanelCard3") as Grid;
                    break;
            }

            if (targetGrid != null)
            {
                // 기존 내용 제거
                targetGrid.Children.Clear();

                // 새 PSTARDevicePanel 생성
                PSTARDevicePanel devicePanel = new PSTARDevicePanel(_devicePanelViewModel);

                // 장치 모델 설정
                devicePanel.SetDeviceModel(devicePanelModel);

                // 패널을 그리드에 추가
                targetGrid.Children.Add(devicePanel);
            }
        }
    }
}