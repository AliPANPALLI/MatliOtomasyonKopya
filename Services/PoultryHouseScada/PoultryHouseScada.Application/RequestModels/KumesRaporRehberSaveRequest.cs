using PoultryHouseScada.Application.ViewModels;

namespace PoultryHouseScada.Application.RequestModels
{
    public class KumesRapoRehberSaveRequest
    {
        public int LokasyonId { get; set; }
        public string? RehberAdi { get; set; }
        public int? HayvanCinsId { get; set; }
        public List<KumesRaporRehberViewModel> Guides { get; set; } = new();
    }
}
