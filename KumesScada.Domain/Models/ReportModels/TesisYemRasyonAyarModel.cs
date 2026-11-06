namespace KumesScada.Domain.Models.ReportModels
{
    public class TesisYemRasyonAyarModel
    {
        public int LokasyonId { get; set; }
        public List<TesisYemRasyonSatirModel> FeedRows { get; set; }
        public List<TesisKumesRasyonModel> KumesRasyonSelections { get; set; }
    }
    public class TesisYemRasyonSatirModel
    {
        public string YemAdi { get; set; }
        public string Rasyon { get; set; }
        public decimal BirimFiyat { get; set; }
    }

    public class TesisKumesRasyonModel
    {
        public int KumesNo { get; set; }
        public string Rasyon { get; set; }
    }
}