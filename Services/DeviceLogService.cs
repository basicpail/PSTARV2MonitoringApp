using PSTARV2MonitoringApp.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PSTARV2MonitoringApp.Services
{
    /// <summary>
    /// ��ġ �α׸� �����ϴ� ����
    /// </summary>
    public class DeviceLogService
    {
        private static DeviceLogService _instance;
        public static DeviceLogService Instance => _instance ??= new DeviceLogService();

        private readonly ObservableCollection<DeviceLogEntry> _logEntries;
        private const int MaxLogEntries = 1000; // �ִ� �α� ����

        public ObservableCollection<DeviceLogEntry> LogEntries => _logEntries;

        private DeviceLogService()
        {
            _logEntries = new ObservableCollection<DeviceLogEntry>();
        }

        /// <summary>
        /// ��ġ ���� �α� �߰�
        /// </summary>
        /// <param name="deviceId">��ġ ID</param>
        /// <param name="action">���� ����</param>
        public void AddLog(string deviceId, string action)
        {
            // UI �����忡�� ����ǵ��� ����
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var logEntry = new DeviceLogEntry(deviceId, action);

                // �ֽ� �α׸� �� ���� �߰�
                _logEntries.Insert(0, logEntry);

                // �ִ� ���� �ʰ� �� ������ �α� ����
                while (_logEntries.Count > MaxLogEntries)
                {
                    _logEntries.RemoveAt(_logEntries.Count - 1);
                }
            });
        }

        #region �ý��� �α� �޼���
        public void AddSystemLog(string message)
            => AddLog("�ý���", message);

        public void AddSystemErrorLog(string errorMessage)
            => AddLog("�ý���", DeviceLogMessages.Format(DeviceLogMessages.System.Error, errorMessage));

        public void LogMonitoringStart()
            => AddSystemLog(DeviceLogMessages.System.MonitoringStart);

        public void LogDashboardAccess()
            => AddSystemLog(DeviceLogMessages.System.DashboardAccess);

        public void LogTestPageAccess()
            => AddSystemLog(DeviceLogMessages.System.TestPageAccess);

        public void LogCleared()
            => AddSystemLog(DeviceLogMessages.System.LogCleared);

        public void LogAllDevicesRemoved()
            => AddSystemLog(DeviceLogMessages.System.AllDevicesRemoved);
        #endregion

        #region ��ġ �α� �޼���
        public void LogDeviceAdded(string deviceId, string deviceModel)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            var message = DeviceLogMessages.Format(DeviceLogMessages.Device.Added, deviceModel);
            AddLog(displayId, message);
        }

        public void LogDeviceRemoved(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Device.Removed);
        }

        public void LogDeviceModelSet(string deviceId, string deviceModel)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            var message = DeviceLogMessages.Format(DeviceLogMessages.Device.ModelSet, deviceModel);
            AddLog(displayId, message);
        }

        public void LogDeviceMonitoringStart(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Device.MonitoringStart);
        }

        public void LogDeviceMonitoringEnd(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Device.MonitoringEnd);
        }

        public void LogDeviceConnectionWaiting(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Device.ConnectionWaiting);
        }
        #endregion

        #region ��ġ ���� �α� �޼���
        public void LogDeviceStart(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Control.Start);
        }

        public void LogDeviceStop(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Control.Stop);
        }

        public void LogDeviceStopReset(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Control.StopReset);
        }

        public void LogDeviceReset(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Control.Reset);
        }

        public void LogHeatOn(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Control.HeatOn);
        }

        public void LogHeatOff(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Control.HeatOff);
        }

        public void LogManualMode(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Control.ManualMode);
        }
        public void LogStandByMode(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, "��� ���� ��ȯ");
        }

        public void LogAutoMode(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Control.AutoMode);
        }
        #endregion

        #region �׽�Ʈ �α� �޼���
        public void LogLPTestStart(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Test.LPTestStart);
        }

        public void LogLPTestEnd(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Test.LPTestEnd);
        }

        public void LogRandomTest(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Test.RandomTest);
        }
        #endregion

        #region ���� ���� �α� �޼���
        public void LogStatusChange(string statusCardId, string propertyName, string value)
        {
            var displayId = DeviceLogMessages.GetDisplayIdFromStatusCard(statusCardId);
            var message = DeviceLogMessages.Status.CreateStatusChangeMessage(propertyName, value);
            AddLog(displayId, message);
        }
        #endregion

        #region �˻� ���� �α� �޼���
        public void LogRandomInspection(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            var random = new Random();
            var randomAction = DeviceLogMessages.Inspection.RandomInspectionActions[
                random.Next(DeviceLogMessages.Inspection.RandomInspectionActions.Length)];
            AddLog(displayId, randomAction);
        }
        #endregion

        


        /// <summary>
        /// ��� �α� ����
        /// </summary>
        public void ClearLogs()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _logEntries.Clear();
            });
        }

        /// <summary>
        /// Ư�� ��ġ�� �α׸� ��ȸ
        /// </summary>
        /// <param name="deviceId">��ġ ID</param>
        /// <returns>�ش� ��ġ�� �α� ���</returns>
        public ObservableCollection<DeviceLogEntry> GetLogsByDeviceId(string deviceId)
        {
            var filteredLogs = new ObservableCollection<DeviceLogEntry>(
                _logEntries.Where(log => log.Id == deviceId).ToList());
            return filteredLogs;
        }
    }
}