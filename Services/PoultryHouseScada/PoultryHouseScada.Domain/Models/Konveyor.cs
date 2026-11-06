namespace PoultryHouseScada.Domain.Models
{
    public class Konveyor
    {
        /// <summary>
        /// Yumurta toplama konveyoru icin bu tablo olusturuldu.
        /// </summary>
        public int Id { get; set; }
        public string? Name { get; set; }
        public byte? Tip { get; set; }
        public int? ParentId { get; set; }
        public int LokasyonId { get; set; }
        public int? EtiketIdForKonveyorRapor { get; set; }
        public bool? Silindi { get; set; }
    }
}