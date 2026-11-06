namespace SharedApp.Application.ViewModels.RequestModel
{
    public class KameraAlarmRequest
    {
        public string? KameraNo { get; set; }
        public string? AlarmTip { get; set; }
        public DateTime? KayitZamani { get; set; }
        public string? AnlikFotografBase64 { get; set; }
        public string? AnlikFotografContentType { get; set; }

    }
}
