using PSTARV2MonitoringApp.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace PSTARV2MonitoringApp.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty]
        private int _testValue = 0; // 테스트용 값

        private const int MaxItems = 16;          // 할당 할 최대 데이터 수
        private readonly Random _rnd = new Random();
        private readonly DispatcherTimer _timer;

        public ObservableCollection<RawDataTestModel> TestRawDataTimelineItems { get; } = new ObservableCollection<RawDataTestModel>();
        public ObservableCollection<SituationLogTestModel> TestSituationLogTestItems { get; } = new ObservableCollection<SituationLogTestModel>();
        
        
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
            for (int i = 0; i < MaxItems; i++)
            {
                TestSituationLogTestItems.Add(new SituationLogTestModel
                {
                    Date = DateTime.Now.AddMinutes(-i).ToString("yyyy-MM-dd HH:mm:ss"),
                    Id = $"ID_{i + 1}",
                    Contents = $"Test log content {i + 1}"
                });
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged(string propName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
