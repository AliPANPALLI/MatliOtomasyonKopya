namespace PoultryHouseScada.Domain.Models.ReportModels
{
    public class KumesRaporModel
    {
        public DateTime? KayitZamani { get; set; }
        public int KumesId { get; set; }
        public string? KumesName { get; set; }
        public string? KumesServisApiUrl { get; set; }
        //public int? DonemId { get; set; }
        //public string? DonemName { get; set; }
        //public int? DonemGun { get; set; }
        //public int? DonemHafta { get; set; }
        public int? CanliHayvanAdet { get; set; }
        public int? OlenSayisiBugun { get; set; }
        public decimal? SicaklikKumes { get; set; }
        public decimal? SicaklikDis { get; set; }
        public decimal? Sicaklik1 { get; set; }
        public decimal? Sicaklik2 { get; set; }
        public decimal? Sicaklik3 { get; set; }
        public decimal? Sicaklik4 { get; set; }
        public decimal? Sicaklik5 { get; set; }
        public decimal? Sicaklik6 { get; set; }
        //public decimal? Sicaklik7 { get; set; }
        //public decimal? Sicaklik8 { get; set; }
        public decimal? NemKumes { get; set; }
        public decimal? NemDis { get; set; }
        public decimal? Nem1 { get; set; }
        public decimal? Nem2 { get; set; }
        public decimal? Nem3 { get; set; }
        public decimal? Nem4 { get; set; }
        public decimal? Nem5 { get; set; }
        public decimal? Nem6 { get; set; }
        //public decimal? Nem7 { get; set; }
        //public decimal? Nem8 { get; set; }
        public decimal? Silo1 { get; set; }
        public decimal? Silo2 { get; set; }
        public bool? Silo1Bagli { get; set; }
        public bool? Silo2Bagli { get; set; }
        public decimal? BasincKumes { get; set; }
        public decimal? Co2Kumes { get; set; }
        public decimal? SuTuketimBugun { get; set; }
        public decimal? PedSuTuketimBugun { get; set; }
        public decimal? IcmeSuyuSayac { get; set; }
        public decimal? PedSuSayac { get; set; }
        public decimal? YemTuketimBugun { get; set; }
        public int? YumurtaAdet { get; set; }

        public bool? PlcBagliOkunuyor { get; set; }
        public bool? Aktif { get; set; }
        public decimal? HayvanBasinaYemTuketim { get; set; }
        public decimal? HayvanBasinaSuTuketim { get; set; }
        public decimal? HayvanBasinaYumurtaAdet { get; set; }
        // Yeni alanlar
        public int? HaftaSayisi { get; set; }
        // Yem tüketimi / yumurta adedi
        public decimal? YumurtaBasinaYemTuketim { get; set; }
        // Yumurta adedi / canlı hayvan sayısı * 100
        public decimal? Randiman { get; set; }
        // Guide çizgisi
        public decimal? RandimanGuide { get; set; }
        public decimal? HaftalikOlum { get; set; }
        public decimal? CatiKontrolDeger { get; set; }
    }
}
