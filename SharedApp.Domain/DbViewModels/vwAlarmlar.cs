namespace SharedApp.Domain.DbViewModels
{
    public class vwAlarmlar
    {
        public int Id { get; set; }
        public string? AlarmAd { get; set; }
        public string? AlarmMetni { get; set; }
        public string? Aciklama { get; set; }
        public string? SesliMesajMetni { get; set; }
        public bool? Silindi { get; set; }
        public int? KonveyorId { get; set; }
        public int? KumesId { get; set; }
        public int? LokasyonId { get; set; }
        public int? CihazId { get; set; }
        public string? KumesName { get; set; }
        public string? KonveyorName { get; set; }
        public string? CihazName { get; set; }
        public string? MakineName { get; set; }
        public string? BolumName { get; set; }
        public string? LokasyonName { get; set; }
        public string? SirketName { get; set; }
        public string? Yer { get; set; }
        public string? LokasyonName2 { get; set; }
        public string? LokasyonName3 { get; set; }
        public string? TipAdi { get; set; }
        public string? Tip { get; set; }
        public bool? AlarmScadaEkrandaGosterilsinMi { get; set; }
        public string? AlarmEslemeSozcugu { get; set; }
        public int? FizikselLokasyonId { get; set; }

    }
}