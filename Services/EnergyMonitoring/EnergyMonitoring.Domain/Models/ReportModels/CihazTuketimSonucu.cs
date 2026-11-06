namespace EnergyMonitoring.Domain.Models.ReportModels
{
    //public class CihazTuketimSonucu
    //{
    //    public int CihazId { get; set; }
    //    public decimal? BaslangicDegeri { get; set; }
    //    public DateTime? BaslangicZamani { get; set; }
    //    public decimal? BitisDegeri { get; set; }
    //    public DateTime? BitisZamani { get; set; }
    //    public decimal? Tuketim { get; set; }
    //    public string? Not { get; set; } // veri bulunamadıysa açıklama
    //}

    public class ToplamTuketimSonucu
    {
        public List<CihazTuketimSonucu> CihazSonuclari { get; set; } = new List<CihazTuketimSonucu>();


        /*
         * GES cihazlarının Export değerleri toplamı
         */
        public decimal GesUretim { get; set; }


        /*
         * GES üretim × 0.975
         */
        public decimal DuzenlenmisGesUretim { get; set; }


        /*
         * GES olmayanların Import sayaç farkları toplamı
         */
        public decimal NormalImport { get; set; }


        /*
         * GES olmayanların Export sayaç farkları toplamı
         */
        public decimal NormalExport { get; set; }


        /*
         * Nihai sonuç
         */
        public decimal ToplamTuketim { get; set; }
    }

    public class CihazTuketimSonucu
    {
        public int CihazId { get; set; }
        public string? CihazName { get; set; }

        public bool GesMi { get; set; }


        public DateTime? BaslangicZamani { get; set; }

        public DateTime? BitisZamani { get; set; }


        /*
         * NORMAL CİHAZ IMPORT
         */
        public decimal? BaslangicImportDegeri { get; set; }

        public decimal? BitisImportDegeri { get; set; }

        public decimal? ImportTuketim { get; set; }


        /*
         * NORMAL CİHAZ EXPORT
         */
        public decimal? BaslangicExportDegeri { get; set; }

        public decimal? BitisExportDegeri { get; set; }

        public decimal? ExportTuketim { get; set; }


        /*
         * GES CİHAZI
         *
         * Seçilen tarih aralığındaki Export değerleri
         * doğrudan toplanır.
         */
        public decimal? GesExportToplami { get; set; }


        public string? Not { get; set; }
    }
}
