namespace EnergyMonitoring.Application.ViewModels.RequestModel
{
    public class EnerjiDagilimRaporRequest
    {
        public List<string> SecilenNodeIdsAndTypes { get; set; }
        public DateTime BaslangicZamani { get; set; }
        public DateTime BitisZamani { get; set; }
        public DateTime OncekiDonemBaslangicZamani { get; set; }
        public DateTime OncekiDonemBitisZamani { get; set; }
    }
}
