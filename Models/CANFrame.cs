using System;

namespace PSTARV2MonitoringApp.Models
{
    /// <summary>
    /// CAN 프레임 데이터 구조
    /// </summary>
    public class CANFrame
    {
        public uint Id { get; set; }
        public byte[] Data { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsExtended { get; set; } = false;
        public bool IsRemote { get; set; } = false;

        /// <summary>
        /// 데이터를 16진수 문자열로 변환
        /// </summary>
        public string DataAsHex => Data != null ? BitConverter.ToString(Data).Replace("-", " ") : string.Empty;

        /// <summary>
        /// 특정 바이트 위치의 값 가져오기
        /// </summary>
        public byte GetByte(int index)
        {
            return Data != null && index < Data.Length ? Data[index] : (byte)0;
        }

        /// <summary>
        /// 특정 비트 위치의 값 가져오기
        /// </summary>
        public bool GetBit(int byteIndex, int bitIndex)
        {
            if (Data == null || byteIndex >= Data.Length) return false;
            return (Data[byteIndex] & (1 << bitIndex)) != 0;
        }
    }

    /// <summary>
    /// 장치별 CAN 데이터 해석 결과
    /// </summary>
    public class DeviceCANData
    {
        public string DeviceId { get; set; }
        public DateTime Timestamp { get; set; }
        
        // 장치 상태
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
        
        // 통신 상태 정보
        public string CommStatus { get; set; } = "Normal";
        public string RunStatus { get; set; } = "Stopped";
        public string RunMode { get; set; } = "Manual";
        public string StandByStatus { get; set; } = "Standby";
        public string OverloadStatus { get; set; } = "Normal";
        public string LowPressureStatus { get; set; } = "Normal";
    }
}