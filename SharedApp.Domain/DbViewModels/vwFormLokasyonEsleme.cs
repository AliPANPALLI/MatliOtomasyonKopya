namespace SharedApp.Domain.DbViewModels
{
    public class vwFormLokasyonEsleme
    {
        public int Id { get; set; }
        public int FormId { get; set; }
        public int LokasyonId { get; set; }
        public bool? Silindi { get; set; }
        public string? FormName { get; set; }
        public bool? FormSilindi { get; set; }
        public string? ModulName { get; set; }
        public bool? ModulSilindi { get; set; }
        public string? LokasyonNo { get; set; }
        public string? LokasyonName { get; set; }
        public int SirketId { get; set; }
        public bool? LokasyonSilindi { get; set; }
        public string SirketNo { get; set; }
        public string SirketName { get; set; }
        public bool? SirketSilindi { get; set; }
        public int? CizimId { get; set; }
        public string? CizimName { get; set; }

    }
}