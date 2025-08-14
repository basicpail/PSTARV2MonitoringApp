using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.Views.Dialogs;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;

namespace PSTARV2MonitoringApp.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _testValue = 0; // 테스트용 값

        [ObservableProperty]
        private string? _selectedIdFilter = null; // 선택된 ID 필터

        private const int MaxItems = 20;      // 할당 할 최대 데이터 수
        private readonly Random _rnd = new Random();
        private readonly DispatcherTimer _timer;

        // 원본 데이터
        private readonly ObservableCollection<SituationLogTestModel> _allSituationLogItems = new();

        public ObservableCollection<RawDataTestModel> TestRawDataTimelineItems { get; } = new ObservableCollection<RawDataTestModel>();
        public ObservableCollection<SituationLogTestModel> TestSituationLogTestItems { get; } = new ObservableCollection<SituationLogTestModel>();

        // 사용 가능한 ID 목록
        public ObservableCollection<string> AvailableIds { get; } = new ObservableCollection<string>();

        public DashboardViewModel()
        {
            // 1초마다 Tick
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (s, e) => AddRandomData();
            _timer.Start();

            //몇 개의 초기 데이터를 추가
            InitializeTestData();
            initializeSituationLogTestData();
        }

        private void AddRandomData()
        {
            // 새 랜덤 데이터 생성
            var data = new RawDataTestModel
            {
                STBY_Start = RandOnOff(),
                RunLamp = RandOnOff(),
                Overload = RandOnOff(),
                ModeStatus = RandOnOff(),
                RUN_req = RandOnOff(),
                ResetButton = RandOnOff(),
                StandByLamp = RandOnOff(),
                TXLowpress = RandOnOff()
            };

            // 맨 앞(최신) 에 삽입
            TestRawDataTimelineItems.Insert(0, data);

            //개수 초과 시 맨 뒤(오래된) 제거
            if (TestRawDataTimelineItems.Count > MaxItems)
                TestRawDataTimelineItems.RemoveAt(TestRawDataTimelineItems.Count - 1);

            // 상황 로그에도 랜덤 데이터 추가
            AddRandomSituationLog();
        }

        private void AddRandomSituationLog()
        {
            var randomIds = new[] { "ID1", "ID2", "ID3" };
            var randomContents = new[] { "시스템 시작", "정상 운전", "알람 발생", "점검 완료", "통신 연결", "설정 변경" };

            var newLog = new SituationLogTestModel
            {
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Id = randomIds[_rnd.Next(randomIds.Length)],
                Contents = randomContents[_rnd.Next(randomContents.Length)]
            };

            // 원본 데이터에 추가
            _allSituationLogItems.Insert(0, newLog);

            // 개수 제한
            if (_allSituationLogItems.Count > MaxItems)
                _allSituationLogItems.RemoveAt(_allSituationLogItems.Count - 1);

            // 필터 적용하여 표시 데이터 업데이트
            ApplyIdFilter();

            // 사용 가능한 ID 목록 업데이트
            UpdateAvailableIds();
        }

        private string RandOnOff()
            => _rnd.NextDouble() < 0.5 ? "ON" : "OFF";

        public void InitializeTestData()
        {
            for (int i = 0; i < MaxItems; i++)
            {
                TestRawDataTimelineItems.Add(new RawDataTestModel
                {
                    STBY_Start = RandOnOff(),
                    RunLamp = RandOnOff(),
                    Overload = RandOnOff(),
                    ModeStatus = RandOnOff(),
                    RUN_req = RandOnOff(),
                    ResetButton = RandOnOff(),
                    StandByLamp = RandOnOff(),
                    TXLowpress = RandOnOff()
                });
            }
        }

        public void initializeSituationLogTestData()
        {
            var sampleIds = new[] { "ID1", "ID2", "ID3" };
            var sampleContents = new[] { "시스템 초기화", "정상 운전 중", "알람 해제", "점검 시작", "통신 정상", "설정 저장" };

            for (int i = 0; i < MaxItems; i++)
            {
                var logItem = new SituationLogTestModel
                {
                    Date = DateTime.Now.AddMinutes(-i).ToString("yyyy-MM-dd HH:mm:ss"),
                    Id = sampleIds[_rnd.Next(sampleIds.Length)],
                    Contents = sampleContents[_rnd.Next(sampleContents.Length)]
                };

                _allSituationLogItems.Add(logItem);
                TestSituationLogTestItems.Add(logItem);
            }

            UpdateAvailableIds();
        }

        private void UpdateAvailableIds()
        {
            var currentIds = _allSituationLogItems.Select(x => x.Id).Distinct().OrderBy(x => x).ToList();

            AvailableIds.Clear();
            foreach (var id in currentIds)
            {
                AvailableIds.Add(id);
            }
        }

        private void ApplyIdFilter()
        {
            TestSituationLogTestItems.Clear();

            var filteredItems = string.IsNullOrEmpty(SelectedIdFilter)
                ? _allSituationLogItems.ToList()
                : _allSituationLogItems.Where(x => x.Id == SelectedIdFilter).ToList();

            foreach (var item in filteredItems)
            {
                TestSituationLogTestItems.Add(item);
            }
        }

        [RelayCommand]
        public async Task ShowIdFilterDialog()
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

                    // 다이얼로그 크기 설정
                    //MinWidth = 380,
                    //MinHeight = 300,
                    //MaxWidth = 500,
                    //MaxHeight = 600,

                    // 컨텐츠 스타일 설정
                    //ContentMargin = new Thickness(0),
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

                    ApplyIdFilter();
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 일반 MessageBox 사용
                MessageBox.Show($"필터 다이얼로그 오류: {ex.Message}", "오류", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}