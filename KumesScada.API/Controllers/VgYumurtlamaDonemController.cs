using KumesScada.Application.Interfaces;
using KumesScada.Application.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace KumesScada.API.Controllers
{
    /// <summary>
    /// Yumurtlama Takip -> Veri Girişi sekmesindeki "Yumurtlama Kartı
    /// Excel'ini Oluştur" formunda elle girilen dönem bilgisini
    /// (kümese giriş tarihi / başlangıç hayvan sayısı / ırk) kaydeder —
    /// bir kez girilince kullanıcı bir daha elle girmek zorunda kalmaz,
    /// bkz. IVgYumurtlamaDonemAppService.
    /// </summary>
    [ApiController]
    [Route("api/vg/yumurtlama/donem")]
    public sealed class VgYumurtlamaDonemController : ControllerBase
    {
        private readonly IVgYumurtlamaDonemAppService _service;

        public VgYumurtlamaDonemController(IVgYumurtlamaDonemAppService service)
        {
            _service = service;
        }

        [HttpPost("Save")]
        public async Task<IActionResult> Save(
            [FromBody] VgYumurtlamaDonemSaveRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _service.SaveAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpGet("GetLatestByKumesNo")]
        public async Task<IActionResult> GetLatestByKumesNo(
            string kumesNo,
            CancellationToken cancellationToken)
        {
            var result = await _service.GetLatestByKumesNo(kumesNo, cancellationToken);
            return Ok(result);
        }
    }
}
