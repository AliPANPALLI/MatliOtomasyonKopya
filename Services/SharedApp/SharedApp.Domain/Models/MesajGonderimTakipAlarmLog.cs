namespace SharedApp.Domain.Models
{
    public class MesajGonderimTakipAlarmLog
    {
        public long Id { get; set; }
        public int? MesajGonderimTakipId { get; set; }
        public int TakipId { get; set; }
        public int AlarmLogId { get; set; }
        public string AlarmKaynakTuru { get; set; }
        public string TakipTipi { get; set; }
    }
}
