using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels.RequestModel;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductionReportController : ControllerBase
    {
        private readonly IProductionReportAppService _productionReportAppService;

        public ProductionReportController(IProductionReportAppService productionReportAppService)
        {
            _productionReportAppService = productionReportAppService;
        }
           
        [HttpGet("GetBornProductionReport")]
        public async Task<IActionResult> GetBornProductionReport([FromQuery] ProductionReportRequestModel request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _productionReportAppService.GetProductionReportAsync(request, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }
            catch (InvalidOperationException exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, exception.Message);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Üretim raporu alınırken bir hata oluştu.");
            }
        }
    }
}