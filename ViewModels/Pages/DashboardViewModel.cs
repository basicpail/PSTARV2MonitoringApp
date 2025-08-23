using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.Services;
using PSTARV2MonitoringApp.ViewModels.Controls;
using PSTARV2MonitoringApp.Views.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace PSTARV2MonitoringApp.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _testValue = 0;

        [ObservableProperty]
        private string? _selectedIdFilter = null;

        // Raw Data 최대 아이템 수
        private const int MaxRawDataItems = 100;

        // CAN ID 필터 설정
        [ObservableProperty]
        private bool _showAllCanMessages = true;

        [ObservableProperty]
        private string _selectedCanIdFilter = "모든 ID";

        private const int MaxItems = 20;
        private readonly Random _rnd = new Random();
        private readonly DispatcherTimer _timer;
        private readonly DeviceLogService _logService;
        private readonly CANCommunicationService _canCommService;
        private readonly CANDataProcessingService _canDataService;
        private readonly ObservableCollection<DeviceLogEntry> _filteredDeviceLogs;

        // 사용 가능한 CAN ID 목록
        public ObservableCollection<string> AvailableCanIds { get; } = new ObservableCollection<string>() {
            "모든 ID", "0x100", "0x200", "0x300"
        };

        public DeviceStatusCardViewModel DeviceStatusCardViewModel { get; }

        public ObservableCollection<DeviceLogEntry> FilteredDeviceLogs => _filteredDeviceLogs;

        public ObservableCollection<DeviceLogEntry> DeviceLogs => _logService.LogEntries;

        public ObservableCollection<string> AvailableIds { get; } = new ObservableCollection<string>();

        public ObservableCollection<RawDataTestModel> TestRawDataTimelineItems { get; } = new ObservableCollection<RawDataTestModel>();

        public DashboardViewModel(DeviceStatusCardViewModel deviceStatusCardViewModel)
        {
            DeviceStatusCardViewModel = deviceStatusCardViewModel;
            _logService = DeviceLogService.Instance;
            _filteredDeviceLogs = new ObservableCollection<DeviceLogEntry>();
            _canCommService = CANCommunicationService.Instance;
            _canDataService = CANDataProcessingService.Instance;

            // DeviceLogService의 로그 변경 이벤트 구독
            _logService.LogEntries.CollectionChanged += LogEntries_CollectionChanged;

            // CAN 통신 이벤트 구독
            _canCommService.ConnectionStatusChanged += OnCANConnectionStatusChanged;
            _canCommService.ErrorOccurred += OnCANErrorOccurred;

            // CAN 데이터 서비스 등록
            _canDataService.RegisterServices(deviceStatusCardViewModel);

            // CAN 전송 데이터 구독 (실제 장치가 송신한 데이터)
            _canCommService.DataTransmitted += OnCANDataTransmitted;

            // CAN 설정을 테스트 모드로 초기화
            _canCommService.Settings.InterfaceType = "TEST";
            _canCommService.Settings.Channel = "Virtual";
            _canCommService.Settings.BaudRate = 500000;

            // 초기 필터 적용
            ApplyLogFilter();
            UpdateAvailableLogIds();

            // 초기 샘플 로그 추가
            InitializeSampleLogs();
        }

        private void LogEntries_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // DeviceLogService의 로그가 변경될 때마다 필터 적용 및 ID 목록 업데이트
            Application.Current.Dispatcher.Invoke(() =>
            {
                ApplyLogFilter();
                UpdateAvailableLogIds();
            });
        }

        /// <summary>
        /// 장치가 송신한 CAN 데이터 처리
        /// </summary>
        private void OnCANDataTransmitted(object sender, CANTransmitEventArgs e)
        {
            var frame = e.Frame;

            // CAN ID 필터 적용
            if (!ShowAllCanMessages && SelectedCanIdFilter != "모든 ID")
            {
                string frameHexId = $"0x{frame.Id:X3}";
                if (frameHexId != SelectedCanIdFilter)
                    return;
            }

            // Raw Data 그리드에 데이터 추가
            Application.Current.Dispatcher.Invoke(() =>
            {
                // CAN 데이터를 RawDataTestModel로 변환
                var rawData = new RawDataTestModel
                {
                    STBY_Start = frame.Data[0] == 1 ? "ON" : "OFF",
                    RunLamp = frame.Data[1] == 1 ? "ON" : "OFF",
                    Overload = frame.Data[2] == 1 ? "ON" : "OFF",
                    ModeStatus = frame.Data[3] == 1 ? "ON" : "OFF",
                    RUN_req = frame.Data[4] == 1 ? "ON" : "OFF",
                    ResetButton = frame.Data[5] == 1 ? "ON" : "OFF",
                    StandByLamp = frame.Data[6] == 1 ? "ON" : "OFF",
                    TXLowpress = frame.Data[7] == 1 ? "ON" : "OFF",
                    // 추가 필드: CAN ID와 타임스탬프
                    CanId = $"0x{frame.Id:X3}",
                    Timestamp = frame.Timestamp.ToString("HH:mm:ss.fff")
                };

                //맨 앞에 추가
                TestRawDataTimelineItems.Insert(0, rawData);

                //최대 아이템 수 제한
                while (TestRawDataTimelineItems.Count > MaxRawDataItems)
                {
                    TestRawDataTimelineItems.RemoveAt(TestRawDataTimelineItems.Count - 1);
                }
            });
        }

        private void OnRawDataUpdated(object sender, RawDataUpdatedEventArgs e)
        {
            // 이 메서드는 더 이상 Raw Data 업데이트에 사용되지 않음
            // OnCANDataTransmitted에서 실제 CAN 송신 데이터로 대체
        }

        private void UpdateAvailableLogIds()
        {
            var currentIds = _logService.LogEntries.Select(x => x.Id).Distinct().OrderBy(x => x).ToList();

            AvailableIds.Clear();
            foreach (var id in currentIds)
            {
                AvailableIds.Add(id);
            }
        }

        private void ApplyLogFilter()
        {
            _filteredDeviceLogs.Clear();

            var filteredItems = string.IsNullOrEmpty(SelectedIdFilter)
                ? _logService.LogEntries.OrderByDescending(x => x.Date).ToList()
                : _logService.LogEntries.Where(x => x.Id == SelectedIdFilter).OrderByDescending(x => x.Date).ToList();

            foreach (var item in filteredItems)
            {
                _filteredDeviceLogs.Add(item);
            }
        }

        /// <summary>
        /// CAN ID 필터 전환
        /// </summary>
        [RelayCommand]
        public void ToggleCanIdFilter(string canId)
        {
            SelectedCanIdFilter = canId;
        }

        /// <summary>
        /// 모든 CAN 메시지 표시 전환
        /// </summary>
        [RelayCommand]
        public void ToggleShowAllCanMessages()
        {
            ShowAllCanMessages = !ShowAllCanMessages;
        }

        [RelayCommand]
        public async Task ShowLogFilterDialog()
        {
            try
            {
                // IdFilterDialog 인스턴스 생성
                var idFilterDialog = new IdFilterDialog();

                // 사용 가능한 ID 목록 전달
                idFilterDialog.LoadAvailableIds(AvailableIds);

                // 현재 선택된 필터 설정
                idFilterDialog.SetSelectedId(SelectedIdFilter);

                // IdFilterDialog의 크기 속성 설정
                idFilterDialog.HorizontalAlignment = HorizontalAlignment.Stretch;
                idFilterDialog.VerticalAlignment = VerticalAlignment.Stretch;
                idFilterDialog.Width = double.NaN; // Auto
                idFilterDialog.Height = double.NaN; // Auto

                // ContentDialog 생성
                var dialog = new ContentDialog
                {
                    Content = idFilterDialog,
                    PrimaryButtonText = "적용",
                    SecondaryButtonText = "취소",
                    CloseButtonText = "닫기",
                    DefaultButton = ContentDialogButton.Primary,
                    IsPrimaryButtonEnabled = true,
                    Effect = null,

                    // 컨텐츠 스타일 설정
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch
                };

                // ContentPresenter를 DialogHost로 설정
                if (Application.Current.MainWindow?.FindName("RootContentDialog") is ContentPresenter contentPresenter)
                {
                    dialog.DialogHost = contentPresenter;
                }

                // 다이얼로그 표시
                var result = await dialog.ShowAsync();

                // 결과 처리
                if (result == ContentDialogResult.Primary || idFilterDialog.FilterCleared)
                {
                    // 다이얼로그에서 선택된 ID 또는 필터가 해제된 경우
                    var selectedRadioButton = idFilterDialog.FindName("IdSelectionPanel") is StackPanel panel
                        ? panel.Children.OfType<RadioButton>().FirstOrDefault(rb => rb.IsChecked == true)
                        : null;

                    SelectedIdFilter = selectedRadioButton?.Tag?.ToString() == "ALL" || selectedRadioButton == null
                        ? null
                        : selectedRadioButton.Tag?.ToString();

                    ApplyLogFilter();
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 일반 MessageBox 사용
                MessageBox.Show($"필터 다이얼로그 오류: {ex.Message}", "오류", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);

                // 오류 로그 추가
                _logService.AddSystemErrorLog(ex.Message);
            }
        }

        private void InitializeSampleLogs()
        {
            _logService.LogMonitoringStart();

            // DeviceInfo를 사용하여 각 장치별 초기 상태 로그
            foreach (var deviceInfo in DeviceInfo.SupportedDeviceIds)
            {
                _logService.LogDeviceConnectionWaiting(deviceInfo.Id);
            }
        }

        [RelayCommand]
        public void AddRandomLog()
        {
            var random = new System.Random();
            var deviceInfos = DeviceInfo.SupportedDeviceIds;
            var randomDevice = deviceInfos[random.Next(deviceInfos.Count)];

            _logService.LogRandomInspection(randomDevice.Id);
        }

        public void AddSystemLog(string message)
        {
            _logService.AddSystemLog(message);
        }

        public void AddDeviceLog(string deviceId, string action)
        {
            var deviceInfo = DeviceInfo.GetDeviceIdById(deviceId);
            var displayId = deviceInfo?.DisplayText ?? deviceId;
            _logService.AddLog(displayId, action);
        }

        public void LogStatusCardUpdate(string statusCardId, string property, string value)
        {
            _logService.LogStatusChange(statusCardId, property, value);
        }

        [RelayCommand]
        public void ClearLogs()
        {
            _logService.ClearLogs();
            _logService.LogCleared();
        }

        [RelayCommand]
        public async Task StartCANSimulation()
        {
            try
            {
                _logService.AddSystemLog("CAN 시뮬레이션 시작");

                // 테스트 모드로 설정
                _canCommService.Settings.InterfaceType = "TEST";

                var success = await _canCommService.StartAsync();

                if (success)
                {
                    // CAN 데이터 처리 서비스 시작
                    await _canDataService.StartCommunicationAsync();
                    _logService.AddSystemLog("CAN 시뮬레이션 연결 성공 - 가상 데이터 수신 시작");
                }
                else
                {
                    _logService.AddSystemLog("CAN 시뮬레이션 연결 실패");
                }
            }
            catch (Exception ex)
            {
                _logService.AddSystemErrorLog($"CAN 시뮬레이션 시작 오류: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task StopCANSimulation()
        {
            try
            {
                _logService.AddSystemLog("CAN 시뮬레이션 중지");
                await _canCommService.StopAsync();
                await _canDataService.StopCommunicationAsync();
                _logService.AddSystemLog("CAN 시뮬레이션 중지 완료");
            }
            catch (Exception ex)
            {
                _logService.AddSystemErrorLog($"CAN 시뮬레이션 중지 오류: {ex.Message}");
            }
        }

        [RelayCommand]
        public async Task ShowCANSettings()
        {
            try
            {
                // CAN 설정 다이얼로그 표시
                var settingsDialog = new CANSettingsDialog();
                settingsDialog.LoadSettings(_canCommService.Settings);

                var dialog = new ContentDialog
                {
                    Content = settingsDialog,
                    Title = "CAN 통신 설정",
                    PrimaryButtonText = "적용",
                    CloseButtonText = "취소",
                    DefaultButton = ContentDialogButton.Primary
                };

                if (Application.Current.MainWindow?.FindName("RootContentDialog") is ContentPresenter contentPresenter)
                {
                    dialog.DialogHost = contentPresenter;
                }

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // 설정 적용
                    settingsDialog.ApplySettings(_canCommService.Settings);
                    _logService.AddSystemLog($"CAN 설정 변경: {_canCommService.Settings.InterfaceType}");
                }
            }
            catch (Exception ex)
            {
                _logService.AddSystemErrorLog($"CAN 설정 오류: {ex.Message}");
            }
        }

        private void OnCANConnectionStatusChanged(object sender, string status)
        {
            _logService.AddSystemLog($"CAN 통신 상태: {status}");
        }

        private void OnCANErrorOccurred(object sender, string error)
        {
            _logService.AddSystemErrorLog($"CAN 통신 오류: {error}");
        }

        public void OnNavigatedTo()
        {
            _logService.LogDashboardAccess();
        }

        public void OnNavigatedFrom()
        {
            // Dashboard에서 다른 페이지로 이동할 때 실행
        }
    }
}