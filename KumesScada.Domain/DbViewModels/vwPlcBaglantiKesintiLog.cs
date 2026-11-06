namespace KumesScada.Domain.DbViewModels
{
    public class vwPlcBaglantiKesintiLog
    {
        public DateTime? Tarih{ get; set; }
        public int KumesId { get; set; }
        public int? KopmaSayisi{ get; set; }
        public long? ToplamKesintiSaniye{ get; set; }
        public decimal? ToplamKesintiDakika{ get; set; }
        public decimal? OrtalamaKesintiSaniye{ get; set; }
        public long? EnUzunKesintiSaniye{ get; set; }
    }
}