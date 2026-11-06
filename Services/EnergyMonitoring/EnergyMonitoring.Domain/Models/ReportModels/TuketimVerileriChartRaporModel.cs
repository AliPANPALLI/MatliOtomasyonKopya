using MongoDB.Bson;

namespace EnergyMonitoring.Domain.Models.ReportModels
{
    public class TuketimVerileriChartRaporModel
    {
        //public string CihazAd { get; set; }
        //public Dictionary<string, Decimal128> ZamansalVeri { get; set; } = new();
        public string CihazAd { get; set; }
        public int Yil { get; set; }
        public int Ay { get; set; }
        public decimal Tuketim { get; set; }
    }
}
