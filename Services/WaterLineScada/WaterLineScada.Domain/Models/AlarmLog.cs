namespace WaterLineScada.Domain.Models
{
    public class AlarmLog
    {
        public int Id { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? Mesaj { get; set; }
        public int? AlarmEnum { get; set; }
        public int? AlarmId { get; set; }//SharedDbContext Alarm tablosundaki Id değerine karşılık gelir
        public byte? AlarmSeviye { get; set; }
        public bool? Aktif { get; set; }
        public DateTime? PasifZamani { get; set; }
        public bool? Silindi { get; set; }
        public int? SuKaynakId { get; set; }
        public int? LokasyonId { get; set; }
        public int? CizimId { get; set; }
    }
}