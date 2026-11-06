namespace EnergyMonitoring.Domain.DbViewModels
{
    public class vwCihaz_Onay
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int MakineId { get; set; }
        public int ParentCihazId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public int GatewayId { get; set; }
        public int ModelId { get; set; }
        public int SlaveId { get; set; }
        public bool? LokasyonEnerjiHesabindaKullan { get; set; }
        public bool Aktif { get; set; }
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
        public bool? Onaylandi { get; set; }
        public DateTime? OnaylanmaZamani { get; set; }
        public string? OnaylayanUserId { get; set; }
        public DateTime? EklenmeZamani { get; set; }
        public string? EkleyenUserId { get; set; }
        public string? EkleyenUserName { get; set; }
        public DateTime? GuncellemeZamani { get; set; }
        public string? GuncelleyenUserId { get; set; }
        public string? GuncelleyenUserName { get; set; }
    }
}
