namespace KumesScada.Domain.Models.ReportModels
{
    public class KonveyorNode
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public byte? Tip { get; set; }
        public int? ParentId { get; set; }
        public int LokasyonId { get; set; }
        public bool? Silindi { get; set; }
        public List<KonveyorNode> Children { get; set; } = new List<KonveyorNode>();
    }
}
