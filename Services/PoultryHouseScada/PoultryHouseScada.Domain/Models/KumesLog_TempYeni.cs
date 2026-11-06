namespace PoultryHouseScada.Domain.Models
{
    public class KumesLog_TempYeni
    {
        public long Id { get; set; }
        public int? KumesId { get; set; }
        public DateTime? KayitZamani { get; set; }
        public int? DonemId { get; set; }

        //iklimlendirme ile ilgili
        public decimal? SicaklikKumes { get; set; }
        public decimal? SicaklikDis { get; set; }
        public decimal? Sicaklik1 { get; set; }
        public decimal? Sicaklik2 { get; set; }
        public decimal? Sicaklik3 { get; set; }
        public decimal? Sicaklik4 { get; set; }
        public decimal? Sicaklik5 { get; set; }
        public decimal? Sicaklik6 { get; set; }
        public decimal? Sicaklik7 { get; set; }
        public decimal? Sicaklik8 { get; set; }
        public decimal? NemKumes { get; set; }
        public decimal? NemDis { get; set; }
        public decimal? Nem1 { get; set; }
        public decimal? Nem2 { get; set; }
        public decimal? Nem3 { get; set; }
        public decimal? Nem4 { get; set; }
        public decimal? Nem5 { get; set; }
        public decimal? Nem6 { get; set; }
        public decimal? Nem7 { get; set; }
        public decimal? Nem8 { get; set; }
        public decimal? Silo1 { get; set; }
        public decimal? Silo2 { get; set; }
        public decimal? Basinc1 { get; set; }
        public decimal? Co2_1 { get; set; }
        public decimal? SuTuketimBugun { get; set; }
        public decimal? YemTuketimBugun { get; set; }
        public decimal? PedSuTuketimBugun { get; set; }
        public int? OlenSayisiBugun { get; set; }

        //yumurta uretimi ile ilgili
        public int? YumurtaAdet { get; set; }
        public int? SAdet { get; set; }
        public int? MAdet { get; set; }
        public int? LAdet { get; set; }
        public int? XLAdet { get; set; }
        public int? AsiriKucukAdet { get; set; }
        public int? KirikYumurtaAdet { get; set; }
        public int? HicKirliYumurtaAdet { get; set; }
        public int? AzKirliYumurtaAdet { get; set; }
        public int? OrtaKirliYumurtaAdet { get; set; }
        public int? CokKirliYumurtaAdet { get; set; }
        public int? HicKanliYumurtaAdet { get; set; }
        public int? AzKanliYumurtaAdet { get; set; }
        public int? OrtaKanliYumurtaAdet { get; set; }
        public int? CokKanliYumurtaAdet { get; set; }
        public int? HicDiskiliYumurtaAdet { get; set; }
        public int? AzDiskiliYumurtaAdet { get; set; }
        public int? OrtaDiskiliYumurtaAdet { get; set; }
        public int? CokDiskiliYumurtaAdet { get; set; }
        //public int? SYumurtaAdet { get; set; }
        //public int? MYumurtaAdet { get; set; }
        //public int? LYumurtaAdet { get; set; }
        //public int? XLYumurtaAdet { get; set; }
        //public int? CatlakYumurtaAdet { get; set; }
        //public int? KirikYumurtaAdet { get; set; }

        //TODO: tavuk bilgisi ile ilgili ozellikler eklenebilir belki. OluTavuk vs.
        //public int? OluHayvanAdet { get; set; }
        public bool? Silindi { get; set; }
        //public decimal? IcmeSuyuSayaci { get; set; }
        //Buffel_Sol = 23,
        //Buffel_Sag = 24,
    }
}
