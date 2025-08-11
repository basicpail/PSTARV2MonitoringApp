using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSTARV2MonitoringApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using System.Windows;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;

namespace PSTARV2MonitoringApp.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _testValue = 0; // 테스트용 값

        [ObservableProperty]
        private string? _selectedIdFilter = null; // 선택된 ID 필터

        private const int MaxItems = 16;          // 할당 할 최대 데이터 수
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
                // 라디오 버튼들을 담을 StackPanel 생성
                var radioButtonPanel = new StackPanel();
                var radioButtons = new List<System.Windows.Controls.RadioButton>();

                // "전체 보기" 라디오 버튼 추가
                var allRadioButton = new System.Windows.Controls.RadioButton
                {
                    Content = "전체 보기",
                    GroupName = "IdFilter",
                    IsChecked = string.IsNullOrEmpty(SelectedIdFilter),
                    FontSize = 14,
                    //Margin = new Thickness(0, 5),
                    Foreground = Brushes.White,
                    Tag = null
                };
                radioButtons.Add(allRadioButton);
                radioButtonPanel.Children.Add(allRadioButton);

                // 사용 가능한 ID별 라디오 버튼 추가
                foreach (var id in AvailableIds.OrderBy(x => x))
                {
                    var radioButton = new System.Windows.Controls.RadioButton
                    {
                        Content = id,
                        GroupName = "IdFilter",
                        IsChecked = SelectedIdFilter == id,
                        FontSize = 14,
                        //Margin = new Thickness(0, 5),
                        Foreground = Brushes.White,
                        Tag = id
                    };
                    radioButtons.Add(radioButton);
                    radioButtonPanel.Children.Add(radioButton);
                }

                // ScrollViewer로 감싸기
                var scrollViewer = new ScrollViewer
                {
                    Content = radioButtonPanel,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    MaxHeight = 200
                };

                // 메인 Grid 생성
                var mainGrid = new Grid
                {
                    Margin = new Thickness(20),
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48))
                };

                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // 제목 TextBlock
                var titleTextBlock = new System.Windows.Controls.TextBlock
                {
                    Text = "필터링할 ID를 선택하세요:",
                    FontSize = 16,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                Grid.SetRow(titleTextBlock, 0);

                // ScrollViewer를 Grid에 추가
                Grid.SetRow(scrollViewer, 1);

                // 필터 해제 버튼
                var clearButton = new Wpf.Ui.Controls.Button
                {
                    Content = "필터 해제",
                    Background = new SolidColorBrush(Color.FromRgb(108, 117, 125)),
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 15, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                Grid.SetRow(clearButton, 2);

                // Grid에 요소들 추가
                mainGrid.Children.Add(titleTextBlock);
                mainGrid.Children.Add(scrollViewer);
                mainGrid.Children.Add(clearButton);

                // ContentDialog 생성
                var dialog = new ContentDialog
                {
                    Title = "ID 필터",
                    Content = mainGrid,
                    PrimaryButtonText = "확인",
                    SecondaryButtonText = "취소",
                    Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(63, 63, 70)),
                    BorderThickness = new Thickness(1),
                    Width = 400,
                    Height = 350
                };

                // ContentPresenter를 DialogHost로 설정
                if (Application.Current.MainWindow?.FindName("RootContentDialog") is ContentPresenter contentPresenter)
                {
                    dialog.DialogHost = contentPresenter;
                }

                // 필터 해제 버튼 이벤트
                clearButton.Click += (s, e) =>
                {
                    allRadioButton.IsChecked = true;
                    foreach (var rb in radioButtons.Where(rb => rb.Tag != null))
                    {
                        rb.IsChecked = false;
                    }
                };

                // 다이얼로그 표시
                var result = await dialog.ShowAsync();

                // 결과 처리
                if (result == ContentDialogResult.Primary)
                {
                    var selectedRadioButton = radioButtons.FirstOrDefault(rb => rb.IsChecked == true);
                    SelectedIdFilter = selectedRadioButton?.Tag?.ToString();
                    ApplyIdFilter();
                }
            }
            catch (Exception ex)
            {
                // 오류 발생 시 일반 MessageBox 사용
                MessageBox.Show($"필터 다이얼로그 오류: {ex.Message}", "오류", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 필터 해제 명령
        [RelayCommand]
        public void ClearIdFilter()
        {
            SelectedIdFilter = null;
            ApplyIdFilter();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged(string propName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}