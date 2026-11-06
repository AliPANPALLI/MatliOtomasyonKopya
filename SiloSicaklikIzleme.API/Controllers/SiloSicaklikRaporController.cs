using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SiloSicaklikIzleme.Application.Interfaces;

namespace SiloSicaklikIzleme.API.Controllers
{
    // Tüm endpoint'lerde tesisKodu = FACILITY.Code (örn. "OSB-YEM", "MERKEZ-YEM");
    // verilmezse tüm tesisler döner.
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SiloSicaklikRaporController : ControllerBase
    {
        private readonly ISiloSicaklikRaporAppService _siloSicaklikRaporAppService;

        public SiloSicaklikRaporController(ISiloSicaklikRaporAppService siloSicaklikRaporAppService)
        {
            _siloSicaklikRaporAppService = siloSicaklikRaporAppService;
        }

        [HttpGet("Tesisler")]
        public async Task<IActionResult> GetTesisler()
        {
            var result = await _siloSicaklikRaporAppService.GetTesislerAsync();
            return Ok(result);
        }

        [HttpGet("Depolar")]
        public async Task<IActionResult> GetDepolar(string? tesisKodu = null)
        {
            var result = await _siloSicaklikRaporAppService.GetDepolarAsync(tesisKodu);
            return Ok(result);
        }

        [HttpGet("Kablolar")]
        public async Task<IActionResult> GetKablolar(string? tesisKodu = null, Guid? depotId = null)
        {
            var result = await _siloSicaklikRaporAppService.GetKablolarAsync(tesisKodu, depotId);
            return Ok(result);
        }

        [HttpGet("Sensorler")]
        public async Task<IActionResult> GetSensorler(string? tesisKodu = null, Guid? cableId = null)
        {
            var result = await _siloSicaklikRaporAppService.GetSensorlerAsync(tesisKodu, cableId);
            return Ok(result);
        }

        [HttpGet("AnlikDegerler")]
        public async Task<IActionResult> GetAnlikDegerler(string? tesisKodu = null, Guid? depotId = null)
        {
            var result = await _siloSicaklikRaporAppService.GetAnlikDegerlerAsync(tesisKodu, depotId);
            return Ok(result);
        }

        [HttpGet("SicaklikRaporu")]
        public async Task<IActionResult> GetSicaklikRaporu(
            DateTime baslangic,
            DateTime bitis,
            string periyot = "saatlik",
            string? tesisKodu = null,
            Guid? depotId = null,
            [FromQuery] List<Guid>? sensorIds = null)
        {
            var result = await _siloSicaklikRaporAppService.GetSicaklikRaporuAsync(
                AsUtc(baslangic),
                AsUtc(bitis),
                periyot,
                tesisKodu,
                depotId,
                sensorIds);

            return Ok(result);
        }

        [HttpGet("AlarmKurallari")]
        public async Task<IActionResult> GetAlarmKurallari(string? tesisKodu = null, Guid? depotId = null)
        {
            var result = await _siloSicaklikRaporAppService.GetAlarmKurallariAsync(tesisKodu, depotId);
            return Ok(result);
        }

        [HttpGet("AktifAlarmlar")]
        public async Task<IActionResult> GetAktifAlarmlar(string? tesisKodu = null, Guid? depotId = null)
        {
            var result = await _siloSicaklikRaporAppService.GetAktifAlarmlarAsync(tesisKodu, depotId);
            return Ok(result);
        }

        // IANA adı ICU gerektirir (Windows Server 2016 ve öncesi / InvariantGlobalization'da
        // yok); bulunamazsa Windows adına düşülür.
        private static readonly TimeZoneInfo TurkiyeSaatDilimi =
            TimeZoneInfo.TryFindSystemTimeZoneById("Europe/Istanbul", out var tz)
                ? tz
                : TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        // Postgres taraftaki kolonlar timestamptz; Npgsql yalnızca Kind=Utc kabul ediyor.
        // - "Z" ile gelen (Kind=Utc) olduğu gibi kullanılır.
        // - "+03:00" gibi offset'le gelen değeri model binding sunucu yerel saatine
        //   çevirir (Kind=Local); gerçek UTC'ye geri çevrilir.
        // - Offset'siz gelen (Kind=Unspecified) değer Türkiye saati kabul edilir.
        private static DateTime AsUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => TimeZoneInfo.ConvertTimeToUtc(value, TurkiyeSaatDilimi),
        };
    }
}
