namespace SharedApp.Application.ViewModels
{
    public class AlarmLogViewModel
    {
        public int Id { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? Mesaj { get; set; }
        public int? AlarmId { get; set; }
        public byte? AlarmSeviye { get; set; }
        public bool? Aktif { get; set; }
        public DateTime? PasifZamani { get; set; }
        public bool? Silindi { get; set; }
        public DateTime? GuncellemeZamani { get; set; }
    }
}