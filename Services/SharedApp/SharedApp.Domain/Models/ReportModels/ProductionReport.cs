namespace SharedApp.Domain.Models.ReportModels
{
    public class ProductionReport
    {
        public string? LotNo { get; set; }
        public int? MaterialDocumentID { get; set; }
        public int? ProductionID { get; set; }
        public int? BatchOrderID { get; set; }
        public string? FormulaName { get; set; }
        public string? OrderNo { get; set; }
        public int? BatchNo { get; set; }
        public string? MaterialCode { get; set; }
        public string? MaterialName { get; set; }
        public string? MaterialGroup { get; set; }
        public string? ContainerCode { get; set; }
        public DateTime? BeginDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Duration { get; set; }
        public decimal? PlanAmount { get; set; }
        public decimal? RealAmount { get; set; }
        public decimal? Diff { get; set; }
        public decimal? Perc { get; set; }
        public DateTime? HammBeginDate { get; set; }
        public DateTime? HammStartDate { get; set; }
        public DateTime? HammCompleteDate { get; set; }
        public DateTime? HammEndDate { get; set; }
        public int? HammDuration { get; set; }
        public DateTime? MixBeginDate { get; set; }
        public DateTime? MixStartDate { get; set; }
        public DateTime? MixCompleteDate { get; set; }
        public DateTime? MixEndDate { get; set; }
        public int? MixDuration { get; set; }
        public int? MixActionDuration { get; set; }
        public int? MixGateDuration { get; set; }
        public int? MixDurationNoGate { get; set; }
        public decimal? MolassesDuration { get; set; }
        public decimal? MolassesDurationSet { get; set; }
        public int? OutOfTolerance { get; set; }
        public int? OutOfTolerance20 { get; set; }
        public int? MlsDuration { get; set; }
        public decimal? MelasFault { get; set; }
        public decimal? MelasFaultRatio { get; set; }
        public decimal? VinasFault { get; set; }
        public decimal? VinasFaultRatio { get; set; }
        public decimal? MixAltiRuntime { get; set; }
        public decimal? MixRuntime { get; set; }
        public int? SieveActive { get; set; }
        public int? SievePassive { get; set; }
        public decimal? SieveStatePercentage { get; set; }
        public int? UniteID { get; set; }
        public int? PelletOrderID { get; set; }
    }
}
