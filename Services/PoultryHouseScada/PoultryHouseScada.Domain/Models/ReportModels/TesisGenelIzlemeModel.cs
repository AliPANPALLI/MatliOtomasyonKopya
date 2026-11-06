namespace PoultryHouseScada.Domain.Models.ReportModels
{
    public class TesisGenelIzlemeModel
    {
        public DateTime? KayitZamani { get; set; }
        //public int? CizimId { get; set; }
        public int KumesId { get; set; }
        public string? KumesName { get; set; }
        public int? DonemId { get; set; }
        public string? DonemName { get; set; }
        public string? DonemGun { get; set; }
        public string? DonemHafta { get; set; }
        public string? HayvanAdet { get; set; }
        public string? OluHayvanAdet { get; set; }
        public string? SicaklikKumes { get; set; }
        public string? NemKumes { get; set; }
        public string? Co2Kumes { get; set; }
        public string? BasincKumes { get; set; }
        public string? SuTuketimBugun { get; set; }
        public string? YemTuketimBugun { get; set; }
        public string? YumurtaAdet { get; set; }
        public bool? AlarmVar { get; set; }
        public List<string>? AlarmMesajList { get; set; }
        public bool? Aktif { get; set; } = false;
        public bool? PlcBagli { get; set; } = false;
        public bool? SorumluMu { get; set; } = false;
        public bool? YemRasyonYetki { get; set; } = false;
        public bool? RaporRehberYetki { get; set; } = false;
        public string? KumesServisApiUrl { get; set; }
    }
}