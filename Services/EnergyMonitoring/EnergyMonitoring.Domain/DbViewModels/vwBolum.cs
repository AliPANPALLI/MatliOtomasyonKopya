namespace EnergyMonitoring.Domain.DbViewModels
{
    public class vwBolum
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int LokasyonId { get; set; }
        public bool? Aktif { get; set; }
        public bool? Silindi { get; set; }
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
