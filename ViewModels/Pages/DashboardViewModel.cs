using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;
using static Peak.Can.Basic.ParameterValue;

namespace PSTARV2MonitoringApp.Views.Pages
{
    public class RawData
    {
        public string Timestamp { get; set; } = "";
        public string CanId { get; set; } = "";
        public int STBY_Start { get; set; }
        public int RunLamp { get; set; }
        public int Overload { get; set; }
        public int ModeStatus { get; set; }
        public int RUN_req { get; set; }
        public int ResetButton { get; set; }
        public int StandByLamp { get; set; }
        public int TXLowpress { get; set; }
    }

    public class DeviceStatusCardModel : INotifyPropertyChanged
    {
        private string _title = "";
        public string Title { get => _title; set { _title = value; OnPropertyChanged(nameof(Title)); } }

        private string _commStatus = "";
        public string CommStatus { get => _commStatus; set { _commStatus = value; OnPropertyChanged(nameof(CommStatus)); } }

        private string _standbyLamp = "";
        public string StandbyLamp { get => _standbyLamp; set { _standbyLamp = value; OnPropertyChanged(nameof(StandbyLamp)); } }

        private string _runStatus = "";
        public string RunStatus { get => _runStatus; set { _runStatus = value; OnPropertyChanged(nameof(RunStatus)); } }

        private string _mode = "";
        public string Mode { get => _mode; set { _mode = value; OnPropertyChanged(nameof(Mode)); } }

        private string _standbyStart = "";
        public string StandbyStart { get => _standbyStart; set { _standbyStart = value; OnPropertyChanged(nameof(StandbyStart)); } }

        private string _overload = "";
        public string Overload { get => _overload; set { _overload = value; OnPropertyChanged(nameof(Overload)); } }

        private string _lowpress = "";
        public string Lowpress { get => _lowpress; set { _lowpress = value; OnPropertyChanged(nameof(Lowpress)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }


    // ======= Page ViewModel =======
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private const int RAW_CAPACITY = 30;   // Raw Data 상한
        private const int LOG_CAPACITY = 30;   // 종합 상황(로그) 상한


        public ObservableCollection<RawData> RawDataItems { get; } = new();
        public ObservableCollection<DeviceStatusCardModel> DeviceStatusCardModels { get; } = new();

        //----------------------------------------------------------------------------------------//
        private readonly DeviceLogService _logService;
        private readonly CANCommunicationService _canService;

        // _filteredDeviceLogs를 public 속성으로 변경
        private ObservableCollection<DeviceLogEntry> _filteredDeviceLogs;
        public ObservableCollection<DeviceLogEntry> FilteredDeviceLogs => _filteredDeviceLogs;
        public ObservableCollection<DeviceLogEntry> DeviceLogs => _logService.LogEntries;

        //----------------------------------------------------------------------------------------//

        private static string NowWithMs() => DateTime.Now.ToString("HH:mm:ss.ff");

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
        //----------------------------------------------------------------------------------------//

        public DashboardViewModel()
        {
            _logService = DeviceLogService.Instance;
            _canService = CANCommunicationService.Instance;

            _filteredDeviceLogs = new ObservableCollection<DeviceLogEntry>();
            // DeviceLogService의 로그 변경 이벤트 구독
            _logService.LogEntries.CollectionChanged += LogEntries_CollectionChanged;

            // CAN 통신 이벤트 구독
            _canService.CANDataReceived += OnCANDataReceived;
            //_canService.ConnectionStatusChanged += OnConnectionStatusChanged;

            // CAN 설정을 테스트 모드로 초기화
            _canService.Settings.InterfaceType = "TEST";
            _canService.Settings.Channel = "Virtual";
            _canService.Settings.BaudRate = 500000;


            // 초기 Device 카드 3개
            DeviceStatusCardModels.Add(new DeviceStatusCardModel { Title = "ID 1" });
            DeviceStatusCardModels.Add(new DeviceStatusCardModel { Title = "ID 2" });
            DeviceStatusCardModels.Add(new DeviceStatusCardModel { Title = "ID 3" });
        }

        private void LogEntries_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // 로그 변경 시 _filteredDeviceLogs 업데이트
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.NewItems != null)
                {
                    foreach (DeviceLogEntry entry in e.NewItems)
                    {
                        Console.WriteLine($"New Log: {entry.Date} - {entry.Id} - {entry.Contents}");
                        _filteredDeviceLogs.Insert(0, entry); // 새 로그를 목록 맨 위에 추가
                    }
                }

                // 로그 최대 개수 제한
                while (_filteredDeviceLogs.Count > LOG_CAPACITY)
                {
                    _filteredDeviceLogs.RemoveAt(_filteredDeviceLogs.Count - 1);
                }

                // 필터링된 로그 변경 알림
                OnPropertyChanged(nameof(FilteredDeviceLogs));
            });
        }


        private void OnCANDataReceived(object sender, CANDataReceivedEventArgs e)
        {
            var id = $"0x{e.Frame.Id:X3}";
            var data = e.Frame.Data;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var rawData = new RawData
                {
                    Timestamp = NowWithMs(),
                    CanId = id,
                    STBY_Start = data[0],
                    RunLamp = data[1],
                    Overload = data[2],
                    ModeStatus = data[3],
                    RUN_req = data[4],
                    ResetButton = data[5],
                    StandByLamp = data[6],
                    TXLowpress = data[7]
                };

                //맨 앞에 추가
                RawDataItems.Insert(0, rawData);

                //최대 수 제한
                if (RawDataItems.Count > RAW_CAPACITY)
                {
                    RawDataItems.RemoveAt(RawDataItems.Count - 1);
                }

                if (id == "0x100") // Device 1
                {
                    //DeviceStatusCardModels[0].Title = "ID 1";
                    DeviceStatusCardModels[0].CommStatus = "Connected";
                    DeviceStatusCardModels[0].StandbyStart = (data[0] == 1) ? "STBY Start" : "STBY Stop";
                    DeviceStatusCardModels[0].RunStatus = (data[1] == 1) ? "RUN" : "STOP";
                    DeviceStatusCardModels[0].Overload = (data[2] == 1) ? "ON" : "OFF";
                    DeviceStatusCardModels[0].Mode = (data[3] == 1) ? "StandBy" : "Manual";
                    DeviceStatusCardModels[0].StandbyLamp = (data[6] == 1) ? "ON" : "OFF";
                    DeviceStatusCardModels[0].Lowpress = (data[7] == 1) ? "ON" : "OFF";
                }
                else if (id == "0x200") // Device 2
                {
                    //DeviceStatusCardModels[1].Title = "ID 2";
                    DeviceStatusCardModels[1].CommStatus = "Connected";
                    DeviceStatusCardModels[1].StandbyStart = (data[0] == 1) ? "STBY Start" : "STBY Stop";
                    DeviceStatusCardModels[1].RunStatus = (data[1] == 1) ? "RUN" : "STOP";
                    DeviceStatusCardModels[1].Overload = (data[2] == 1) ? "ON" : "OFF";
                    DeviceStatusCardModels[1].Mode = (data[3] == 1) ? "StandBy" : "Manual";
                    DeviceStatusCardModels[1].StandbyLamp = (data[6] == 1) ? "ON" : "OFF";
                    DeviceStatusCardModels[1].Lowpress = (data[7] == 1) ? "ON" : "OFF";
                }
                else if (id == "0x300") // Device 3
                {
                    //DeviceStatusCardModels[2].Title = "ID 3";
                    DeviceStatusCardModels[2].CommStatus = "Connected";
                    DeviceStatusCardModels[2].StandbyStart = (data[0] == 1) ? "STBY Start" : "STBY Stop";
                    DeviceStatusCardModels[2].RunStatus = (data[1] == 1) ? "RUN" : "STOP";
                    DeviceStatusCardModels[2].Overload = (data[2] == 1) ? "ON" : "OFF";
                    DeviceStatusCardModels[2].Mode = (data[3] == 1) ? "StandBy" : "Manual";
                    DeviceStatusCardModels[2].StandbyLamp = (data[6] == 1) ? "ON" : "OFF";
                    DeviceStatusCardModels[2].Lowpress = (data[7] == 1) ? "ON" : "OFF";
                }
            });
        }
    }
}
