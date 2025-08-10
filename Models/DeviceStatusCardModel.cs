using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSTARV2MonitoringApp.Models
{
    public class DeviceStatusCardModel
    {
        public string DeviceId { get; set; }
        public string CommStatus { get; set; }
        public string RunStatus { get; set; }
        public string RunMode { get; set; }
        public string StandByStatus { get; set; }
        public string OverloadStatus { get; set; }
        public string LowPressureStatus { get; set; }
    }
}
