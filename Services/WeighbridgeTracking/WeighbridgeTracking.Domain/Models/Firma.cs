namespace WeighbridgeTracking.Domain.Models
{
    public class Firma
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool? Silindi { get; set; }
        public ICollection<KantarGuvenlik> KantarGuvenlikler { get; set; } = new List<KantarGuvenlik>();
    }
}
