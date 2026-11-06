using KumesScada.Application.ViewModels;

namespace KumesScada.Application.RequestModels
{
    public class RaporVeriSaveRequest
    {
        public string RaporKategoriAd { get; set; } = string.Empty;

        public List<RaporVeriViewModel> Guides { get; set; } = new();
    }
}
