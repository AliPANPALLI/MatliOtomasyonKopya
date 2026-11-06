using PanoKimliklendirme.Domain.Models;

namespace PanoKimliklendirme.Application.ViewModels.ReportModels
{
    public class EditNodeModel
    {
        public CihazViewModel? Cihaz { get; set; }
        public PanoViewModel? Pano { get; set; }
        public MakineViewModel? Makine { get; set; }
        public BolumViewModel? Bolum { get; set; }
        public int LokasyonId { get; set; }
    }
}
