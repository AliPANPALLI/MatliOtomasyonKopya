namespace HegCoolingScada.Domain.Models.ReportModels
{
    // kompresor_okuma_saatlik/gunluk/aylik view'lerinden bir periyot (bucket) satırı,
    // kompresör tanım bilgisiyle (tag/ad) birleştirilmiş hali.
    public class KompresorRaporSatiri
    {
        public short KompresorId { get; set; }
        public string? TagNo { get; set; }
        public string? Ad { get; set; }

        public DateTime Bucket { get; set; }

        public decimal? OrtKapasite { get; set; }
        public decimal? MinKapasite { get; set; }
        public decimal? MaksKapasite { get; set; }

        // Periyot başındaki ve sonundaki kümülatif ham (PLC sayaç, offset düzeltmesi
        // uygulanmamış) çalışma saati değeri.
        public decimal? CalismaSaatiBaslangicHam { get; set; }
        public decimal? CalismaSaatiBitisHam { get; set; }

        // Bucket başı/sonu net çalışma saati farkı - bu periyotta çalıştığı saat.
        public decimal? CalismaSaatiFarki { get; set; }

        public long? OrnekSayisi { get; set; }
    }
}
