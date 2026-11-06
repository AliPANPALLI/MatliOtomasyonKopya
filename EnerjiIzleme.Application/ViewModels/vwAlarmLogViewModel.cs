namespace EnerjiIzleme.Application.ViewModels
{
    public class vwAlarmLogViewModel
    {
        public int Id { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? Mesaj { get; set; }
        public int? AlarmId { get; set; }
        public byte? AlarmSeviye { get; set; }
        public bool? Aktif { get; set; }
        public DateTime? PasifZamani { get; set; }
        public bool? Silindi { get; set; }
        public int? CihazId { get; set; }

        public bool? MakineSilindi { get; set; }
        public bool? BolumSilindi { get; set; }
        public string? MakineName { get; set; }
        public string? BolumName { get; set; }
        public int? MakineId { get; set; }
        public int? BolumId { get; set; }
        public int? LokasyonId { get; set; }
    }
}
