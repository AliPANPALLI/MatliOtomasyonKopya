using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Infrastructure.Sender;
using SharedKernel.Extensions;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static SharedKernel.ViewModels.UserYetkiClaimViewModel;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YetkiController : ControllerBase
    {
        private readonly IYetkiAppService _yetkiAppService;
        private readonly IUserExtension _userExtension;
        private readonly EmailSender _emailSender;

        public YetkiController(IYetkiAppService yetkiAppService, IUserExtension userExtension, EmailSender emailSender)
        {
            _yetkiAppService = yetkiAppService;
            _userExtension = userExtension;
            _emailSender = emailSender;
        }

        [HttpGet("GetAllTopBar")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTopBar()
        {
            var result = await _yetkiAppService.GetAllTopBar();
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllSideBar")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllSideBar()
        {
            var result = await _yetkiAppService.GetAllSideBar();
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetUserFormYetkiByUserIdAndFormLink")]
        public async Task<IActionResult> GetUserFormYetkiByUserIdAndFormLink(string formLink)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _yetkiAppService.GetUserFormYetkiByUserIdAndFormLink(userId, formLink);
            return Ok(result);
        }

        [HttpGet("GetvwUserEnerjiIzlemeYetkiByUserIdAndLokasyonId")]
        public async Task<IActionResult> GetvwUserEnerjiIzlemeYetkiByUserIdAndLokasyonId(int lokasyonId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _yetkiAppService.GetvwUserEnerjiIzlemeYetkiByUserIdAndLokasyonId(userId, lokasyonId);
            return Ok(result);
        }

        [HttpGet("GetUserFormYetkiListByUserId")]
        [Authorize]
        public async Task<IActionResult> GetUserFormYetkiListByUserId()
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _yetkiAppService.GetUserFormYetkiListByUserId(userId);
            return Ok(result);
        }

        [HttpGet("GetUserPermissions")]
        [Authorize]
        public async Task<UserPermissions> GetUserPermissions()
        {
                var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
                var userYetkiliFormIds = (await _yetkiAppService.GetUserFormYetkiListByUserId(userId)).Select(x => x.FormId).ToList();
                var panoKimlikYetkiler = await _yetkiAppService.GetPermissionDetailForPanoKimliklendirme(userId);
                var kumesScadaYetkiler = await _yetkiAppService.GetPermissionDetailForKumesScada(userId);
                var suHatScadaYetkiler = await _yetkiAppService.GetPermissionDetailForSuHatScada(userId);
                var enerjiIzlemeYetkiler = await _yetkiAppService.GetPermissionDetailForEnerjiIzleme(userId);
                //var sunucuYetkiler = await _yetkiAppService.GetPermissionDetailForSunucuScada(userId);
                //var tesisScadaYetkiler = await _yetkiAppService.GetPermissionDetailForTesisScada(userId);
                var topolojiYetkiler = await _yetkiAppService.GetPermissionDetailForTopoloji(userId);
                var ticketVeYonetimYetkiler = await _yetkiAppService.GetPermissionDetailForTicketAndYonetim(userId);

                var kullaniciYetki = new UserPermissions
                {
                    KullaniciYetkiler = new List<PermissionDetail>()
                };

                kullaniciYetki.KullaniciYetkiler.AddRange(panoKimlikYetkiler.Where(x => userYetkiliFormIds.Contains(x.FormId)));
                kullaniciYetki.KullaniciYetkiler.AddRange(kumesScadaYetkiler.Where(x => userYetkiliFormIds.Contains(x.FormId)));
                kullaniciYetki.KullaniciYetkiler.AddRange(suHatScadaYetkiler.Where(x => userYetkiliFormIds.Contains(x.FormId)));
                kullaniciYetki.KullaniciYetkiler.AddRange(enerjiIzlemeYetkiler.Where(x => userYetkiliFormIds.Contains(x.FormId)));
                //kullaniciYetki.KullaniciYetkiler.AddRange(sunucuYetkiler.Where(x => userYetkiliFormIds.Contains(x.FormId)));
                //kullaniciYetki.KullaniciYetkiler.AddRange(tesisScadaYetkiler.Where(x => userYetkiliFormIds.Contains(x.FormId)));
                kullaniciYetki.KullaniciYetkiler.AddRange(topolojiYetkiler.Where(x => userYetkiliFormIds.Contains(x.FormId)));
                kullaniciYetki.KullaniciYetkiler.AddRange(ticketVeYonetimYetkiler.Where(x => userYetkiliFormIds.Contains(x.FormId)));

                // Boş dizileri null'a çevir: JWT'ye gömülürken JSON.Serialize null alanları atlıyor,
                // frontend zaten bu alanları "?? []" ile null-safe okuyor (general.guard.ts, sidebar.component.ts).
                foreach (var detail in kullaniciYetki.KullaniciYetkiler)
                {
                    if (detail.LokasyonIds != null && detail.LokasyonIds.Count == 0) detail.LokasyonIds = null;
                    if (detail.CizimIds != null && detail.CizimIds.Count == 0) detail.CizimIds = null;
                    if (detail.KumesIds != null && detail.KumesIds.Count == 0) detail.KumesIds = null;
                    if (detail.AllowedPaths != null && detail.AllowedPaths.Count == 0) detail.AllowedPaths = null;
                }

                return kullaniciYetki;
        }

        [HttpGet("GetUserLinkYetkiByLink")]
        [Authorize]
        public async Task<IActionResult> GetUserLinkYetkiByLink(string link)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _yetkiAppService.GetUserLinkYetkiByUserIdAndLink(userId, link);
            return Ok(result);
        }

        [HttpPut("SendEmailForScheduledMaintenance")]
        public async Task<IActionResult> SendEmailForScheduledMaintenance([FromForm] string emailKonu, [FromForm] string emailGovde, [FromForm] bool panoKimlikKullanicisiMi, [FromForm] bool kumesScadaKullanicisiMi, [FromForm] bool suHatScadaKullanicisiMi, [FromForm] IFormFile[]? file)
        {
            var toList = await _yetkiAppService.SendEmailForScheduledMaintenance(panoKimlikKullanicisiMi, kumesScadaKullanicisiMi, suHatScadaKullanicisiMi);
            _emailSender.SendScheduledMaintenanceMessage(emailKonu, emailGovde, new List<string>() { "beray.bayazit@matli.com.tr" }, file);
            var result = true;
            return Ok(result);
        }

        private static string AesEncrypt(string plainText, string key)
        {
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[16]; // 16 byte boş IV

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs)) sw.Write(plainText);

            return Convert.ToBase64String(ms.ToArray());
        }
    }
}