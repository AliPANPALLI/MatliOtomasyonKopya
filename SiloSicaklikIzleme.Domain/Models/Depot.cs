namespace SiloSicaklikIzleme.Domain.Models
{
    public class Depot
    {
        public Guid Id { get; set; }
        public Guid FacilityId { get; set; }
        public int DepotNo { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
