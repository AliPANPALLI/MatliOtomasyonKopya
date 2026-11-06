namespace EnergyMonitoring.Domain.DbViewModels
{
    public class vwCihaz
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? MakineId { get; set; }
        public int? BolumId { get; set; }
        public int? LokasyonId { get; set; }
        public bool? Silindi { get; set; }
        public bool? Aktif { get; set; }
        public bool? MakineSilindi { get; set; }
        public bool? BolumSilindi { get; set; }
        public string? MakineName { get; set; }
        public string? BolumName { get; set; }
        public int ParentCihazId { get; set; }
    }
}
