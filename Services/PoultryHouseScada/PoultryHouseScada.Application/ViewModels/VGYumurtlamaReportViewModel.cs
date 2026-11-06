namespace PoultryHouseScada.Application.ViewModels
{
    public sealed class VGYumurtlamaReportViewModel
    {
        public string FileName { get; set; } = string.Empty;

        public string KumesNo { get; set; } = string.Empty;

        public int Year { get; set; }

        public int RecordCount { get; set; }

        public List<VGYumurtlamaReportRowViewModel> Data { get; set; } = new();
    }
}