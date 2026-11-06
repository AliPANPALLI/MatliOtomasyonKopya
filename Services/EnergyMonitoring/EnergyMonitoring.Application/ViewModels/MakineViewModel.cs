namespace EnergyMonitoring.Application.ViewModels
{
    public class MakineViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int BolumId { get; set; }
        public string? MakineNo { get; set; }
        public string? Aciklama { get; set; }
        public bool? Silindi { get; set; }
        public bool? Aktif { get; set; }
        public DateTime? EklenmeZamani { get; set; }
        public string? EkleyenUserName { get; set; }
        public DateTime? GuncellemeZamani { get; set; }
        public string? GuncelleyenUserName { get; set; }

    }
}
