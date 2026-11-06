namespace EnerjiIzleme.Domain.Models
{
    public class CihazModel
    {
        public int Id { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public bool? Silindi { get; set; }
        public ICollection<Cihaz> Cihazlar { get; set; } = new List<Cihaz>();
    }
}
