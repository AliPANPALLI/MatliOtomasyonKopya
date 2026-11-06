using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnerjiIzleme.Domain.Models.ReportModels
{
    public class HaftalikSayacTuketimModel
    {
        public DateTime HaftaBaslangic { get; set; }   // Cmt 17:00
        public DateTime HaftaBitis { get; set; }        // +7 gün Cmt 17:00
        public decimal ToplamTuketim_kWh { get; set; }
        public List<HaftalikCihazTuketimDetayModel> CihazDetaylar { get; set; }
    }
}
