namespace EnerjiIzleme.Application.ViewModels
{
    public class CihazViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        //Sirket-Lokasyon-Bolum-Makine
        public int? MakineId { get; set; }
        public DateTime? KayitZamani { get; set; }
        //public string? Marka { get; set; }
        //public string? Model { get; set; }
        public int? ModelId { get; set; }
        public bool? Aktif { get; set; }
        public int? SlaveId { get; set; }
        public int? GatewayId { get; set; }
        public bool? Silindi { get; set; }
        public bool? LokasyonEnerjiHesabindaKullan { get; set; }
        public int? ParentCihazId { get; set; }
        public DateTime? EklenmeZamani { get; set; }
        public string? EkleyenUserName { get; set; }
        public DateTime? GuncellemeZamani { get; set; }
        public string? GuncelleyenUserName { get; set; }
    }
}