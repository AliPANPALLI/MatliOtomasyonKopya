using DevExtreme.AspNet.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Extensions;
using System.Security.Claims;
using System.Text.Json;
using System.Security.Cryptography;
using System.Linq;
using System.Text;
using System.Text.Json;
using Ticket.Application.Interfaces;
using Ticket.Application.ViewModels;

namespace Ticket.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TicketTanimController : BaseApiController<TicketTanimViewModel>
    {
        private readonly ITicketTanimAppService _ticketTanimAppService;
        private readonly IUserExtension _userExtension;

        public TicketTanimController(ITicketTanimAppService ticketTanimAppService, IUserExtension userExtension) : base(ticketTanimAppService)
        {
            _ticketTanimAppService = ticketTanimAppService;
            _userExtension = userExtension;
        }

        private string GetUserIdFromContext()
        {
            // Try normal claims principal first
            var claim = _userExtension.GetClaimsIdentity()?.FirstOrDefault(s => s.Type == ClaimTypes.NameIdentifier || s.Type == "sub" || s.Type == "id" || s.Type == "userId" || s.Type == "nameid")?.Value;
            if (!string.IsNullOrEmpty(claim)) return claim;

            // Fallback: try to read Authorization header and decode token without validation
            try
            {
                var auth = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(auth))
                {
                    var token = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? auth.Substring("Bearer ".Length).Trim() : auth;
                    if (!string.IsNullOrEmpty(token))
                    {
                        // Manual payload decode to avoid dependency on Jwt libs at runtime
                        var parts = token.Split('.');
                        if (parts.Length >= 2)
                        {
                            var payload = parts[1];
                            // Base64Url decode
                            payload = payload.Replace('-', '+').Replace('_', '/');
                            switch (payload.Length % 4)
                            {
                                case 2: payload += "=="; break;
                                case 3: payload += "="; break;
                            }
                            var bytes = Convert.FromBase64String(payload);
                            var json = System.Text.Encoding.UTF8.GetString(bytes);
                            using var doc = JsonDocument.Parse(json);
                            var root = doc.RootElement;
                            string? val = null;
                            if (root.TryGetProperty(ClaimTypes.NameIdentifier, out var p)) val = p.GetString();
                            if (string.IsNullOrEmpty(val) && root.TryGetProperty("sub", out p)) val = p.GetString();
                            if (string.IsNullOrEmpty(val) && root.TryGetProperty("nameid", out p)) val = p.GetString();
                            if (string.IsNullOrEmpty(val) && root.TryGetProperty("id", out p)) val = p.GetString();
                            if (string.IsNullOrEmpty(val) && root.TryGetProperty("userId", out p)) val = p.GetString();
                            if (!string.IsNullOrEmpty(val)) return val;
                        }
                    }
                }
            }
            catch { }

            return string.Empty;
        }

        [HttpGet("GetTicketById")]
        public async Task<IActionResult> GetTicketById(int ticketTanimId)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _ticketTanimAppService.GetTicketById(ticketTanimId, userId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetTakipciUserKisitliYetki")]
        public async Task<IActionResult> GetTakipciUserKisitliYetki()
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _ticketTanimAppService.GetTakipciUserKisitliYetki(userId);
            return Ok(result);
        }

        [HttpGet("GetTakipciUserYetkisiz")]
        public async Task<IActionResult> GetTakipciUserYetkisiz()
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _ticketTanimAppService.GetTakipciUserYetkisiz(userId);
            return Ok(result);
        }

        [HttpGet("GetUserSorumluMu")]
        public async Task<IActionResult> GetUserSorumluMu()
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }

            var result = await _ticketTanimAppService.GetUserSorumluMu(userId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllSorumluAciklamalarByTicketId")]
        public async Task<IActionResult> GetAllSorumluAciklamalarByTicketId(int ticketTanimId)
        {
            var result = await _ticketTanimAppService.GetAllSorumluAciklamalarByTicketId(ticketTanimId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllTakipciAciklamalarByTicketId")]
        public async Task<IActionResult> GetAllTakipciAciklamalarByTicketId(int ticketTanimId)
        {
            var result = await _ticketTanimAppService.GetAllTakipciAciklamalarByTicketId(ticketTanimId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllFilteredTicketTanims")]
        public async Task<IActionResult> GetAllFilteredTicketTanims(DateTime baslangicZamani, DateTime bitisZamani)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _ticketTanimAppService.GetAllFilteredTicketTanims(baslangicZamani, bitisZamani, userId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllTicketTanims")]
        public async Task<IActionResult> GetAllTicketTanims()
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _ticketTanimAppService.GetAllTicketTanims(userId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllTicketTanimsDx")]
        public async Task<IActionResult> GetAllTicketTanimsDx(DataSourceLoadOptions loadOptions, DateTime? baslangicZamani, DateTime? bitisZamani)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _ticketTanimAppService.GetAllTicketTanimsDx(loadOptions, baslangicZamani, bitisZamani, userId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllTedarikciForTicketTanim")]
        public async Task<IActionResult> GetAllTedarikciForTicketTanim()
        {
            var result = await _ticketTanimAppService.GetAllTedarikciForTicketTanim();
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllTicketDurums")]
        public async Task<IActionResult> GetAllTicketDurums()
        {
            var result = await _ticketTanimAppService.GetAllTicketDurums();
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllvwTicketDurumLogs")]
        public async Task<IActionResult> GetAllvwTicketDurumLogs(int ticketTanimId)
        {
            var result = await _ticketTanimAppService.GetAllvwTicketDurumLogs(ticketTanimId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllvwTicketTeklifFiyatLogs")]
        public async Task<IActionResult> GetAllvwTicketTeklifFiyatLogs(int ticketTanimId)
        {
            var result = await _ticketTanimAppService.GetAllvwTicketTeklifFiyatLogs(ticketTanimId);
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        //[HttpGet("debug-claims")]
        //public IActionResult DebugClaims()
        //{
        //    var headers = HttpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        //    var authenticated = HttpContext.User?.Identity?.IsAuthenticated ?? false;
        //    var claimEnumerable = _userExtension.GetClaimsIdentity();
        //    List<object> claims;
        //    if (claimEnumerable != null)
        //        claims = claimEnumerable.Select(c => (object)new { c.Type, c.Value }).ToList();
        //    else
        //        claims = new List<object>();

        //    return Ok(new { headers, authenticated, claims });
        //}

        [HttpGet("GetAllParaBirimis")]
        public async Task<IActionResult> GetAllParaBirimis()
        {
            var result = await _ticketTanimAppService.GetAllParaBirimis();
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpGet("GetAllvwTedarikciUser")]
        public async Task<IActionResult> GetAllvwTedarikciUser()
        {
            var result = await _ticketTanimAppService.GetAllvwTedarikciUser();
            var json = JsonSerializer.Serialize(result);
            var encrypted = AesEncrypt(json, "mysecuritykeymysecuritykey123456");
            return Ok(encrypted);
        }

        [HttpPost("AddTicketTanim")]
        public async Task<IActionResult> AddTicketTanim([FromForm] int formId, [FromForm] string kisaTanim, [FromForm] int tedarikciId, [FromForm] int lokasyonId, [FromForm] List<string> secilenMatliTakipciler, [FromForm] string tanim, [FromForm] IFormFile[]? files)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            await _ticketTanimAppService.AddTicketTanim(formId, kisaTanim, tedarikciId, lokasyonId, secilenMatliTakipciler, tanim, userId, files);
            return Ok();
        }

        [HttpPost("AddTicketTanim2")]
        public async Task<IActionResult> AddTicketTanim2([FromForm] int formId, [FromForm] string kisaTanim, [FromForm] int tedarikciId, [FromForm] int lokasyonId, [FromForm] List<string> secilenMatliTakipciler, [FromForm] string tanim, [FromForm] IFormFile[]? files)
        {
            var userId = GetUserIdFromContext();
            if (string.IsNullOrEmpty(userId)) { throw new Exception("Kullanıcı bulunamadı!"); }
            var result = await _ticketTanimAppService.AddTicketTanim2(formId, kisaTanim, tedarikciId, lokasyonId, secilenMatliTakipciler, tanim, userId, files);
            return Ok(result);
        }

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

        [HttpGet("GetAllFilesByTicketId")]
        public async Task<IActionResult> GetAllFilesByTicketId(int ticketTanimId)
        {
            var result = await _ticketTanimAppService.GetAllFilesByTicketId(ticketTanimId);

            return Ok(result);
        }

        [HttpGet("GetDocument")]
        public async Task<IActionResult> GetDocument(string fileName, string filePath)
        {
            var result = await _ticketTanimAppService.GetDocument(fileName, filePath);

            return File(result.file, result.contentType, result.file.Name.Split("\\").Last());
        }

        [HttpPut("DeleteTicketFile")]
        public async Task<IActionResult> DeleteTicketFile(string filePath)
        {
            _ticketTanimAppService.DeleteTicketFile(filePath);
            return Ok();
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