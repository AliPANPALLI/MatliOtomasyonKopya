namespace KumesScada.Domain.Models.ReportModels
{
    public sealed class TesisGenelSnapshotModel
    {
        public int LokasyonId { get; set; }
        public List<TesisGenelIzlemeModel> Kumesler { get; set; } = new();
        public TesisGenelKartModel? TesisOzet { get; set; }
        //public DateTime SunucuZamani { get; set; }
    }

    public sealed class TesisGenelIzlemeYetkiModel
    {
        public int LokasyonId { get; set; }
        public bool ErisimVar { get; set; }
        public List<TesisGenelIzlemeKumesYetkiModel> KumesYetkileri { get; set; } = new();
        public DateTime OlusturmaZamani { get; set; }
    }

    public sealed class TesisGenelIzlemeKumesYetkiModel
    {
        public int KumesId { get; set; }
        public bool SorumluMu { get; set; }
        public bool YemRasyonYetki { get; set; }
        public bool RaporRehberYetki { get; set; }
    }
}