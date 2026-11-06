namespace PoultryHouseScada.Application.ViewModels
{
    public sealed class VGYumurtlamaAllReportViewModel
    {
        public string FileName { get; set; } = string.Empty;

        public int Year { get; set; }

        public int KumesCount { get; set; }

        public List<VGYumurtlamaKumesReportViewModel> Kumesler { get; set; } = new();
    }

    public sealed class VGYumurtlamaKumesReportViewModel
    {
        public string KumesNo { get; set; } = string.Empty;

        public int RecordCount { get; set; }

        public List<VGYumurtlamaReportRowViewModel> Data { get; set; } = new();
    }
}