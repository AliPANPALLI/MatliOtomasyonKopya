namespace SharedApp.Application.ViewModels
{
    public class MailGonderimTakipViewModel
    {
        public int Id { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? MailBasligi { get; set; }
        public string? MailIcerik { get; set; }
        public string? EkYolu { get; set; }
        public bool? EkYoluKlasorMu { get; set; }
        public bool? GonderimBasarili { get; set; }
        public string? HataMesaji { get; set; }
        public string? GonderilecekTo { get; set; }
        public string? GonderilecekCc { get; set; }
        public DateTime? SonDenemeZamani { get; set; }
        public short? KalanDenemeSayisi { get; set; }
        public string? UserId { get; set; }
        public int? KumesAlarmLogId { get; set; }
        public int? KonveyorAlarmLogId { get; set; }
        public int? SuHatAlarmLogId { get; set; }
        public string? KumesAlarmLogIds { get; set; }
        public string? KonveyorAlarmLogIds { get; set; }
        public string? SuHatAlarmLogIds { get; set; }
        public int? SeviyeNo { get; set; }
        public bool? IsBodyHtml { get; set; }
    }
}
