namespace HegSogutmaScada.Domain.Models.ReportModels
{
    public class VanaViewModel
    {
        public short VanaId { get; set; }
        public short? OdaId { get; set; }
        public string? OdaAdi { get; set; }
        public string? OdaKat { get; set; }
        public string? EkipmanKodu { get; set; }
        public string? Aciklama { get; set; }
        public bool? GuncelDurumAcik { get; set; }
        public DateTime? GuncelDurumBaslangici { get; set; }
        public DateTime? GuncellemeZamani { get; set; }
    }
}
