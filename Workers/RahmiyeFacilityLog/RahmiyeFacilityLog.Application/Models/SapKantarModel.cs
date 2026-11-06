namespace RahmiyeFacilityLog.Application.Models
{
    /// <summary>
    /// SAP ZWS_F0017 kantar (tartım) servisinin T_KANTAR/item elemanına karşılık gelir.
    /// </summary>
    public class SapKantarModel
    {
        public string? FisNo { get; set; }
        public string? KantarId { get; set; }
        public string? Surec { get; set; }
        public string? Plaka { get; set; }
        public string? Lgort { get; set; }

        public string? FirstDate { get; set; }
        public string? FirstTime { get; set; }
        public decimal? FirstValue { get; set; }
        public string? FirstUser { get; set; }
        public string? FirstOk { get; set; }

        public string? SecondDate { get; set; }
        public string? SecondTime { get; set; }
        public decimal? SecondValue { get; set; }
        public string? SecondUser { get; set; }
        public string? SecondOk { get; set; }

        public decimal? LastValue { get; set; }

        public string? ThirdDate { get; set; }
        public string? ThirdTime { get; set; }
        public string? ThirdUser { get; set; }
        public string? ThirdOk { get; set; }

        public decimal? NetValue { get; set; }

        public string? DriverId { get; set; }
        public string? DriverName { get; set; }
        public string? DriverSurname { get; set; }
        public string? TelNumber { get; set; }

        public string? Finished { get; set; }
    }
}
