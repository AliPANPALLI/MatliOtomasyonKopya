namespace EnerjiIzleme.Domain.Models.ReportModels
{
    public class EnerjiKumesTesisIzlemeModel
    {
        public double? Today { get; set; }       // Toplam Tüketim
        public double? Total10 { get; set; }     // 10 Günlük Toplam
        public List<double>? TrendTotal { get; set; }
    }

    public class EnergyWidgetDto
    {
        public decimal today { get; set; }        // kWh
        public decimal todayPer { get; set; }     // kW (ortalama)
        public decimal total10 { get; set; }      // kWh
        public decimal total10Per { get; set; }   // kW (ortalama)
        public List<decimal> trendTotal { get; set; } = new(); // 10 gün kWh
        public List<decimal> trendPer { get; set; } = new();   // 10 gün kW
    }

    public class EnergyFarmPacket
    {
        public string id { get; set; } = "";      // "0".."4"
        public string name { get; set; } = "";
        public EnergyWidgetDto energy { get; set; } = new();
    }

    public class EnergyFarmConfig
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        //public int NodeId { get; set; } // cihazId
        public List<int> NodeIds { get; set; } // cihazIds
    }
}