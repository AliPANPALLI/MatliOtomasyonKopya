namespace MakineIzleme.Domain.Models.ReportModels
{
    public class MakineIzlemeDetayReportModel
    {
        public string Date { get; set; } = "";     // "31.12.2025"
        public string Time { get; set; } = "";     // "14:25"
        public string Status { get; set; } = "";   // Run/Stop/Error
        public int Duration { get; set; }          // dk
        public string Desc { get; set; } = "";
    }

    public class MachineLogRowDto
    {
        public string Date { get; set; } = "";   // "31.12.2025"
        public string Time { get; set; } = "";   // "14:25"
        public string Status { get; set; } = ""; // Run/Stop/Error
        public int Duration { get; set; }        // dk
        public string Desc { get; set; } = "";
    }
}
