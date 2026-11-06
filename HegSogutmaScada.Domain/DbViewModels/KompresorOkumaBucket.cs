namespace HegSogutmaScada.Domain.DbViewModels
{
    // kompresor_okuma_saatlik / _gunluk / _aylik view'lerinin üçü de aynı şekle
    // sahip (TimescaleDB continuous aggregate). Her periyot ayrı CLR tipiyle
    // eşleniyor ki EF Core her birini kendi view'ine (HasNoKey + ToView) bağlayabilsin.
    public abstract class KompresorOkumaBucketBase
    {
        public short KompresorId { get; set; }
        public DateTime Bucket { get; set; }
        public decimal? OrtKapasite { get; set; }
        public decimal? MinKapasite { get; set; }
        public decimal? MaksKapasite { get; set; }
        public decimal? SaatBaslangicHam { get; set; }
        public decimal? SaatBitisHam { get; set; }
        public decimal? SaatBaslangicNet { get; set; }
        public decimal? SaatBitisNet { get; set; }
        public decimal? SaatFark { get; set; }
        public long? OrnekSayisi { get; set; }
    }

    public class KompresorOkumaSaatlik : KompresorOkumaBucketBase
    {
    }

    public class KompresorOkumaGunluk : KompresorOkumaBucketBase
    {
    }

    public class KompresorOkumaAylik : KompresorOkumaBucketBase
    {
    }
}
