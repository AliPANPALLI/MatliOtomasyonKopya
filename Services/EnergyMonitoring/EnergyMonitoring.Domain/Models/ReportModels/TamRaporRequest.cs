using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EnergyMonitoring.Domain.Models.ReportModels
{
    public class TamRaporRequest
    {
        public List<AnalizorDto> TumAnalizorler { get; set; } = [];
        public List<TrafoDto> Trafolar { get; set; } = [];
        public List<Dictionary<string, JsonElement>> TrafoKarsilastirmaData { get; set; } = [];
        public string FromDate { get; set; } = "";
        public string ToDate { get; set; } = "";
    }

    public class AnalizorDto
    {
        public string CihazName { get; set; } = "";
        public double TuketimKwh { get; set; }
        public double ToplamTuketimdekiPay { get; set; }
        public double OncekiDonemTuketimKwh { get; set; }
        public string BagliTrafoName { get; set; } = "";
        public double OncekiHaftayaGoreDegisimKwh { get; set; }
    }

    public class TrafoDto
    {
        public string TrafoName { get; set; } = "";
        public double? SayacDegeriKwh { get; set; }
        public double? SayacAnalizorFarkiKwh { get; set; }
        public List<AnalizorDto> Analizorler { get; set; } = [];
    }
}
