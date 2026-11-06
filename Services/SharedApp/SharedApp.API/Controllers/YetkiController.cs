using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Infrastructure.Sender;
using System.Security.Claims;
using System.Text.Json;
using static MatliOtomasyon.Shared.Application.ViewModels.UserPermissionClaimViewModel;
using MatliOtomasyon.Shared.Application.Abstractions;
using MatliOtomasyon.Shared.Web.Http;

namespace SharedApp.API.Controllers
{
    /// <summary>
    /// Menü (top bar / side bar) tanımlarını ve oturumdaki kullanıcının form, link ve modül bazlı yetkilerini sunar.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class YetkiController : ControllerBase
    {
        private readonly IYetkiAppService _yetkiAppService;
        private readonly IUserExtension _userExtension;
        private readonly EmailSender _emailSender;
        private readonly IResponseEncryptor _responseEncryptor;

        /// <summary>
        /// Yetki uygulama servisini, oturumdaki kullanıcıya erişimi, e-posta göndericiyi ve cevap kodlayıcıyı alır.
        /// </summary>
        public YetkiController(IYetkiAppService yetkiAppService, IUserExtension userExtension, EmailSender emailSender, IResponseEncryptor responseEncryptor)
        {
            _yetkiAppService = yetkiAppService;
            _userExtension = userExtension;
            _emailSender = emailSender;
            _responseEncryptor = responseEncryptor;
        }

        /// <summary>
        /// Üst menü tanımlarını frontend'in beklediği kodlanmış biçimde döner. Giriş ekranında da gerektiği için anonimdir.
        /// </summary>
        [HttpGet("GetAllTopBar")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTopBar()
        {
            var result = await _yetkiAppService.GetAllTopBar();
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Yan menü tanımlarını frontend'in beklediği kodlanmış biçimde döner. Giriş ekranında da gerektiği için anonimdir.
        /// </summary>
        [HttpGet("GetAllSideBar")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllSideBar()
        {
            var result = await _yetkiAppService.GetAllSideBar();
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Oturumdaki kullanıcının verilen form linkine ait yetkisini (Okuma/Yazma) döner; yetki yoksa boş cevap döner.
        /// Authentication ve MobileUpdate API'leri yetki kontrolü için bu ucu kullanır.
        /// </summary>
        [HttpGet("GetUserFormYetkiByUserIdAndFormLink")]
        public async Task<IActionResult> GetUserFormYetkiByUserIdAndFormLink(string formLink)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _yetkiAppService.GetUserFormYetkiByUserIdAndFormLink(userId, formLink);
            return Ok(result);
        }

        /// <summary>
        /// Oturumdaki kullanıcının verilen lokasyondaki enerji izleme yetkisini döner.
        /// </summary>
        [HttpGet("GetvwUserEnerjiIzlemeYetkiByUserIdAndLokasyonId")]
        public async Task<IActionResult> GetvwUserEnerjiIzlemeYetkiByUserIdAndLokasyonId(int lokasyonId)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (userId == null) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _yetkiAppService.GetvwUserEnerjiIzlemeYetkiByUserIdAndLokasyonId(userId, lokasyonId);
            return Ok(result);
        }

        /// <summary>
        /// Oturumdaki kullanıcının yetkili olduğu bütün formları döner.
        /// </summary>
        [HttpGet("GetUserFormYetkiListByUserId")]
        [Authorize]
        public async Task<IActionResult> GetUserFormYetkiListByUserId()
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _yetkiAppService.GetUserFormYetkiListByUserId(userId);
            return Ok(result);
        }

        /// <summary>
        /// Oturumdaki kullanıcının modül bazlı yetki detaylarını (pano kimliklendirme, kümes, su hattı, enerji, topoloji,
        /// ticket/yönetim) tek modelde toplar; sadece kullanıcının yetkili olduğu formlar dahil edilir.
        /// Frontend menü ve sayfa erişimini bu cevaba göre belirler.
        /// </summary>
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

        /// <summary>
        /// Oturumdaki kullanıcının verilen linke ait yetkisini döner.
        /// </summary>
        [HttpGet("GetUserLinkYetkiByLink")]
        [Authorize]
        public async Task<IActionResult> GetUserLinkYetkiByLink(string link)
        {
            var userId = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _yetkiAppService.GetUserLinkYetkiByUserIdAndLink(userId, link);
            return Ok(result);
        }

        /// <summary>
        /// Planlı bakım duyurusu e-postası gönderir. Alıcı listesi seçilen kullanıcı gruplarına göre hesaplanır,
        /// ancak e-posta şu an sabit tek bir adrese gidiyor.
        /// </summary>
        [Authorize]
        [HttpPut("SendEmailForScheduledMaintenance")]
        public async Task<IActionResult> SendEmailForScheduledMaintenance([FromForm] string emailKonu, [FromForm] string emailGovde, [FromForm] bool panoKimlikKullanicisiMi, [FromForm] bool kumesScadaKullanicisiMi, [FromForm] bool suHatScadaKullanicisiMi, [FromForm] IFormFile[]? file)
        {
            var toList = await _yetkiAppService.SendEmailForScheduledMaintenance(panoKimlikKullanicisiMi, kumesScadaKullanicisiMi, suHatScadaKullanicisiMi);
            _emailSender.SendScheduledMaintenanceMessage(emailKonu, emailGovde, new List<string>() { "beray.bayazit@matli.com.tr" }, file);
            var result = true;
            return Ok(result);
        }
    }
}