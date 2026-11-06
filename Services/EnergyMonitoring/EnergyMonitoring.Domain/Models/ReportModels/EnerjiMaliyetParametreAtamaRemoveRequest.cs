namespace EnergyMonitoring.Domain.Models.ReportModels
{
    public class EnerjiMaliyetParametreAtamaRemoveRequest
    {
        public int NodeId { get; set; }
        public string NodeType { get; set; } = null!;
    }
}
