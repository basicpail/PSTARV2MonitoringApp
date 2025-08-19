using System;

namespace PSTARV2MonitoringApp.Models
{
    /// <summary>
    /// CAN ������ ������ ����
    /// </summary>
    public class CANFrame
    {
        public uint Id { get; set; }
        public byte[] Data { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsExtended { get; set; } = false;
        public bool IsRemote { get; set; } = false;

        /// <summary>
        /// �����͸� 16���� ���ڿ��� ��ȯ
        /// </summary>
        public string DataAsHex => Data != null ? BitConverter.ToString(Data).Replace("-", " ") : string.Empty;

        /// <summary>
        /// Ư�� ����Ʈ ��ġ�� �� ��������
        /// </summary>
        public byte GetByte(int index)
        {
            return Data != null && index < Data.Length ? Data[index] : (byte)0;
        }

        /// <summary>
        /// Ư�� ��Ʈ ��ġ�� �� ��������
        /// </summary>
        public bool GetBit(int byteIndex, int bitIndex)
        {
            if (Data == null || byteIndex >= Data.Length) return false;
            return (Data[byteIndex] & (1 << bitIndex)) != 0;
        }
    }

    /// <summary>
    /// ��ġ�� CAN ������ �ؼ� ���
    /// </summary>
    public class DeviceCANData
    {
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        
        // ��ġ ����
        public bool IsSourceOn { get; set; }
        public bool IsAbnormal { get; set; }
        public bool IsRunning { get; set; }
        public bool IsStopped { get; set; }
        public bool IsHeating { get; set; }
        public bool IsCommFailure { get; set; }
        public bool IsLowPressure { get; set; }
        public bool IsStandby { get; set; }
        public bool IsHeatOn { get; set; }
        public bool IsStandbyMode { get; set; }
        public bool IsManualMode { get; set; }
        
        // Raw Data
        public bool STBY_Start { get; set; }
        public bool RunLamp { get; set; }
        public bool Overload { get; set; }
        public bool ModeStatus { get; set; }
        public bool RUN_req { get; set; }
        public bool ResetButton { get; set; }
        public bool StandByLamp { get; set; }
        public bool TXLowpress { get; set; }
        
        // ��� ���� ����
        public string CommStatus { get; set; } = "Normal";
        public string RunStatus { get; set; } = "Stopped";
        public string RunMode { get; set; } = "Manual";
        public string StandByStatus { get; set; } = "Standby";
        public string OverloadStatus { get; set; } = "Normal";
        public string LowPressureStatus { get; set; } = "Normal";
    }
}