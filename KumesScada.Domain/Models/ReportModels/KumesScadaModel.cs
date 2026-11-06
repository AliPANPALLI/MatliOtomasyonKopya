namespace KumesScada.Domain.Models.ReportModels
{
    public class KumesScadaModel
    {
        //public int? CizimId { get; set; }
        public int KumesId { get; set; }
        public string? KumesName { get; set; }
        public string? KumesHmiIpAdres { get; set; }
        public int? DonemId { get; set; }
        public string? DonemName { get; set; }
        public int? DonemGun { get; set; }
        public int? DonemHafta { get; set; }
        public int? CanliHayvanAdet { get; set; }
        public int? CizimId { get; set; }
        public string? CizimUrl { get; set; }
    }
}
