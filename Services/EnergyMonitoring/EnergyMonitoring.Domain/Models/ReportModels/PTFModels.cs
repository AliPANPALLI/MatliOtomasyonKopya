using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnergyMonitoring.Domain.Models.ReportModels
{
    public class PtfResponse
    {
        public List<PtfItem> Items { get; set; }
    }

    public class PtfItem
    {
        public DateTime Date { get; set; }  // 2025-09-09T00:00:00+03:00
        public string Hour { get; set; }    // "00:00"
        public decimal Price { get; set; }  // TL/MWh
    }

}
