namespace SharedApp.Application.ViewModels.RequestModel
{
    public sealed class KaydedilenFotografBilgisi
    {
        public required string GoreliDosyaYolu { get; init; }

        public required string TamDosyaYolu { get; init; }

        public required string DosyaAdi { get; init; }

        public required string ContentType { get; init; }

        public required long BoyutByte { get; init; }
    }
}
