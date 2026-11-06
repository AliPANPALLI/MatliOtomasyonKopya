namespace SharedApp.Application.ViewModels.RequestModel
{
    public sealed class KameraAlarmResult
    {
        public int AlarmLogId { get; init; }

        public bool AlarmIslendi { get; init; }

        public bool FotografGeldi { get; init; }

        public bool FotografKaydedildi { get; init; }

        public string? FotografDosyaYolu { get; init; }

        public string? Uyari { get; init; }
    }
}
