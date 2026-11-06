using PoultryHouseScada.API.RequestModels;
using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace PoultryHouseScada.API.Controllers
{
    [ApiController]
    [Route("api/vg/yumurtlama-report")]
    public sealed class VGYumurtlamaReportController
        : ControllerBase
    {
        private readonly IVGYumurtlamaReportService _reportService;

        private readonly IVGYumurtlamaExcelExportService
            _excelExportService;


        public VGYumurtlamaReportController(
            IVGYumurtlamaReportService reportService,
            IVGYumurtlamaExcelExportService excelExportService)
        {
            _reportService = reportService;
            _excelExportService = excelExportService;
        }


        [HttpPost]
        [Consumes("multipart/form-data")]
        [Produces(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
        public async Task<IActionResult> GetReport(
            [FromForm] VGYumurtlamaUploadRequest request,
            [FromQuery] int year)
        {
            if (request.File == null ||
                request.File.Length == 0)
            {
                return BadRequest(
                    "Excel dosyası seçilmedi.");
            }


            if (year < 2000 ||
                year > 2100)
            {
                return BadRequest(
                    "Geçerli bir yıl giriniz.");
            }


            var extension =
                Path.GetExtension(
                    request.File.FileName)
                    .ToLowerInvariant();


            if (extension != ".xlsx")
            {
                return BadRequest(
                    "Sadece .xlsx dosyası desteklenmektedir.");
            }

            if (request.TasnifFile != null &&
                request.TasnifFile.Length > 0)
            {
                var tasnifExtension =
                    Path.GetExtension(
                        request.TasnifFile.FileName)
                        .ToLowerInvariant();

                if (tasnifExtension != ".xlsx")
                {
                    return BadRequest(
                        "Tasnif dosyası için sadece .xlsx desteklenmektedir.");
                }
            }


            await using var stream =
                request.File.OpenReadStream();

            await using var tasnifStream =
                request.TasnifFile != null && request.TasnifFile.Length > 0
                    ? request.TasnifFile.OpenReadStream()
                    : null;


            // =========================================
            // GİRİŞ EXCELLERİNİ OKU + RAPORU HAZIRLA
            // =========================================

            var report =
                await _reportService.BuildAsync(
                    stream,
                    request.File.FileName,
                    tasnifStream,
                    request.TasnifFile?.FileName,
                    year);


            // =========================================
            // RAPORU YENİ EXCEL DOSYASINA ÇEVİR
            // =========================================

            var excelBytes =
                _excelExportService.Export(report);


            var downloadFileName =
                $"Yumurtlama-{year}.xlsx";


            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                downloadFileName);
        }
    }
}