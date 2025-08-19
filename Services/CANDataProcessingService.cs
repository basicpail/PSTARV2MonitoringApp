using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.ViewModels.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PSTARV2MonitoringApp.Services
{
    /// <summary>
    /// CAN 데이터를 처리하고 각 컴포넌트에 배포하는 중앙 서비스
    /// </summary>
    public class CANDataProcessingService
    {
        private static CANDataProcessingService _instance;
        public static CANDataProcessingService Instance => _instance ??= new CANDataProcessingService();

        private readonly CANCommunicationService _canService;
        private readonly DeviceLogService _logService;
        private readonly Dictionary<uint, string> _deviceIdMapping;

        // 각 서비스 참조
        private DeviceStatusCardViewModel _deviceStatusCardViewModel;
        private readonly Dictionary<string, PSTARDevicePanelViewModel> _devicePanelViewModels;

        private CANDataProcessingService()
        {
            _canService = CANCommunicationService.Instance;
            _logService = DeviceLogService.Instance;
            _devicePanelViewModels = new Dictionary<string, PSTARDevicePanelViewModel>();
            
            // CAN ID와 장치 ID 매핑
            _deviceIdMapping = new Dictionary<uint, string>
            {
                { 0x100, "1" },  // CAN ID 0x100 -> Device ID 1
                { 0x101, "2" },  // CAN ID 0x101 -> Device ID 2
                { 0x102, "3" }   // CAN ID 0x102 -> Device ID 3
            };

            // CAN 데이터 수신 이벤트 구독
            _canService.DataReceived += OnCANDataReceived;
            _canService.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        /// <summary>
        /// 서비스 참조 등록
        /// </summary>
        public void RegisterServices(DeviceStatusCardViewModel deviceStatusCardViewModel)
        {
            _deviceStatusCardViewModel = deviceStatusCardViewModel;
        }

        /// <summary>
        /// 장치 패널 ViewModel 등록
        /// </summary>
        public void RegisterDevicePanel(string deviceId, PSTARDevicePanelViewModel viewModel)
        {
            _devicePanelViewModels[deviceId] = viewModel;
        }

        /// <summary>
        /// 장치 패널 ViewModel 등록 해제
        /// </summary>
        public void UnregisterDevicePanel(string deviceId)
        {
            _devicePanelViewModels.Remove(deviceId);
        }

        /// <summary>
        /// CAN 데이터 수신 처리
        /// </summary>
        private void OnCANDataReceived(object sender, CANDataReceivedEventArgs e)
        {
            try
            {
                var frame = e.Frame;
                
                // CAN ID로 장치 식별
                if (!_deviceIdMapping.TryGetValue(frame.Id, out var deviceId))
                {
                    // 알 수 없는 CAN ID
                    _logService.AddSystemLog($"알 수 없는 CAN ID: 0x{frame.Id:X3}");
                    return;
                }

                // CAN 데이터 해석
                var deviceData = ParseCANData(deviceId, frame);
                
                // 각 컴포넌트 업데이트
                UpdateDeviceStatusCard(deviceData);
                UpdateDevicePanel(deviceData);
                UpdateRawData(deviceData);
                
                // 로그 기록
                LogDataChanges(deviceData);
            }
            catch (Exception ex)
            {
                _logService.AddSystemErrorLog($"CAN 데이터 처리 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// CAN 프레임 데이터를 장치 데이터로 변환 (실제 데이터 형식)
        /// </summary>
        private DeviceCANData ParseCANData(string deviceId, CANFrame frame)
        {
            var data = new DeviceCANData
            {
                DeviceId = deviceId,
                Timestamp = frame.Timestamp
            };

            // 실제 데이터 형식에 맞게 파싱
            // tx_data[0] = STBY_Start;
            // tx_data[1] = RunLamp;
            // tx_data[2] = Overload;
            // tx_data[3] = ModeStatus;
            // tx_data[4] = RUN_req;
            // tx_data[5] = ResetButton;
            // tx_data[6] = StandByLamp;
            // tx_data[7] = TXLowpress;

            if (frame.Data != null && frame.Data.Length >= 8)
            {
                data.STBY_Start = frame.Data[0] == 1;
                data.RunLamp = frame.Data[1] == 1;
                data.Overload = frame.Data[2] == 1;
                data.ModeStatus = frame.Data[3] == 1;
                data.RUN_req = frame.Data[4] == 1;
                data.ResetButton = frame.Data[5] == 1;
                data.StandByLamp = frame.Data[6] == 1;
                data.TXLowpress = frame.Data[7] == 1;

                // Raw Data를 기반으로 장치 상태 매핑
                data.IsRunning = data.RunLamp;
                data.IsStandby = data.StandByLamp;
                data.IsLowPressure = data.TXLowpress;
                data.IsAbnormal = data.Overload;
                data.IsManualMode = data.ModeStatus;
                data.IsSourceOn = data.STBY_Start;
                data.IsStopped = !data.RunLamp;
                data.IsHeatOn = data.RunLamp || data.StandByLamp;

                // 상태 문자열 변환
                data.CommStatus = "Connected"; // CAN 데이터를 받고 있으므로 연결됨
                data.RunStatus = data.RunLamp ? "Running" : "Stopped";
                data.RunMode = data.ModeStatus ? "Manual" : "Auto";
                data.StandByStatus = data.StandByLamp ? "Standby" : "Ready";
                data.OverloadStatus = data.Overload ? "Warning" : "Normal";
                data.LowPressureStatus = data.TXLowpress ? "Low" : "Normal";
            }

            return data;
        }

        /// <summary>
        /// DeviceStatusCard 업데이트
        /// </summary>
        private void UpdateDeviceStatusCard(DeviceCANData data)
        {
            if (_deviceStatusCardViewModel == null) return;

            var deviceInfo = DeviceInfo.GetDeviceIdById(data.DeviceId);
            var statusCardId = deviceInfo?.StatusCardId;

            if (string.IsNullOrEmpty(statusCardId)) return;

            // 장치 패널 모델을 찾아서 상태 카드 업데이트
            if (_devicePanelViewModels.TryGetValue(data.DeviceId, out var viewModel) &&
                viewModel.DeviceModel != null)
            {
                // PSTARDeviceModel 데이터를 사용하여 상태 카드 업데이트
                _deviceStatusCardViewModel.UpdateFromDeviceModel(statusCardId, viewModel.DeviceModel);
            }
            else
            {
                // 장치 패널 모델을 찾을 수 없는 경우 CAN 데이터로 직접 업데이트
                var cardModel = new DeviceStatusCardModel
                {
                    DeviceId = statusCardId,
                    CommStatus = data.CommStatus,
                    RunStatus = data.RunStatus,
                    RunMode = data.RunMode,
                    StandByStatus = data.StandByStatus,
                    OverloadStatus = data.OverloadStatus,
                    LowPressureStatus = data.LowPressureStatus
                };

                _deviceStatusCardViewModel.AddDeviceStatusCard(cardModel);
            }
        }
        /// <summary>
        /// PSTARDevicePanel 업데이트
        /// </summary>
        private void UpdateDevicePanel(DeviceCANData data)
        {
            if (_devicePanelViewModels.TryGetValue(data.DeviceId, out var viewModel))
            {
                viewModel.UpdateDeviceState(
                    isSourceOn: data.IsSourceOn,
                    isAbnormal: data.IsAbnormal,
                    isRunning: data.IsRunning,
                    isStopped: data.IsStopped,
                    isHeating: data.IsHeating,
                    isCommFailure: data.IsCommFailure,
                    isLowPressure: data.IsLowPressure,
                    isStandby: data.IsStandby
                );

                // 추가적으로 DeviceModel의 다른 속성들도 업데이트
                if (viewModel.DeviceModel != null)
                {
                    viewModel.DeviceModel.IsHeatOn = data.IsHeatOn;
                    viewModel.DeviceModel.IsManualMode = data.IsManualMode;
                    viewModel.DeviceModel.IsStandbyMode = data.IsStandbyMode;
                }
            }
        }

        /// <summary>
        /// Raw Data 업데이트 (DashboardViewModel에 전달)
        /// </summary>
        private void UpdateRawData(DeviceCANData data)
        {
            // Raw Data는 DashboardViewModel에서 직접 처리하거나
            // 별도의 이벤트로 전달할 수 있음
            RawDataUpdated?.Invoke(this, new RawDataUpdatedEventArgs(data));
        }

        /// <summary>
        /// 데이터 변경 로그 기록
        /// </summary>
        private void LogDataChanges(DeviceCANData data)
        {
            var deviceInfo = DeviceInfo.GetDeviceIdById(data.DeviceId);
            var displayId = deviceInfo?.DisplayText ?? data.DeviceId;

            // 중요한 상태 변경만 로그 기록
            if (data.IsAbnormal)
            {
                _logService.AddLog(displayId, "비정상 상태 감지");
            }

            if (data.IsCommFailure)
            {
                _logService.AddLog(displayId, "통신 오류 발생");
            }

            if (data.IsLowPressure)
            {
                _logService.AddLog(displayId, "저압 상태 감지");
            }

            // 상태 변경 로그 (필요에 따라)
            _logService.AddLog(displayId, $"CAN 데이터 수신: {data.CommStatus}, {data.RunStatus}");
        }

        /// <summary>
        /// 연결 상태 변경 처리
        /// </summary>
        private void OnConnectionStatusChanged(object sender, string status)
        {
            _logService.AddSystemLog($"CAN 통신 상태: {status}");
        }

        /// <summary>
        /// Raw Data 업데이트 이벤트
        /// </summary>
        public event EventHandler<RawDataUpdatedEventArgs> RawDataUpdated;

        /// <summary>
        /// CAN 통신 시작
        /// </summary>
        public async Task StartCommunicationAsync()
        {
            await _canService.StartAsync();
        }

        /// <summary>
        /// CAN 통신 중지
        /// </summary>
        public async Task StopCommunicationAsync()
        {
            await _canService.StopAsync();
        }
    }

    /// <summary>
    /// Raw Data 업데이트 이벤트 인수
    /// </summary>
    public class RawDataUpdatedEventArgs : EventArgs
    {
        public DeviceCANData Data { get; }

        public RawDataUpdatedEventArgs(DeviceCANData data)
        {
            Data = data;
        }
    }
}