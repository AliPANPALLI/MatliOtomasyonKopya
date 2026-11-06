using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace PoultryHouseScada.Api.Controllers
{
    [ApiController]
    [Route("api/vg/excel")]
    public class VGExcelController : ControllerBase
    {
        private readonly IVGExcelImportService _service;


        public VGExcelController(
            IVGExcelImportService service)
        {
            _service = service;
        }


        [HttpPost("oku")]
        public async Task<IActionResult> ReadExcel(
      IFormFile file,
      [FromQuery] int? year,
      CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Excel dosyası seçilmedi.");

            await using var stream = file.OpenReadStream();

            var request = new VGExcelImportRequest
            {
                FileName = file.FileName,
                FileStream = stream,
                Year = year
            };

            var result = await _service.ImportAsync(
                request,
                cancellationToken);

            return Ok(result);
        }
    }
}