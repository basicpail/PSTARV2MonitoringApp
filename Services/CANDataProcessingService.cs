using PSTARV2MonitoringApp.Models;
using PSTARV2MonitoringApp.ViewModels.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PSTARV2MonitoringApp.Services
{
    /// <summary>
    /// CAN �����͸� ó���ϰ� �� ������Ʈ�� �����ϴ� �߾� ����
    /// </summary>
    public class CANDataProcessingService
    {
        private static CANDataProcessingService _instance;
        public static CANDataProcessingService Instance => _instance ??= new CANDataProcessingService();

        private readonly CANCommunicationService _canService;
        private readonly DeviceLogService _logService;
        private readonly Dictionary<uint, string> _deviceIdMapping;

        // �� ���� ����
        private DeviceStatusCardViewModel _deviceStatusCardViewModel;
        private readonly Dictionary<string, PSTARDevicePanelViewModel> _devicePanelViewModels;

        private CANDataProcessingService()
        {
            _canService = CANCommunicationService.Instance;
            _logService = DeviceLogService.Instance;
            _devicePanelViewModels = new Dictionary<string, PSTARDevicePanelViewModel>();
            
            // CAN ID�� ��ġ ID ����
            _deviceIdMapping = new Dictionary<uint, string>
            {
                { 0x100, "1" },  // CAN ID 0x100 -> Device ID 1
                { 0x101, "2" },  // CAN ID 0x101 -> Device ID 2
                { 0x102, "3" }   // CAN ID 0x102 -> Device ID 3
            };

            // CAN ������ ���� �̺�Ʈ ����
            _canService.DataReceived += OnCANDataReceived;
            _canService.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        /// <summary>
        /// ���� ���� ���
        /// </summary>
        public void RegisterServices(DeviceStatusCardViewModel deviceStatusCardViewModel)
        {
            _deviceStatusCardViewModel = deviceStatusCardViewModel;
        }

        /// <summary>
        /// ��ġ �г� ViewModel ���
        /// </summary>
        public void RegisterDevicePanel(string deviceId, PSTARDevicePanelViewModel viewModel)
        {
            _devicePanelViewModels[deviceId] = viewModel;
        }

        /// <summary>
        /// ��ġ �г� ViewModel ��� ����
        /// </summary>
        public void UnregisterDevicePanel(string deviceId)
        {
            _devicePanelViewModels.Remove(deviceId);
        }

        /// <summary>
        /// CAN ������ ���� ó��
        /// </summary>
        private void OnCANDataReceived(object sender, CANDataReceivedEventArgs e)
        {
            try
            {
                var frame = e.Frame;
                
                // CAN ID�� ��ġ �ĺ�
                if (!_deviceIdMapping.TryGetValue(frame.Id, out var deviceId))
                {
                    // �� �� ���� CAN ID
                    _logService.AddSystemLog($"�� �� ���� CAN ID: 0x{frame.Id:X3}");
                    return;
                }

                // CAN ������ �ؼ�
                var deviceData = ParseCANData(deviceId, frame);
                
                // �� ������Ʈ ������Ʈ
                UpdateDeviceStatusCard(deviceData);
                UpdateDevicePanel(deviceData);
                UpdateRawData(deviceData);
                
                // �α� ���
                LogDataChanges(deviceData);
            }
            catch (Exception ex)
            {
                _logService.AddSystemErrorLog($"CAN ������ ó�� ����: {ex.Message}");
            }
        }

        /// <summary>
        /// CAN ������ �����͸� ��ġ �����ͷ� ��ȯ (���� ������ ����)
        /// </summary>
        private DeviceCANData ParseCANData(string deviceId, CANFrame frame)
        {
            var data = new DeviceCANData
            {
                DeviceId = deviceId,
                Timestamp = frame.Timestamp
            };

            // ���� ������ ���Ŀ� �°� �Ľ�
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

                // Raw Data�� ������� ��ġ ���� ����
                data.IsRunning = data.RunLamp;
                data.IsStandby = data.StandByLamp;
                data.IsLowPressure = data.TXLowpress;
                data.IsAbnormal = data.Overload;
                data.IsManualMode = data.ModeStatus;
                data.IsSourceOn = data.STBY_Start;
                data.IsStopped = !data.RunLamp;
                data.IsHeatOn = data.RunLamp || data.StandByLamp;

                // ���� ���ڿ� ��ȯ
                data.CommStatus = "Connected"; // CAN �����͸� �ް� �����Ƿ� �����
                data.RunStatus = data.RunLamp ? "Running" : "Stopped";
                data.RunMode = data.ModeStatus ? "Manual" : "Auto";
                data.StandByStatus = data.StandByLamp ? "Standby" : "Ready";
                data.OverloadStatus = data.Overload ? "Warning" : "Normal";
                data.LowPressureStatus = data.TXLowpress ? "Low" : "Normal";
            }

            return data;
        }

        /// <summary>
        /// DeviceStatusCard ������Ʈ
        /// </summary>
        private void UpdateDeviceStatusCard(DeviceCANData data)
        {
            if (_deviceStatusCardViewModel == null) return;

            var deviceInfo = DeviceInfo.GetDeviceIdById(data.DeviceId);
            var statusCardId = deviceInfo?.StatusCardId;

            if (string.IsNullOrEmpty(statusCardId)) return;

            // ��ġ �г� ���� ã�Ƽ� ���� ī�� ������Ʈ
            if (_devicePanelViewModels.TryGetValue(data.DeviceId, out var viewModel) &&
                viewModel.DeviceModel != null)
            {
                // PSTARDeviceModel �����͸� ����Ͽ� ���� ī�� ������Ʈ
                _deviceStatusCardViewModel.UpdateFromDeviceModel(statusCardId, viewModel.DeviceModel);
            }
            else
            {
                // ��ġ �г� ���� ã�� �� ���� ��� CAN �����ͷ� ���� ������Ʈ
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
        /// PSTARDevicePanel ������Ʈ
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

                // �߰������� DeviceModel�� �ٸ� �Ӽ��鵵 ������Ʈ
                if (viewModel.DeviceModel != null)
                {
                    viewModel.DeviceModel.IsHeatOn = data.IsHeatOn;
                    viewModel.DeviceModel.IsManualMode = data.IsManualMode;
                    viewModel.DeviceModel.IsStandbyMode = data.IsStandbyMode;
                }
            }
        }

        /// <summary>
        /// Raw Data ������Ʈ (DashboardViewModel�� ����)
        /// </summary>
        private void UpdateRawData(DeviceCANData data)
        {
            // Raw Data�� DashboardViewModel���� ���� ó���ϰų�
            // ������ �̺�Ʈ�� ������ �� ����
            RawDataUpdated?.Invoke(this, new RawDataUpdatedEventArgs(data));
        }

        /// <summary>
        /// ������ ���� �α� ���
        /// </summary>
        private void LogDataChanges(DeviceCANData data)
        {
            var deviceInfo = DeviceInfo.GetDeviceIdById(data.DeviceId);
            var displayId = deviceInfo?.DisplayText ?? data.DeviceId;

            // �߿��� ���� ���游 �α� ���
            if (data.IsAbnormal)
            {
                _logService.AddLog(displayId, "������ ���� ����");
            }

            if (data.IsCommFailure)
            {
                _logService.AddLog(displayId, "��� ���� �߻�");
            }

            if (data.IsLowPressure)
            {
                _logService.AddLog(displayId, "���� ���� ����");
            }

            // ���� ���� �α� (�ʿ信 ����)
            _logService.AddLog(displayId, $"CAN ������ ����: {data.CommStatus}, {data.RunStatus}");
        }

        /// <summary>
        /// ���� ���� ���� ó��
        /// </summary>
        private void OnConnectionStatusChanged(object sender, string status)
        {
            _logService.AddSystemLog($"CAN ��� ����: {status}");
        }

        /// <summary>
        /// Raw Data ������Ʈ �̺�Ʈ
        /// </summary>
        public event EventHandler<RawDataUpdatedEventArgs> RawDataUpdated;

        /// <summary>
        /// CAN ��� ����
        /// </summary>
        public async Task StartCommunicationAsync()
        {
            await _canService.StartAsync();
        }

        /// <summary>
        /// CAN ��� ����
        /// </summary>
        public async Task StopCommunicationAsync()
        {
            await _canService.StopAsync();
        }
    }

    /// <summary>
    /// Raw Data ������Ʈ �̺�Ʈ �μ�
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