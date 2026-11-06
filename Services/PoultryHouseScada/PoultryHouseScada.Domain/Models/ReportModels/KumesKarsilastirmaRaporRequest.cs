namespace PoultryHouseScada.Domain.Models.ReportModels
{
    public sealed class KumesKarsilastirmaRaporRequest
    {
        public List<int> KumesIds { get; set; } = new();
        public DateTime BaslangicZamani { get; set; }
        public DateTime BitisZamani { get; set; }
        public int DakikaInterval { get; set; }
    }
}
