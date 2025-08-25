using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.Services;
using PSTARV2MonitoringApp.ViewModels.Controls;
using PSTARV2MonitoringApp.Views.Controls;
using PSTARV2MonitoringApp.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace PSTARV2MonitoringApp.ViewModels.Pages
{
    public partial class TestViewModel : ObservableObject
    {
        // 각 카드별로 독립적인 ViewModel과 Panel을 관리하는 딕셔너리
        private readonly Dictionary<string, PSTARDevicePanelViewModel> _deviceViewModels;
        private readonly Dictionary<string, PSTARDevicePanel> _devicePanels;

        // TestPage 참조를 저장하기 위한 속성
        private Views.Pages.TestPage _testPage;

        // 로그 서비스 참조
        private readonly DeviceLogService _logService;

        // DataGrid에 바인딩할 로그 컬렉션
        public ObservableCollection<DeviceLogEntry> DeviceLogs => _logService.LogEntries;

        public TestViewModel()
        {
            _deviceViewModels = new Dictionary<string, PSTARDevicePanelViewModel>();
            _devicePanels = new Dictionary<string, PSTARDevicePanel>();
            _logService = DeviceLogService.Instance;
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
                    // 선택된 디바이스 ID와 모델 객체 가져오기
                    var selectedDeviceId = addDeviceDialog.GetSelectedDeviceIdObject();
                    var selectedDeviceModel = addDeviceDialog.GetSelectedDeviceModelObject();

                    // 유효한 선택인지 확인
                    if (selectedDeviceId != null && selectedDeviceModel != null)
                    {
                        // TestPage가 로드되어 있을 경우 UI 업데이트
                        if (_testPage != null)
                        {
                            // ID에 따라 해당 Card 위치에 PSTARDevicePanel 추가
                            AddDevicePanelToCard(selectedDeviceId, selectedDeviceModel);
                        }
                    }
                    else
                    {
                        MessageBox.Show("올바른 장치 ID와 모델을 선택해주세요.", "선택 오류", 
                            System.Windows.MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"장치 추가 오류: {ex.Message}", "오류", 
                    System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                
                // 오류 로그 추가
                _logService.AddLog("시스템", $"오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 모든 장치를 제거합니다.
        /// </summary>
        [RelayCommand]
        public void ClearAllDevices()
        {
            var deviceIds = new List<string>(_deviceViewModels.Keys);
            foreach (var deviceId in deviceIds)
            {
                RemoveDevice(deviceId);
            }

            MessageBox.Show("모든 장치가 제거되었습니다.", "완료",
                System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);

            _logService.LogAllDevicesRemoved();
        }

        /// <summary>
        /// 로그 초기화
        /// </summary>
        [RelayCommand]
        public void ClearLogs()
        {
            _logService.ClearLogs();
            _logService.LogCleared();
            MessageBox.Show("로그가 초기화되었습니다.", "완료",
                System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddDevicePanelToCard(DeviceId deviceId, DeviceModel deviceModel)
        {
            // 이미 등록된 장치인지 확인
            if (_deviceViewModels.ContainsKey(deviceId.Id))
            {
                // 이미 등록된 장치라면 사용자에게 확인
                System.Windows.MessageBoxResult result = MessageBox.Show(
                    $"이미 등록된 장치 ID입니다. 새로 등록하시겠습니까?\n\n장치 ID: {deviceId.DisplayText}",
                    "장치 등록 확인",
                    System.Windows.MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    // 기존 장치 삭제
                    RemoveDevice(deviceId.Id);
                    _logService.AddLog(deviceId.DisplayText, DeviceLogMessages.Device.ReregisteredAfterRemoval);
                }
                else
                {
                    // 취소 선택 시 함수 종료
                    return;
                }
            }

            // deviceId에 따라 적절한 카드 위치 선택
            Grid targetGrid = GetTargetGridByDeviceId(deviceId);

            if (targetGrid != null)
            {
                // 기존 내용 제거
                targetGrid.Children.Clear();

                // 새로운 독립적인 ViewModel 생성
                var deviceViewModel = new PSTARDevicePanelViewModel(deviceId.Id);

                // 장치 모델 생성 및 설정
                var devicePanelModel = new PSTARDeviceModel(deviceId.Id, deviceModel.ModelName);
                deviceViewModel.SetDeviceModel(devicePanelModel);

                // 새 PSTARDevicePanel 생성 (독립적인 ViewModel 사용)
                //var devicePanel = new PSTARDevicePanel(deviceId.Id);
                // 새 PSTARDevicePanel 생성 (ViewModel 주입)
                var devicePanel = new PSTARDevicePanel(deviceId.Id, deviceViewModel);
                devicePanel.SetDeviceModel(devicePanelModel);
                RegisterDevicePanelEvents(devicePanel);

                // 딕셔너리에 저장
                _deviceViewModels[deviceId.Id] = deviceViewModel;
                _devicePanels[deviceId.Id] = devicePanel;

                // 패널을 그리드에 추가
                targetGrid.Children.Add(devicePanel);

                // 성공 메시지
                MessageBox.Show($"장치가 성공적으로 추가되었습니다.\n\n장치 ID: {deviceId.DisplayText}\n모델: {deviceModel.ModelName}",
                    "장치 추가 완료", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
                
                // 로그 추가
                _logService.LogDeviceAdded(deviceId.Id, deviceModel.ModelName);
            }
            else
            {
                MessageBox.Show($"장치 ID '{deviceId.DisplayText}'에 해당하는 카드를 찾을 수 없습니다.",
                    "오류", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
                
                // 오류 로그 추가
                _logService.AddLog(deviceId.DisplayText, DeviceLogMessages.Device.CardNotFound);
            }
        }

        private Grid GetTargetGridByDeviceId(DeviceId deviceId)
        {
            return _testPage.FindName(deviceId.UICardElementName) as Grid;
        }

        private void RemoveDevice(string deviceId)
        {
            // 장치 ViewModel과 관련 리소스 정리
            if (_deviceViewModels.TryGetValue(deviceId, out var viewModel))
            {
                // 중요: ViewModel의 Dispose 메서드 호출하여 리소스 정리
                viewModel.Dispose();

                // DeviceDeleted 이벤트 핸들러 해제
                viewModel.DeviceDeleted -= OnDeviceDeleted;

                // 딕셔너리에서 제거
                _deviceViewModels.Remove(deviceId);
            }

            // 패널 제거
            if (_devicePanels.ContainsKey(deviceId))
            {
                // 패널 참조 제거
                _devicePanels.Remove(deviceId);
            }

            // UI에서 제거
            var deviceInfo = DeviceInfo.GetDeviceIdById(deviceId);
            if (deviceInfo != null)
            {
                var targetGrid = GetTargetGridByDeviceId(deviceInfo);
                if (targetGrid != null)
                {
                    targetGrid.Children.Clear();
                }

                // 로그에 장치 제거 기록
                _logService.LogDeviceRemoved(deviceInfo.DisplayText);
            }

            //// ViewModel 제거
            //if (_deviceViewModels.ContainsKey(deviceId))
            //{
            //    _deviceViewModels.Remove(deviceId);
            //}

            //// Panel 제거
            //if (_devicePanels.ContainsKey(deviceId))
            //{
            //    _devicePanels.Remove(deviceId);
            //}

            //// UI에서 제거
            //var deviceInfo = DeviceInfo.GetDeviceIdById(deviceId);
            //if (deviceInfo != null)
            //{
            //    var targetGrid = GetTargetGridByDeviceId(deviceInfo);
            //    targetGrid?.Children.Clear();

            //    // 제거 로그 추가
            //    _logService.AddLog(deviceInfo.DisplayText, "장치 제거됨");
            //}
        }

        /// <summary>
        /// 특정 장치의 ViewModel을 가져옵니다.
        /// </summary>
        /// <param name="deviceId">장치 ID</param>
        /// <returns>해당 장치의 ViewModel 또는 null</returns>
        public PSTARDevicePanelViewModel GetDeviceViewModel(string deviceId)
        {
            return _deviceViewModels.TryGetValue(deviceId, out var viewModel) ? viewModel : null;
        }

        /// <summary>
        /// 특정 장치의 Panel을 가져옵니다.
        /// </summary>
        /// <param name="deviceId">장치 ID</param>
        /// <returns>해당 장치의 Panel 또는 null</returns>
        public PSTARDevicePanel GetDevicePanel(string deviceId)
        {
            return _devicePanels.TryGetValue(deviceId, out var panel) ? panel : null;
        }

        /// <summary>
        /// 모든 등록된 장치 ID 목록을 가져옵니다.
        /// </summary>
        /// <returns>장치 ID 목록</returns>
        public IEnumerable<string> GetRegisteredDeviceIds()
        {
            return _deviceViewModels.Keys;
        }

        /// <summary>
        /// 특정 장치의 상태를 업데이트합니다.
        /// </summary>
        /// <param name="deviceId">장치 ID</param>
        /// <param name="cardModel">장치 상태 모델</param>
        public void UpdateDeviceStatus(string deviceId, DeviceStatusCardModel cardModel)
        {
            var viewModel = GetDeviceViewModel(deviceId);
            viewModel?.UpdateDeviceFromCardModel(cardModel);
        }

        // PSTARDevicePanel이 생성될 때 삭제 이벤트를 연결합니다
        private void RegisterDevicePanelEvents(PSTARDevicePanel panel)
        {
            if (panel != null && panel.ViewModel != null)
            {
                panel.ViewModel.DeviceDeleted += OnDeviceDeleted;
            }
        }

        // 장치 삭제 이벤트 처리 메서드
        private void OnDeviceDeleted(object sender, DeviceDeletedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.DeviceId))
            {
                RemoveDevice(e.DeviceId);
            }
        }

    }
}