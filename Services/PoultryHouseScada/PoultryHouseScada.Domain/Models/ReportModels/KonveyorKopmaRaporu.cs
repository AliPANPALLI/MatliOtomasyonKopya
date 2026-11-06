namespace PoultryHouseScada.Domain.Models.ReportModels
{
    public class KonveyorKopmaRaporu
    {
        public int KonveyorId { get; set; }
        public string? HatAdi { get; set; }
        public string? KopmaNoktasi { get; set; }
        public DateTime? KopmaZamani { get; set; }
        public DateTime? GiderildigiZaman { get; set; }
        public TimeSpan? Fark { get; set; }
    }
}
