namespace MachineMonitoring.Domain.Models.ReportModels
{
    public class MachineDashboardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string State { get; set; } = "Stop"; // "Run" | "Stop" | "Error"
        public bool IsConnected { get; set; }

        public TodayDurationsDto Today { get; set; } = new();
        public HistoryDurationsDto History { get; set; } = new();
    }

    public class TodayDurationsDto
    {
        public string Run { get; set; } = "00:00:00";   // HH:mm:ss
        public string Stop { get; set; } = "00:00:00";
        public string Error { get; set; } = "00:00:00";
    }

    public class HistoryDurationsDto
    {
        public List<int> Run { get; set; } = new();     // 10 eleman, saat cinsinden
        public List<int> Stop { get; set; } = new();
        public List<int> Error { get; set; } = new();
    }

}