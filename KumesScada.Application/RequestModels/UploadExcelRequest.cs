using Microsoft.AspNetCore.Http;

namespace KumesScada.API.RequestModels
{
    public sealed class UploadExcelRequest
    {
        public IFormFile File { get; set; } = null!;
    }
}