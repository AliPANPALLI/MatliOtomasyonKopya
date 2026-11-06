namespace KumesScada.Domain.Models.ReportModels
{
    public class KonveyorZamanCizelgesi
    {
        public int KonveyorId { get; set; }
        public string? Name { get; set; }
        public string? HatName { get; set; }
        public DateTime? BaslamaZamani { get; set; }
        public DateTime? BitisZamani { get; set; }
        public string? DurumAciklama { get; set; }
        public string? Durum { get; set; }
        public TimeSpan? Fark { get; set; }
    }
}
