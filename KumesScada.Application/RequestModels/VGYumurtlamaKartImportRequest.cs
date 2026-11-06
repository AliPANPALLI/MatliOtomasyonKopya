using Microsoft.AspNetCore.Http;

namespace KumesScada.Application.RequestModels
{
    /// <summary>
    /// Kümesin ana excel'inden ("Yumurtlama Kartı") geçmiş günlük
    /// veriyi içe aktarmak için — bkz. VGYumurtlamaKartExcelReader.
    /// </summary>
    public sealed class VGYumurtlamaKartImportRequest
    {
        public IFormFile File { get; set; } = null!;

        public string KumesNo { get; set; } = string.Empty;
    }
}
