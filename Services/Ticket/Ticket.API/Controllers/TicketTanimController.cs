using DevExtreme.AspNet.Mvc;
using MatliOtomasyon.Shared.Application.Abstractions;
using MatliOtomasyon.Shared.Web.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using Ticket.Application.Interfaces;
using Ticket.Application.ViewModels;

namespace Ticket.API.Controllers
{
    /// <summary>
    /// Ticket (tedarikçi talep) süreçlerinin uçlarını sunar: ticket açma, takipçi/sorumlu güncellemeleri, teklif onayı, durum ve fiyat geçmişi, ek dosyalar.
    /// Liste cevaplarının çoğu frontend'in beklediği kodlanmış biçimde döner.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class TicketTanimController : BaseApiController<TicketTanimViewModel>
    {
        private readonly ITicketTanimAppService _ticketTanimAppService;
        private readonly IUserExtension _userExtension;
        private readonly IResponseEncryptor _responseEncryptor;

        /// <summary>
        /// Ticket uygulama servisini, oturumdaki kullanıcıya erişimi ve frontend'e giden cevapları kodlayan servisi alır.
        /// </summary>
        public TicketTanimController(ITicketTanimAppService ticketTanimAppService, IUserExtension userExtension, IResponseEncryptor responseEncryptor) : base(ticketTanimAppService)
        {
            _ticketTanimAppService = ticketTanimAppService;
            _userExtension = userExtension;
            _responseEncryptor = responseEncryptor;
        }

        /// <summary>
        /// Oturumdaki kullanıcının Id'sini, doğrulanmış token'ın claim'lerinden döner. Token doğrulanmamışsa boş döner;
        /// imzası doğrulanmamış bir token'ın içeriği asla kullanılmaz.
        /// </summary>
        private string GetUserIdFromContext()
        {
            return _userExtension.GetClaimsIdentity()?.FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Oturumdaki kullanıcının erişebildiği ticket'ı detaylarıyla, frontend'in beklediği kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetTicketById")]
        public async Task<IActionResult> GetTicketById(int ticketTanimId)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _ticketTanimAppService.GetTicketById(ticketTanimId, userId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Oturumdaki kullanıcının takipçi olarak kısıtlı yetkili olduğu kayıtları döner.
        /// </summary>
        [HttpGet("GetTakipciUserKisitliYetki")]
        public async Task<IActionResult> GetTakipciUserKisitliYetki()
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _ticketTanimAppService.GetTakipciUserKisitliYetki(userId);
            return Ok(result);
        }

        /// <summary>
        /// Oturumdaki kullanıcının takipçi olarak yetkisiz olduğu kayıtları döner.
        /// </summary>
        [HttpGet("GetTakipciUserYetkisiz")]
        public async Task<IActionResult> GetTakipciUserYetkisiz()
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _ticketTanimAppService.GetTakipciUserYetkisiz(userId);
            return Ok(result);
        }

        /// <summary>
        /// Oturumdaki kullanıcının ticket sorumlusu olup olmadığını kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetUserSorumluMu")]
        public async Task<IActionResult> GetUserSorumluMu()
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _ticketTanimAppService.GetUserSorumluMu(userId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Ticket'a sorumluların yazdığı açıklamaları kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllSorumluAciklamalarByTicketId")]
        public async Task<IActionResult> GetAllSorumluAciklamalarByTicketId(int ticketTanimId)
        {
            var result = await _ticketTanimAppService.GetAllSorumluAciklamalarByTicketId(ticketTanimId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Ticket'a takipçilerin yazdığı açıklamaları kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllTakipciAciklamalarByTicketId")]
        public async Task<IActionResult> GetAllTakipciAciklamalarByTicketId(int ticketTanimId)
        {
            var result = await _ticketTanimAppService.GetAllTakipciAciklamalarByTicketId(ticketTanimId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Oturumdaki kullanıcının tarih aralığındaki ticket'larını kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllFilteredTicketTanims")]
        public async Task<IActionResult> GetAllFilteredTicketTanims(DateTime baslangicZamani, DateTime bitisZamani)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _ticketTanimAppService.GetAllFilteredTicketTanims(baslangicZamani, bitisZamani, userId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Oturumdaki kullanıcının erişebildiği bütün ticket'ları kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllTicketTanims")]
        public async Task<IActionResult> GetAllTicketTanims()
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _ticketTanimAppService.GetAllTicketTanims(userId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Ticket listesini DevExtreme grid'inin sayfalama/filtre seçenekleriyle, kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllTicketTanimsDx")]
        public async Task<IActionResult> GetAllTicketTanimsDx(DataSourceLoadOptions loadOptions, DateTime? baslangicZamani, DateTime? bitisZamani)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _ticketTanimAppService.GetAllTicketTanimsDx(loadOptions, baslangicZamani, bitisZamani, userId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Ticket açılırken seçilebilecek tedarikçileri kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllTedarikciForTicketTanim")]
        public async Task<IActionResult> GetAllTedarikciForTicketTanim()
        {
            var result = await _ticketTanimAppService.GetAllTedarikciForTicketTanim();
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Ticket durum tanımlarını kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllTicketDurums")]
        public async Task<IActionResult> GetAllTicketDurums()
        {
            var result = await _ticketTanimAppService.GetAllTicketDurums();
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Ticket'ın durum değişiklik geçmişini kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllvwTicketDurumLogs")]
        public async Task<IActionResult> GetAllvwTicketDurumLogs(int ticketTanimId)
        {
            var result = await _ticketTanimAppService.GetAllvwTicketDurumLogs(ticketTanimId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Ticket'ın teklif fiyatı geçmişini kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllvwTicketTeklifFiyatLogs")]
        public async Task<IActionResult> GetAllvwTicketTeklifFiyatLogs(int ticketTanimId)
        {
            var result = await _ticketTanimAppService.GetAllvwTicketTeklifFiyatLogs(ticketTanimId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }
        /// <summary>
        /// Teklif fiyatında kullanılabilecek para birimlerini kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllParaBirimis")]
        public async Task<IActionResult> GetAllParaBirimis()
        {
            var result = await _ticketTanimAppService.GetAllParaBirimis();
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Tedarikçi kullanıcılarını kodlanmış biçimde döner.
        /// </summary>
        [HttpGet("GetAllvwTedarikciUser")]
        public async Task<IActionResult> GetAllvwTedarikciUser()
        {
            var result = await _ticketTanimAppService.GetAllvwTedarikciUser();
            var json = JsonSerializer.Serialize(result);
            var encrypted = _responseEncryptor.Encrypt(json);
            return Ok(encrypted);
        }

        /// <summary>
        /// Oturumdaki kullanıcı adına yeni ticket açar; ekleri ticket klasörüne kaydeder.
        /// </summary>
        [HttpPost("AddTicketTanim")]
        public async Task<IActionResult> AddTicketTanim([FromForm] int formId, [FromForm] string kisaTanim, [FromForm] int tedarikciId, [FromForm] int lokasyonId, [FromForm] List<string> secilenMatliTakipciler, [FromForm] string tanim, [FromForm] IFormFile[]? files)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _ticketTanimAppService.AddTicketTanim(formId, kisaTanim, tedarikciId, lokasyonId, secilenMatliTakipciler, tanim, userId, files);
            return Ok();
        }

        /// <summary>
        /// Oturumdaki kullanıcı adına yeni ticket açar ve oluşan kaydı döner; ekleri ticket klasörüne kaydeder.
        /// </summary>
        [HttpPost("AddTicketTanim2")]
        public async Task<IActionResult> AddTicketTanim2([FromForm] int formId, [FromForm] string kisaTanim, [FromForm] int tedarikciId, [FromForm] int lokasyonId, [FromForm] List<string> secilenMatliTakipciler, [FromForm] string tanim, [FromForm] IFormFile[]? files)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _ticketTanimAppService.AddTicketTanim2(formId, kisaTanim, tedarikciId, lokasyonId, secilenMatliTakipciler, tanim, userId, files);
            return Ok(result);
        }

        /// <summary>
        /// Ticket'ı takipçi olarak günceller: tanım, durum, takipçiler, teklif sabitleme, fatura bilgisi ve ekler.
        /// </summary>
        [HttpPost("EditTicketByTakipci")]
        public async Task<IActionResult> EditTicketByTakipci([FromForm] int ticketTanimId, [FromForm] int formId, [FromForm] string kisaTanim, [FromForm] int durumId, [FromForm] List<string> secilenMatliTakipciler, [FromForm] string tanim, [FromForm] bool teklifSabitle, [FromForm] string? sasNo, [FromForm] string? aciklama, [FromForm] bool? faturaKesildi, [FromForm] IFormFile[]? files)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var name = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.Name)?.Value ?? string.Empty;
            var surname = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.Surname)?.Value ?? string.Empty;
            await _ticketTanimAppService.EditTicketByTakipci(ticketTanimId, formId, kisaTanim, durumId, secilenMatliTakipciler, tanim, teklifSabitle, sasNo, aciklama, faturaKesildi, userId, files, name + " " + surname);
            return Ok();
        }

        /// <summary>
        /// Ticket'ı sorumlu olarak günceller: sorumlular, teklif fiyatı, para birimi, açıklama ve durum.
        /// </summary>
        [HttpPost("EditTicketBySorumlu")]
        public async Task<IActionResult> EditTicketBySorumlu([FromForm] int ticketTanimId, [FromForm] List<int> secilenSorumlular, [FromForm] decimal? teklifFiyat, [FromForm] int? paraBirimiId, [FromForm] string? aciklama, [FromForm] int durumId)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var name = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.Name)?.Value ?? string.Empty;
            var surname = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.Surname)?.Value ?? string.Empty;
            await _ticketTanimAppService.EditTicketBySorumlu(ticketTanimId, secilenSorumlular, aciklama, durumId, userId, teklifFiyat, paraBirimiId, name + " " + surname);
            return Ok();
        }

        /// <summary>
        /// Ticket teklifini onaylar veya reddeder ve açıklamayı kaydeder.
        /// </summary>
        [HttpPut("TeklifDurumDegistir")]
        public async Task<IActionResult> TeklifDurumDegistir(int ticketTanimId, byte teklifDurumId, string? aciklama)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var name = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.Name)?.Value ?? string.Empty;
            var surname = _userExtension.GetClaimsIdentity().FirstOrDefault(s => s.Type == ClaimTypes.Surname)?.Value ?? string.Empty;
            var result = await _ticketTanimAppService.TeklifDurumDegistir(ticketTanimId, teklifDurumId, aciklama, userId, name + " " + surname);
            return Ok(result);
        }

        /// <summary>
        /// Ticket'a ait yüklenmiş dosyaların listesini döner.
        /// </summary>
        [HttpGet("GetAllFilesByTicketId")]
        public async Task<IActionResult> GetAllFilesByTicketId(int ticketTanimId)
        {
            var result = await _ticketTanimAppService.GetAllFilesByTicketId(ticketTanimId);

            return Ok(result);
        }

        /// <summary>
        /// Ticket klasöründeki bir dosyayı indirir. Ticket klasörü dışındaki yollar reddedilir.
        /// </summary>
        [HttpGet("GetDocument")]
        public async Task<IActionResult> GetDocument(string fileName, string filePath)
        {
            var result = await _ticketTanimAppService.GetDocument(fileName, filePath);

            return File(result.file, result.contentType, result.file.Name.Split("\\").Last());
        }

        /// <summary>
        /// Ticket klasöründeki bir dosyayı siler. Ticket klasörü dışındaki yollar reddedilir.
        /// </summary>
        [HttpPut("DeleteTicketFile")]
        public async Task<IActionResult> DeleteTicketFile(string filePath)
        {
            _ticketTanimAppService.DeleteTicketFile(filePath);
            return Ok();
        }
    }
}