namespace EnergyMonitoring.Domain.Models.ReportModels
{
    public class TrafoRaporModel
    {
        public string TrafoName { get; set; }
        public int TrafoId { get; set; }
        public decimal SayacDegeri_kWh { get; set; }        // trafonun kendi Import farkı
        public decimal AnalizorlerToplami_kWh { get; set; } // child'ların toplamı
        public decimal SayacAnalizorFarki_kWh { get; set; } // sayaç - analizör toplamı
        public decimal ToplamTuketimdekiPay { get; set; }   // % (tüm trafoların toplamına göre)
        public List<AnalizorSatirModel> Analizorler { get; set; } = new();
    }

    public class AnalizorSatirModel
    {
        public int CihazId { get; set; }
        public string CihazName { get; set; }
        public string MakineName { get; set; }
        public string BolumName { get; set; }
        public decimal Tuketim_kWh { get; set; }
        public decimal OncekiDonemTuketim_kWh { get; set; }
        public decimal ToplamTuketimdekiPay { get; set; }   // % (tüm analizörlerin toplamına göre)
        public decimal OncekiHaftayaGoreDegisim_kWh { get; set; }
        public string BagliTrafoName { get; set; }
    }

    public class EnerjiHaftalikRaporModel
    {
        public DateTime BaslangicZamani { get; set; }
        public DateTime BitisZamani { get; set; }
        public decimal GenelToplamTuketim_kWh { get; set; }
        public List<TrafoRaporModel> Trafolar { get; set; } = new();
        public List<AnalizorSatirModel> TumAnalizorler { get; set; } = new(); // düz tablo için
    }
}
