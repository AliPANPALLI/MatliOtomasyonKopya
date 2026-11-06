using Microsoft.AspNetCore.Http;

namespace PoultryHouseScada.API.RequestModels
{
    public sealed class UploadExcelRequest
    {
        public IFormFile File { get; set; } = null!;
    }
}