namespace PoultryHouseScada.Domain.Models.ReportModels
{
    public class KonveyorZamanListesi
    {
        public int KonveyorId { get; set; }
        public string? Name { get; set; }
        public double? CalisiyorSuresi { get; set; }
        public double? BostaSuresi { get; set; }
        public double? ArizaSuresi { get; set; }
    }
}