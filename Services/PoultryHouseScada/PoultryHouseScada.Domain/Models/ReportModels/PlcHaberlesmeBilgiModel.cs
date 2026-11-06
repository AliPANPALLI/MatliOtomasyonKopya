namespace PoultryHouseScada.Domain.Models.ReportModels
{
    public class PlcHaberlesmeBilgiModel
    {
        public bool? PlcBagli { get; set; }
        public bool? PlcCalisiyor { get; set; }
        public decimal? ByteDensity { get; set; }
        public decimal? PingAverage { get; set; }
        public decimal? LastPackageLength { get; set; }
        public bool? PlcAutoConnect { get; set; }
        public string? PlcIpAdres { get; set; }
        public bool? PlcKumesAktif { get; set; }
    }
}
