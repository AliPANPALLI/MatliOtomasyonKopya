namespace EnergyMonitoring.Domain.Models.ReportModels
{
    //public class EnerjiMaliyetSonuc
    //{
    //    public DateTime Tarih { get; set; }
    //    public int Saat { get; set; }
    //    public decimal TuketimWh { get; set; }
    //    public decimal Ptf { get; set; }
    //    public decimal Maliyet { get; set; }
    //}

    public class EnerjiMaliyetSonuc
    {
        public int CihazId { get; set; }
        public DateTime Baslangic { get; set; }
        public DateTime Bitis { get; set; }
        public double Dakika { get; set; }

        public decimal ImportFarkWh { get; set; }
        public decimal ExportFarkWh { get; set; }
        public decimal NetTuketimWh { get; set; }
        public decimal TuketimKWh { get; set; }

        public decimal? PtfTlMWh { get; set; }
        public decimal? YekdemTlMWh { get; set; }
        public decimal? DagitimTlMWh { get; set; }
        public decimal? AnlasmaOraniYuzde { get; set; }

        public decimal? BirimFiyatTlKWh { get; set; } // ((PTF+YEKDEM)*(1+oran) + Dağıtım) / 1000
        public decimal? MaliyetTl { get; set; }
    }
    //Yeni hesaplama Modeller
    public enum ZamanDilimi { Gunduz, Puant, Gece }

    // ── Tarife-bazlı sonuç modelleri ─────────────────────────────────────────────

    public class ZamanDilimiDetay
    {
        public decimal? TuketimKwh { get; set; }
        public decimal? AktifEnerjiBedeli { get; set; }
        public decimal? DagitimBedeli { get; set; }
        /// <summary>Aktif + Dağıtım — güç bedeli bu kalemde YOK</summary>
        public decimal? ToplamTutar { get; set; }
    }

    public class GenelMaliyetSonuc
    {
        public decimal? ToplamTuketimKwh { get; set; }
        public decimal? EnerjiBedeli { get; set; }
        public decimal? DagitimBedeli { get; set; }
        public decimal? ToplamTutar { get; set; }
        public decimal? BirimMaliyet { get; set; }
    }

    public class TekZamanliSonuc
    {
        public decimal? ToplamTuketimKwh { get; set; }
        public decimal? AktifEnerjiBedeli { get; set; }
        public decimal? DagitimBedeli { get; set; }
        public decimal? ToplamTutar { get; set; }   // Aktif + Dağıtım + Güç
        public decimal? ElektrikTuketimVergisi { get; set; }   // %5  × Aktif
        public decimal? Kdv { get; set; }   // %10 × (Toplam + ETV)
        public decimal? FaturaTutari { get; set; }
        public decimal? BirimMaliyet { get; set; }
    }

    public class UcZamanliSonuc
    {
        public ZamanDilimiDetay? Gunduz { get; set; }
        public ZamanDilimiDetay? Puant { get; set; }
        public ZamanDilimiDetay? Gece { get; set; }
        public decimal? ToplamTuketimKwh { get; set; }
        public decimal? ToplamAktifEnerji { get; set; }
        public decimal? ToplamDagitimBedeli { get; set; }
        public decimal? ToplamTutar { get; set; }
        public decimal? ElektrikTuketimVergisi { get; set; }
        public decimal? Kdv { get; set; }
        public decimal? FaturaTutari { get; set; }
        public decimal? BirimMaliyet { get; set; }
    }
}