
namespace PoultryHouseScada.Domain.Models.ReportModels
{
    public class KumesDonemModel
    {
        public int? KumesId { get; set; }
        public int? DonemId { get; set; }
        public string? DonemName { get; set; }
        public DateTime? GirisTarihi { get; set; }
        public DateTime? CikisTarihi { get; set; }
        public string? DonemGun { get; set; }
        public string? DonemHafta { get; set; }
        public string? HayvanHafta { get; set; }
        public bool? Aktif { get; set; }
        public string? Cins { get; set; }
        public int? GirenHayvanAdet { get; set; }
        public int? CanliHayvanAdet { get; set; }
        public int? BugunOlenHayvanAdet { get; set; }
        public int? ToplamOlenHayvanAdet { get; set; }
    }
}
