namespace WaterLineScada.Domain.DbViewModels
{
    public class vwCizimLokasyonEsleme
    {
        public int Id { get; set; }
        public int CizimId { get; set; }
        public int LokasyonId { get; set; }
        public bool? Silindi { get; set; }
        public string? LokasyonName { get; set; }
        public string? CizimName { get; set; }
        public int? SirketId { get; set; }
    }
}