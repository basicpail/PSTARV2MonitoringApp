using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;
using static Peak.Can.Basic.ParameterValue;

namespace PSTARV2MonitoringApp.Views.Pages
{
    // ======= Models =======
    public class RawDataRow
    {
        public string Timestamp { get; set; } = "";
        public string CanId { get; set; } = "";
        public object STBY_Start { get; set; } = false;   // bool 또는 "ON"/"OFF" 모두 수용
        public object RunLamp { get; set; } = false;
        public object Overload { get; set; } = false;
        public string ModeStatus { get; set; } = "OFF";
        public object RUN_req { get; set; } = false;
        public object ResetButton { get; set; } = false;
        public object StandByLamp { get; set; } = false;
        public object TXLowpress { get; set; } = false;
    }

    public class LogRow
    {
        public string Timestamp { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string Message { get; set; } = "";
    }

    public class DeviceCardModel : INotifyPropertyChanged
    {
        public string Title { get; set; } = "ID";
        private string _commStatus = "Disconnected";
        public string CommStatus { get => _commStatus; set { _commStatus = value; OnPropertyChanged(nameof(CommStatus)); } }

        private string _runStatus = "Stopped";
        public string RunStatus { get => _runStatus; set { _runStatus = value; OnPropertyChanged(nameof(RunStatus)); } }

        private string _mode = "Manual";
        public string Mode { get => _mode; set { _mode = value; OnPropertyChanged(nameof(Mode)); } }

        private int _rxRate;
        public int RxRate { get => _rxRate; set { _rxRate = value; OnPropertyChanged(nameof(RxRate)); } }

        private int _txRate;
        public int TxRate { get => _txRate; set { _txRate = value; OnPropertyChanged(nameof(TxRate)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class DeviceStatusCardsVM
    {
        public ObservableCollection<DeviceCardModel> DeviceStatusCardModels { get; } = new();
    }

    // ======= Page ViewModel =======
    public class Dashboard2ViewModel : INotifyPropertyChanged
    {
        private const int RAW_CAPACITY = 99;   // Raw Data 상한
        private const int LOG_CAPACITY = 99;   // 종합 상황(로그) 상한


        public ObservableCollection<RawDataRow> RawDataItems { get; } = new();
        public ObservableCollection<LogRow> LogItems { get; } = new();

        public ObservableCollection<string> AvailableCanIds { get; } =
            new(new[] { "모든 ID", "0x100", "0x200", "0x300" });

        private string _selectedCanIdFilter = "모든 ID";
        public string SelectedCanIdFilter
        {
            get => _selectedCanIdFilter;
            set { _selectedCanIdFilter = value; OnPropertyChanged(nameof(SelectedCanIdFilter)); }
        }

        private bool _showAllCanMessages = true;
        public bool ShowAllCanMessages
        {
            get => _showAllCanMessages;
            set { _showAllCanMessages = value; OnPropertyChanged(nameof(ShowAllCanMessages)); }
        }

        public DeviceStatusCardsVM DeviceStatusCardViewModel { get; } = new();

        private readonly DispatcherTimer _timer;
        private readonly Random _rand = new();

        public Dashboard2ViewModel()
        {
            // 초기 Device 카드 3개
            DeviceStatusCardViewModel.DeviceStatusCardModels.Add(new DeviceCardModel { Title = "ID1", CommStatus = "Connected", RunStatus = "Running", Mode = "Auto", RxRate = 58, TxRate = 52 });
            DeviceStatusCardViewModel.DeviceStatusCardModels.Add(new DeviceCardModel { Title = "ID2", CommStatus = "Disconnected", RunStatus = "Stopped", Mode = "Manual", RxRate = 0, TxRate = 0 });
            DeviceStatusCardViewModel.DeviceStatusCardModels.Add(new DeviceCardModel { Title = "ID3", CommStatus = "Disconnected", RunStatus = "Stopped", Mode = "Manual", RxRate = 0, TxRate = 0 });

            // 초기 로그/RawData 몇 줄
            for (int i = 0; i < 18; i++)
            {
                AddRandomRawRow();
            }
            for (int i = 0; i < 8; i++)
            {
                LogItems.Add(new LogRow
                {
                    Timestamp = Now(),
                    DeviceId = $"ID {_rand.Next(1, 4)}",
                    Message = _rand.Next(3) switch
                    {
                        0 => "CAN 데이터 수신: Connected, Running",
                        1 => "저압 상태 감지",
                        _ => "알 수 없는 CAN ID"
                    }
                });
            }

            // 타이머로 지속 갱신
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _timer.Tick += (s, e) =>
            {
                // RawData 한 줄 추가
                AddRandomRawRow();

                // 로그 가끔 추가
                if (_rand.NextDouble() < 0.35)
                {
                    LogRow log = new LogRow
                    {
                        Timestamp = Now(),
                        DeviceId = $"ID {_rand.Next(1, 4)}",
                        Message = _rand.Next(4) switch
                        {
                            0 => "CAN 데이터 수신: Connected, Running",
                            1 => "상태 변경: Manual → Auto",
                            2 => "저압 상태 감지",
                            _ => "CAN ID: 0x300"
                        }
                    };

                    AddNewestFirst(LogItems, log, LOG_CAPACITY);

                    if (LogItems.Count > 200) LogItems.RemoveAt(LogItems.Count - 1);
                }

                // Device 카드 Rx/Tx 변화, 연결 상태 랜덤 토글
                foreach (var d in DeviceStatusCardViewModel.DeviceStatusCardModels)
                {
                    d.RxRate = Math.Max(0, d.RxRate + _rand.Next(-5, 6));
                    d.TxRate = Math.Max(0, d.TxRate + _rand.Next(-5, 6));

                    if (_rand.NextDouble() < 0.08)
                    {
                        bool connected = d.CommStatus == "Connected";
                        d.CommStatus = connected ? "Disconnected" : "Connected";
                        d.RunStatus = connected ? "Stopped" : "Running";
                        d.Mode = connected ? "Manual" : "Auto";
                    }
                }

                // RawData 사이즈 유지 (성능/시야)
                while (RawDataItems.Count > 400) RawDataItems.RemoveAt(0);
            };
            _timer.Start();
        }

        private void AddRandomRawRow()
        {
            string[] ids = { "0x100", "0x200", "0x300" };
            string canId = ids[_rand.Next(ids.Length)];
            bool onOff() => _rand.NextDouble() < 0.25;

            var row = new RawDataRow
            {
                Timestamp = NowWithMs(),
                CanId = canId,
                STBY_Start = onOff(),
                RunLamp = onOff(),
                Overload = _rand.NextDouble() < 0.07,
                ModeStatus = _rand.Next(3) switch { 0 => "OFF", 1 => "AUTO", _ => "MAN" },
                RUN_req = onOff(),
                ResetButton = onOff(),
                StandByLamp = onOff(),
                TXLowpress = _rand.NextDouble() < 0.05
            };

            AddNewestFirst(RawDataItems, row, RAW_CAPACITY);
        }

        private static void AddNewestFirst<T>(ObservableCollection<T> list, T item, int capacity)
        {
            list.Insert(0, item);                  // ✅ 항상 맨 위에 추가
            if (list.Count > capacity)             // ✅ 오래된 건 '맨 아래'에서 제거
                list.RemoveAt(list.Count - 1);
        }


        private static string Now() => DateTime.Now.ToString("HH:mm:ss");
        private static string NowWithMs() => DateTime.Now.ToString("HH:mm:ss.ff");

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
