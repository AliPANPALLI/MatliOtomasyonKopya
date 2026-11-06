using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MobilGuncelleme.API.Yetki;
using MobilGuncelleme.Application.Dtos;
using MobilGuncelleme.Application.Interfaces;
using System.Security.Claims;

namespace MobilGuncelleme.API.Controllers
{
    /// <summary>
    /// Sürüm kontrolü ve APK dağıtımı. Kontrol ve İndir mobil uygulamaya (giriş yapmış her kullanıcı),
    /// diğerleri SharedApp'te "Mobil Sürüm Yönetimi" formuna yetkili kullanıcılara açıktır.
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SurumController : ControllerBase
    {
        // IIS ve Kestrel varsayılanları (~30 MB, multipart 128 MB) APK için yetmez.
        private const long ApkBoyutLimiti = 300_000_000;

        private readonly IMobilSurumAppService _mobilSurumAppService;

        public SurumController(IMobilSurumAppService mobilSurumAppService)
        {
            _mobilSurumAppService = mobilSurumAppService;
        }

        /// <summary>
        /// Mobil uygulama girişten sonra bir kez çağırır: cihazı kaydeder ve güncelleme gerekip
        /// gerekmediğini döner.
        /// </summary>
        [HttpPost("Kontrol")]
        public async Task<ActionResult<SurumKontrolSonucDto>> Kontrol([FromBody] CihazBildirDto dto)
        {
            var kullaniciId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(await _mobilSurumAppService.KontrolAsync(dto, kullaniciId));
        }

        /// <summary>
        /// APK'yı stream eder. Range desteği sayesinde kopan indirme kaldığı yerden devam eder.
        /// Pasif veya olmayan sürüm için 404.
        /// </summary>
        [HttpGet("{id:int}/Indir")]
        public async Task<IActionResult> Indir(int id)
        {
            var dosya = await _mobilSurumAppService.IndirmeDosyasiAsync(id);
            if (dosya is null)
                return NotFound();

            return PhysicalFile(dosya.Yol, "application/vnd.android.package-archive", dosya.IndirmeAdi, enableRangeProcessing: true);
        }

        /// <summary>APK ve sürüm bilgisini yükler (multipart/form-data). Form yetkisinde Yazma gerekir.</summary>
        [FormYetki(FormYetkiTuru.Yazma)]
        [HttpPost]
        [RequestSizeLimit(ApkBoyutLimiti)]
        [RequestFormLimits(MultipartBodyLengthLimit = ApkBoyutLimiti)]
        public async Task<ActionResult<SurumDto>> Yukle([FromForm] SurumYukleDto dto, CancellationToken cancellationToken)
        {
            var yukleyen = User.FindFirstValue(ClaimTypes.GivenName) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Ok(await _mobilSurumAppService.YukleAsync(dto, yukleyen, cancellationToken));
        }

        /// <summary>Tüm sürümler, en yeni önce. Form yetkisinde Okuma gerekir.</summary>
        [FormYetki(FormYetkiTuru.Okuma)]
        [HttpGet]
        public async Task<ActionResult<List<SurumDto>>> Listele()
        {
            return Ok(await _mobilSurumAppService.SurumleriListeleAsync());
        }

        /// <summary>
        /// Sürümü aktif/pasif yapar; hatalı sürüm pasife alınınca dağıtım durur.
        /// Form yetkisinde Yazma gerekir.
        /// </summary>
        [FormYetki(FormYetkiTuru.Yazma)]
        [HttpPut("{id:int}/Aktiflik")]
        public async Task<ActionResult<SurumDto>> Aktiflik(int id, [FromBody] AktiflikDto dto)
        {
            return Ok(await _mobilSurumAppService.AktiflikAyarlaAsync(id, dto.Aktif));
        }
    }
}
