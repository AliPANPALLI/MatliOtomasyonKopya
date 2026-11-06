namespace PoultryHouseScada.Application.RequestModels
{
    public class VGExcelImportRequest
    {
        public Stream FileStream { get; set; } = Stream.Null;

        public string FileName { get; set; } = string.Empty;

        public int? Year { get; set; }
    }
}