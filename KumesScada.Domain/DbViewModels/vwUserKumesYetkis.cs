namespace KumesScada.Domain.DbViewModels
{
    public class vwUserKumesYetkis
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int KumesId { get; set; }
        public int LokasyonId { get; set; }
        public int SirketId { get; set; }
        public bool? Read { get; set; }
        public bool? Write { get; set; }
        public bool? SorumluMu { get; set; }
        public bool? Silindi { get; set; }
        public DateTime? SilinmeZamani { get; set; }
        public bool? YemRasyonYetki { get; set; }
        public bool? RaporRehberYetki { get; set; }
        public bool? Aktif { get; set; }
    }
}
