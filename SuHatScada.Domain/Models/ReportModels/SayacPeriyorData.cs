using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuHatScada.Domain.Models.ReportModels
{
    public class SayacPeriyotData2
    {
        public int SayacId { get; set; }

        public DateTime Periyot { get; set; }

        public decimal Toplam { get; set; }
    }

    public class TankPeriyotData2
    {
        public int SuKaynakId { get; set; }

        public DateTime Periyot { get; set; }

        public decimal MinHacim { get; set; }

        public decimal OrtalamaHacim { get; set; }

        public decimal MaxHacim { get; set; }
    }
}
