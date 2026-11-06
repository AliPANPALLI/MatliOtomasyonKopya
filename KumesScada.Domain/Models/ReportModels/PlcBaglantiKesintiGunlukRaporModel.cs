namespace KumesScada.Domain.Models.ReportModels
{
    /// <summary>
    /// PLC bağlantı kesinti raporunun bir satırı: bir kümesin belirli bir gündeki kopma özeti.
    /// </summary>
    public class PlcBaglantiKesintiGunlukRaporModel
    {
        public int KumesId { get; set; }
        public string? KumesName { get; set; }
        public DateTime Tarih { get; set; }
        public int KopmaSayisi { get; set; }
        public long ToplamKesintiSaniye { get; set; }
        public decimal ToplamKesintiDakika { get; set; }
        public long EnUzunKesintiSaniye { get; set; }
    }
}
