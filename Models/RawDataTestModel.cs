using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSTARV2MonitoringApp.Models
{
    public class RawDataTestModel
    {
        public string STBY_Start { get; set; } = "OFF";
        public string RunLamp { get; set; } = "OFF";
        public string Overload { get; set; } = "OFF";
        public string ModeStatus { get; set; } = "OFF";
        public string RUN_req { get; set; } = "OFF";
        public string ResetButton { get; set; } = "OFF";
        public string StandByLamp { get; set; } = "OFF";
        public string TXLowpress { get; set; } = "OFF";
        
        // 추가 정보
        public string CanId { get; set; } = "N/A";
        public string Timestamp { get; set; } = DateTime.Now.ToString("HH:mm:ss.fff");
    }
}
