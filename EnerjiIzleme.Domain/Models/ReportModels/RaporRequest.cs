namespace EnerjiIzleme.Domain.Models.ReportModels
{
    public class Analizor
    {
        public string CihazName { get; set; } = "";
        public double TuketimKwh { get; set; }
        public double ToplamTuketimdekiPay { get; set; }
    }

    public class RaporRequest
    {
        public List<Analizor> Analizorler { get; set; } = [];
        public string FromDate { get; set; } = "";
        public string ToDate { get; set; } = "";
    }
}