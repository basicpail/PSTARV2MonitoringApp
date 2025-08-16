using System.Collections.Generic;

namespace PSTARV2MonitoringApp.Models
{
    /// <summary>
    /// ��ġ �α� �޽��� ī�װ�
    /// </summary>
    public enum LogCategory
    {
        System,     // �ý��� ����
        Device,     // ��ġ �⺻ ����
        Status,     // ���� ����
        Error,      // ����
        Test        // �׽�Ʈ
    }

    /// <summary>
    /// ��ġ �α� �޽����� �߾ӿ��� �����ϴ� Ŭ����
    /// </summary>
    public static class DeviceLogMessages
    {
        #region �ý��� �޽���
        public static class System
        {
            public const string MonitoringStart = "����͸� �ý��� ����";
            public const string DashboardAccess = "��ú��� ������ ����";
            public const string TestPageAccess = "�׽�Ʈ ������ ����";
            public const string LogCleared = "�αװ� �ʱ�ȭ��";
            public const string AllDevicesRemoved = "��� ��ġ ���ŵ�";
            public const string Error = "���� �߻�: {0}";
        }
        #endregion

        #region ��ġ �⺻ ���� �޽���
        public static class Device
        {
            public const string Added = "��ġ �߰��� (��: {0})";
            public const string Removed = "��ġ ���ŵ�";
            public const string ModelSet = "��ġ �� ����: {0}";
            public const string MonitoringStart = "��ġ ����͸� ����";
            public const string MonitoringEnd = "��ġ ����͸� ����";
            public const string ConnectionWaiting = "��ġ ���� ��� ��";
            public const string CardNotFound = "ī�带 ã�� �� ���� - ��ġ �߰� ����";
            public const string ReregisteredAfterRemoval = "���� ��ġ ���� �� ����";
        }
        #endregion

        #region ��ġ ���� �޽���
        public static class Control
        {
            public const string Start = "��ġ ����";
            public const string Stop = "��ġ ����";
            public const string StopReset = "��ġ ����/����";
            public const string Reset = "��ġ ��ü �ʱ�ȭ";
            public const string HeatOn = "���� ��� Ȱ��ȭ";
            public const string HeatOff = "���� ��� ��Ȱ��ȭ";
            public const string ManualMode = "���� ���� ��ȯ";
            public const string AutoMode = "�ڵ� ���� ��ȯ";
        }
        #endregion

        #region �׽�Ʈ �޽���
        public static class Test
        {
            public const string LPTestStart = "LP �׽�Ʈ ����";
            public const string LPTestEnd = "LP �׽�Ʈ ����";
            public const string RandomTest = "���� �׽�Ʈ ����";
        }
        #endregion

        #region ���� ���� �޽���
        public static class Status
        {
            // DeviceStatusCardModel �Ӽ��� �ѱ� ǥ�ø�
            public static readonly Dictionary<string, string> PropertyDisplayNames = new()
            {
                { nameof(DeviceStatusCardModel.CommStatus), "��� ����" },
                { nameof(DeviceStatusCardModel.RunStatus), "���� ����" },
                { nameof(DeviceStatusCardModel.RunMode), "���� ���" },
                { nameof(DeviceStatusCardModel.StandByStatus), "��� ����" },
                { nameof(DeviceStatusCardModel.OverloadStatus), "������ ����" },
                { nameof(DeviceStatusCardModel.LowPressureStatus), "���� ����" }
            };

            /// <summary>
            /// ���� ���� �޽��� ����
            /// </summary>
            /// <param name="propertyName">�Ӽ���</param>
            /// <param name="value">�� ��</param>
            /// <returns>���˵� ���� ���� �޽���</returns>
            public static string CreateStatusChangeMessage(string propertyName, string value)
            {
                var displayName = PropertyDisplayNames.GetValueOrDefault(propertyName, propertyName);
                return $"{displayName}: {value}";
            }
        }
        #endregion

        #region �Ϲ����� �˻� ���� �޽���
        public static class Inspection
        {
            public const string TemperatureCheck = "�µ� üũ";
            public const string PressureMeasurement = "�з� ����";
            public const string StatusCheck = "���� Ȯ��";
            public const string SensorInspection = "���� ����";
            public const string DataCollection = "������ ����";
            public const string AlarmCheck = "�˶� üũ";
            public const string SystemDiagnosis = "�ý��� ����";
            public const string ConnectivityTest = "���Ἲ �׽�Ʈ";
            public const string PerformanceTest = "���� �׽�Ʈ";
            public const string SafetyCheck = "���� ����";

            /// <summary>
            /// ���� �˻� ���� �޽��� �迭
            /// </summary>
            public static readonly string[] RandomInspectionActions = 
            {
                TemperatureCheck,
                PressureMeasurement,
                StatusCheck,
                SensorInspection,
                DataCollection,
                AlarmCheck,
                SystemDiagnosis,
                ConnectivityTest,
                PerformanceTest,
                SafetyCheck
            };
        }
        #endregion

        #region ���� �޼���
        /// <summary>
        /// �Ű������� �ִ� �޽����� ������
        /// </summary>
        /// <param name="message">���� ���ڿ�</param>
        /// <param name="args">�Ű�����</param>
        /// <returns>���˵� �޽���</returns>
        public static string Format(string message, params object[] args)
        {
            return string.Format(message, args);
        }

        /// <summary>
        /// ��ġ ID�� ���� ǥ�� ID ��������
        /// </summary>
        /// <param name="deviceId">��ġ ID</param>
        /// <returns>ǥ�ÿ� ID</returns>
        public static string GetDisplayId(string deviceId)
        {
            var deviceInfo = DeviceInfo.GetDeviceIdById(deviceId);
            return deviceInfo?.DisplayText ?? deviceId;
        }

        /// <summary>
        /// StatusCard ID�� ���� ǥ�� ID ��������
        /// </summary>
        /// <param name="statusCardId">StatusCard ID</param>
        /// <returns>ǥ�ÿ� ID</returns>
        public static string GetDisplayIdFromStatusCard(string statusCardId)
        {
            var deviceInfo = DeviceInfo.GetDeviceIdByStatusCardId(statusCardId);
            return deviceInfo?.DisplayText ?? statusCardId;
        }
        #endregion
    }
}