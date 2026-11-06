namespace PoultryHouseScada.Domain.Models
{
    public class KonveyorLog
    {
        /// <summary>
        /// Yumurta toplama konveyoru icin bu tablo olusturuldu.
        /// </summary>
        public int Id { get; set; }
        public int? KonveyorId { get; set; }
        //public string? KonveyorName { get; set; }
        //public int LokasyonId { get; set; }
        public byte? Durum { get; set; }
        public string? Aciklama { get; set; }
        public DateTime? KayitZamani { get; set; }
    }
}