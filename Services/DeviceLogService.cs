using PSTARV2MonitoringApp.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PSTARV2MonitoringApp.Services
{
    /// <summary>
    /// 장치 로그를 관리하는 서비스
    /// </summary>
    public class DeviceLogService
    {
        private static DeviceLogService _instance;
        public static DeviceLogService Instance => _instance ??= new DeviceLogService();

        private readonly ObservableCollection<DeviceLogEntry> _logEntries;
        private const int MaxLogEntries = 1000; // 최대 로그 개수

        public ObservableCollection<DeviceLogEntry> LogEntries => _logEntries;

        private DeviceLogService()
        {
            _logEntries = new ObservableCollection<DeviceLogEntry>();
        }

        /// <summary>
        /// 장치 동작 로그 추가
        /// </summary>
        /// <param name="deviceId">장치 ID</param>
        /// <param name="action">동작 내용</param>
        public void AddLog(string deviceId, string action)
        {
            // UI 스레드에서 실행되도록 보장
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var logEntry = new DeviceLogEntry(deviceId, action);

                // 최신 로그를 맨 위에 추가
                _logEntries.Insert(0, logEntry);

                // 최대 개수 초과 시 오래된 로그 제거
                while (_logEntries.Count > MaxLogEntries)
                {
                    _logEntries.RemoveAt(_logEntries.Count - 1);
                }
            });
        }

        #region 시스템 로그 메서드
        public void AddSystemLog(string message)
            => AddLog("시스템", message);

        public void AddSystemErrorLog(string errorMessage)
            => AddLog("시스템", DeviceLogMessages.Format(DeviceLogMessages.System.Error, errorMessage));

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

        #region 장치 로그 메서드
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

        #region 장치 제어 로그 메서드
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
            AddLog(displayId, "대기 모드로 전환");
        }

        public void LogAutoMode(string deviceId)
        {
            var displayId = DeviceLogMessages.GetDisplayId(deviceId);
            AddLog(displayId, DeviceLogMessages.Control.AutoMode);
        }
        #endregion

        #region 테스트 로그 메서드
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

        #region 상태 변경 로그 메서드
        public void LogStatusChange(string statusCardId, string propertyName, string value)
        {
            var displayId = DeviceLogMessages.GetDisplayIdFromStatusCard(statusCardId);
            var message = DeviceLogMessages.Status.CreateStatusChangeMessage(propertyName, value);
            AddLog(displayId, message);
        }
        #endregion

        #region 검사 동작 로그 메서드
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
        /// 모든 로그 삭제
        /// </summary>
        public void ClearLogs()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _logEntries.Clear();
            });
        }

        /// <summary>
        /// 특정 장치의 로그만 조회
        /// </summary>
        /// <param name="deviceId">장치 ID</param>
        /// <returns>해당 장치의 로그 목록</returns>
        public ObservableCollection<DeviceLogEntry> GetLogsByDeviceId(string deviceId)
        {
            var filteredLogs = new ObservableCollection<DeviceLogEntry>(
                _logEntries.Where(log => log.Id == deviceId).ToList());
            return filteredLogs;
        }
    }
}