namespace SharedApp.Domain.DbViewModels
{
    public class vwSuHatCizimSirketForYetki
    {
        public int? CizimId { get; set; }
        public string? CizimName { get; set; }
        public int? LokasyonId { get; set; }
        public bool? CizimLokasyonSilindi { get; set; }
        public string? LokasyonName { get; set; }
        public int? SirketId { get; set; }
        public string? SirketName { get; set; }
        public bool? CizimSilindi { get; set; }
        public int? SidebarSira { get; set; }
    }
}
