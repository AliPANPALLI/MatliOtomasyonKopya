using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.RequestModels;
using PoultryHouseScada.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace PoultryHouseScada.API.Controllers
{
    /// <summary>
    /// "RaporKategori"/"RaporVeri" tablolarındaki haftalık hedef
    /// eğrileri (örn. Yumurtlama Takip "%" grafiğindeki "HEDEF %" /
    /// "HEDEF GR") için admin veri girişi ve okuma ucu.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RaporVeriController : BaseApiController<RaporVeriViewModel>
    {
        private readonly IRaporVeriAppService _raporVeriAppService;

        public RaporVeriController(IRaporVeriAppService raporVeriAppService)
            : base(raporVeriAppService)
        {
            _raporVeriAppService = raporVeriAppService;
        }

        [HttpGet("GetKategoriler")]
        public async Task<IActionResult> GetKategoriler()
        {
            var result = await _raporVeriAppService.GetKategoriler();
            return Ok(result);
        }

        [HttpGet("GetByKategoriAdi")]
        public async Task<IActionResult> GetByKategoriAdi(string kategoriAdi)
        {
            var result = await _raporVeriAppService.GetByKategoriAdi(kategoriAdi);
            return Ok(result);
        }

        [HttpPost("SaveGuides")]
        public async Task<IActionResult> SaveGuides([FromBody] RaporVeriSaveRequest request)
        {
            await _raporVeriAppService.SaveGuides(request);

            return Ok(new
            {
                success = true,
                message = "Hedef tanımları kaydedildi."
            });
        }

        [HttpDelete("DeleteGuide")]
        public async Task<IActionResult> DeleteGuide(int id)
        {
            await _raporVeriAppService.DeleteGuide(id);

            return Ok(new
            {
                success = true,
                message = "Hedef kaydı silindi."
            });
        }

        /// <summary>
        /// Kümesin "Yumurtlama Kartı" ana excel'indeki "VERİLER"
        /// sayfasını okuyup "HEDEF %" / "HEDEF GR" hedef eğrilerini bu
        /// kümese özel kategoriler olarak kaydeder. Bu ana excel'ler
        /// genelde eski .xls (binary) formatında arşivlenmiş olduğu
        /// için (diğer VG upload uçlarından farklı olarak) NPOI
        /// tabanlı okuyucu .xls'i de destekler — bkz. RaporVeriExcelReader.
        /// </summary>
        [HttpPost("ImportFromExcel")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportFromExcel(
            [FromForm] RaporVeriExcelImportRequest request,
            CancellationToken cancellationToken)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Excel dosyası seçilmedi.");

            if (string.IsNullOrWhiteSpace(request.KumesNo))
                return BadRequest("Kümes numarası zorunludur.");

            var extension = Path.GetExtension(request.File.FileName).ToLowerInvariant();

            if (extension != ".xlsx" && extension != ".xls")
                return BadRequest("Sadece .xlsx veya .xls dosyası desteklenmektedir.");

            await using var stream = request.File.OpenReadStream();

            var result = await _raporVeriAppService.ImportFromExcelAsync(
                stream,
                request.File.FileName,
                request.KumesNo.Trim(),
                cancellationToken);

            return Ok(result);
        }
    }
}
