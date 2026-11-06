namespace MachineMonitoring.Domain.Models
{
    public class Bolum
    {
        public int Id { get; set; }
        public int LokasyonId { get; set; }
        public string? Name { get; set; }
        public ICollection<Makine> Makineler { get; set; } = new List<Makine>();
    }
}