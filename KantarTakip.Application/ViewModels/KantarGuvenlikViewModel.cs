namespace KantarTakip.Application.ViewModels
{
    // Teslim fişi - Genel Kontroller
    // Zorunlu: ZiyaretciKayitId, Plaka, Tarih. Diğerleri boş bırakılabilir.
    public class KantarGuvenlikViewModel
    {
        public int Id { get; set; }
        public string? ZiyaretciKayitId { get; set; }
        public string? Plaka { get; set; }
        public DateTime? Tarih { get; set; }
        public string? UrunAdi { get; set; }
        public int? FirmaId { get; set; }
        public string? IrsaliyeNo { get; set; }
        public string? SonUrun { get; set; }
    }
}
