namespace KantarTakip.Application.DTOs
{
    public class SapKantarDto
    {
        public string? FisNo { get; set; }
        public int? KantarId { get; set; }
        public string? Surec { get; set; }
        public string? Plaka { get; set; }
        public string? Lgort { get; set; }

        public DateTime? FirstAt { get; set; }
        public decimal? FirstValue { get; set; }
        public string? FirstUser { get; set; }
        public bool FirstOk { get; set; }

        public DateTime? SecondAt { get; set; }
        public decimal? SecondValue { get; set; }
        public string? SecondUser { get; set; }
        public bool SecondOk { get; set; }

        public decimal? NetValue { get; set; }
        public bool Finished { get; set; }

        public string? DriverName { get; set; }
        public string? DriverSurname { get; set; }
    }
}