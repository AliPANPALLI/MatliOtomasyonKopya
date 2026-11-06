namespace EnerjiIzleme.Domain.Models.ReportModels
{
    public class EnerjiMaliyetParametreAtamaRequest
    {
        public int EnerjiMaliyetGrupId { get; set; }
        public int NodeId { get; set; }
        public string NodeType { get; set; } = null!;
    }
}
