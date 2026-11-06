namespace EnerjiIzleme.Domain.Models.ReportModels
{
    public class EnerjiMaliyetAtamaTreeNodeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string NodeType { get; set; } = null!;
        public int? EnerjiMaliyetGrupId { get; set; }
        public string? EnerjiMaliyetGrupName { get; set; }
        public bool DirektAtamaMi { get; set; }
        public bool MirastanMi { get; set; }
        public List<EnerjiMaliyetAtamaTreeNodeDto> Children { get; set; } = new();
    }
}
