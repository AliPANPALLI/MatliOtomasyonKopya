namespace NesneIzleme.Domain.DbViewModels
{
    public class vwLokasyon
    {
        public int Id { get; set; }
        public string? LokasyonNo { get; set; }
        public string? Name { get; set; }
        public int SirketId { get; set; }
        public bool? Silindi { get; set; }
    }
}
