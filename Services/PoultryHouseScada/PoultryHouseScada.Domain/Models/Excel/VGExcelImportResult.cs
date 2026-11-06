namespace PoultryHouseScada.Domain.Models.Excel
{
    public class VGExcelImportResult
    {
        public string FileName { get; set; } = string.Empty;

        public DateTime ImportDate { get; set; }

        public List<VGExcelKumesGunlukData> Data { get; set; } = new();

        public List<string> Warnings { get; set; } = new();
    }
}