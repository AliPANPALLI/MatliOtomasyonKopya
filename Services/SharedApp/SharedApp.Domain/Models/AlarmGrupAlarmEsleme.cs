namespace SharedApp.Domain.Models
{
    public class AlarmGrupAlarmEsleme
    {
        public int Id { get; set; }
        public int? AlarmGrupId { get; set; }
        public int? AlarmId { get; set; }
        public bool? Silindi { get; set; }
    }
}
