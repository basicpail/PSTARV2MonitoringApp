using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSTARV2MonitoringApp.Models
{
    public class RawDataTestModel
    {
        public string STBY_Start { get; set; }
        public string RunLamp { get; set; }
        public string Overload { get; set; }
        public string ModeStatus { get; set; }
        public string RUN_req { get; set; }
        public string ResetButton { get; set; }
        public string StandByLamp { get; set; }
        public string TXLowpress { get; set; }
    }
}
