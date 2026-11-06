using System.Text.Json;
using HegSogutmaScada.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HegSogutmaScada.API.Controllers;

//[Authorize]
[ApiController]
//[Route("api/heg/alarmlar")]
[Route("api/[controller]")]

public sealed class HegAlarmController : ControllerBase
{
    private readonly IHegAlarmAppService _service;

    public HegAlarmController(IHegAlarmAppService service)
    {
        _service = service;
    }

    [HttpGet("ekipmanlar")]
    public async Task<IActionResult> GetEquipment([FromQuery(Name = "tur")] string equipmentType, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.GetEquipmentAsync(equipmentType, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("ayarlar")]
    public async Task<IActionResult> GetSetting(
        [FromQuery(Name = "tur")] string equipmentType,
        [FromQuery] string equipmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetSettingAsync(equipmentType, equipmentId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPut("ayarlar")]
    public async Task<IActionResult> SaveSetting([FromBody] JsonElement request, CancellationToken cancellationToken)
        => Ok(await _service.SaveSettingAsync(request, cancellationToken));

    [HttpPost("parameters")]
    public async Task<IActionResult> SaveParameters(
        [FromQuery] string? equipmentId,
        [FromBody] JsonElement request,
        CancellationToken cancellationToken)
        => Ok(await _service.SaveParametersAsync(equipmentId, request, cancellationToken));

    [HttpGet("aktif")]
    public async Task<IActionResult> GetActive(
        [FromQuery(Name = "tur")] string? equipmentType,
        [FromQuery] string? equipmentId,
        [FromQuery(Name = "sayfa")] int page = 1,
        [FromQuery(Name = "sayfaBoyutu")] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _service.GetActiveAsync(equipmentType, equipmentId, page, pageSize, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("gecmis")]
    public async Task<IActionResult> GetHistory(
        [FromQuery(Name = "baslangic")] DateTimeOffset? start,
        [FromQuery(Name = "bitis")] DateTimeOffset? end,
        [FromQuery(Name = "tur")] string? equipmentType,
        [FromQuery] string? equipmentId,
        [FromQuery(Name = "sayfa")] int page = 1,
        [FromQuery(Name = "sayfaBoyutu")] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _service.GetHistoryAsync(start, end, equipmentType, equipmentId, page, pageSize, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("bildirimler")]
    public async Task<IActionResult> GetNotifications([FromQuery] long alarmOlayId, CancellationToken cancellationToken)
        => Ok(await _service.GetNotificationsAsync(alarmOlayId, cancellationToken));

    [HttpGet("olcumler")]
    public async Task<IActionResult> GetMeasurements(
        [FromQuery(Name = "tur")] string? equipmentType,
        [FromQuery] string? equipmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.GetMeasurementsAsync(equipmentType, equipmentId, cancellationToken);
            return !string.IsNullOrWhiteSpace(equipmentId) && result.Count == 0 ? NotFound() : Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("ozet")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
        => Ok(await _service.GetSummaryAsync(cancellationToken));
}
