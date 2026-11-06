namespace EnergyMonitoring.Domain.Models.ReportModels
{
    public class HaftalikCihazTuketimDetayModel
    {
        public int CihazId { get; set; }
        public string CihazName { get; set; }
        public string MakineName { get; set; }
        public string BolumName { get; set; }
        public decimal IlkOkuma_Wh { get; set; }
        public decimal SonOkuma_Wh { get; set; }
        public decimal Tuketim_kWh { get; set; }
        public DateTime IlkOkumaZamani { get; set; }
        public DateTime SonOkumaZamani { get; set; }
        public List<HaftalikCihazTuketimDetayModel> AltCihazlar { get; set; } = new();
    }
}
