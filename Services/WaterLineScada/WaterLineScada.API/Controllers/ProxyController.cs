using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using WaterLineScada.Application.Interfaces;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WaterLineScada.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProxyController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        private readonly ICizimAppService _cizimAppService;
        private readonly IHttpClientFactory _httpFactory;
        private readonly TimeSpan _ttl = TimeSpan.FromMinutes(5); // id geçerlilik süresi

        public ProxyController(IMemoryCache cache, IHttpClientFactory httpFactory, ICizimAppService cizimAppService)
        {
            _cache = cache;
            _httpFactory = httpFactory;
            _cizimAppService = cizimAppService;
        }

        // 1) Frontend bununla gerçek url gönderir, sunucu bir id döner
        [HttpPut("create")]
        public async Task<IActionResult> Create(int cizimId)
        {
            var url = await _cizimAppService.GetCizimById(cizimId);
            if (string.IsNullOrWhiteSpace(url.CizimUrl)) return BadRequest("url gerekli");

            var id = Guid.NewGuid().ToString("N");
            _cache.Set(id, url.CizimUrl, _ttl);
            return Ok(new { id });
        }

        // 2) iframe src olarak bu endpoint kullanılır: /embed/view/{id}
        [HttpGet("embed/view/{id}")]
        public async Task<IActionResult> View(string id)
        {
            // Referer kontrolü (opsiyonel ama tavsiye edilir)
            var referer = Request.Headers["Referer"].FirstOrDefault() ?? "";
            if (!referer.Contains(Request.Host.Value) && !string.IsNullOrEmpty(referer))
            {
                // Eğer referer yoksa (tarayıcı doğrudan açtıysa) reddet
                return Forbid();
            }

            if (!_cache.TryGetValue<string>(id, out var targetUrl))
                return NotFound();

            // İsteği yap
            try
            {
                var client = _httpFactory.CreateClient();
                var resp = await client.GetAsync(targetUrl);
                if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode);

                var html = await resp.Content.ReadAsStringAsync();
                var contentType = resp.Content.Headers.ContentType?.MediaType ?? "text/html";

                // Optional: HTML içindeki absolute/relative asset linklerini proxy'le veya yeniden yaz
                html = RewriteHtmlAssets(html);

                return Content(html, contentType, Encoding.UTF8);
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, ex.Message);
            }




            catch (Exception ex)
            {
                return StatusCode(502, ex.Message);
            }
        }

        // Basit asset rewrite: <img src="/img.png"> veya <link href="/css"> veya <script src=...>
        private string RewriteHtmlAssets(string html)
        {
            // 1) base tag ekle (göreli url'leri düzgün çözmek için hedef base kullanabilirsin)
            // Burada basitçe tüm src/href absolute değilse targetHost ile birleştirilmesi gerekir.
            // Daha güvenli/sağlam çözüm için HTML parser (AngleSharp) kullan.
            // Örnek : img, script, link src/href -> /embed/asset?url={encoded}
            string pattern = "(?:src|href)=[\"'](?<u>[^\"']+)[\"']";
            return Regex.Replace(html, pattern, m =>
            {
                var u = m.Groups["u"].Value;
                // Eğer u zaten absolute (http) ise proxy'le veya bırak
                if (u.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    var enc = WebUtility.UrlEncode(u);
                    return m.Value.Replace(u, $"/embed/asset?url={enc}");
                }
                else
                {
                    // göreli linkleri de proxy'le (kendi mantığına göre hedef URL ile birleştir)
                    var enc = WebUtility.UrlEncode(u);
                    return m.Value.Replace(u, $"/embed/asset?rel={enc}");
                }
            }, RegexOptions.IgnoreCase);
        }

        // Asset proxy endpoint (örnek)
        [HttpGet("/embed/asset")]
        public async Task<IActionResult> Asset([FromQuery] string url, [FromQuery] string rel)
        {
            string target = null;
            if (!string.IsNullOrEmpty(url)) target = WebUtility.UrlDecode(url);
            else if (!string.IsNullOrEmpty(rel))
            {
                // rel handling: basitçe reddet ya da implement et
                return NotFound();
            }
            if (string.IsNullOrEmpty(target)) return BadRequest();

            try
            {
                var c = _httpFactory.CreateClient();
                var r = await c.GetAsync(target);
                var bytes = await r.Content.ReadAsByteArrayAsync();
                var contentType = r.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                return File(bytes, contentType);
            }
            catch
            {
                return NotFound();
            }
        }

        //[HttpGet("embedProxy")]
        //public async Task<IActionResult> embedProxy([FromQuery] int cizimId)
        //{
        //    var cizim = await _cizimAppService.GetCizimUrlById(cizimId);
        //    // 1️⃣ URL doğrulama (güvenlik açısından önemli)
        //    if (string.IsNullOrWhiteSpace(cizim))
        //        return BadRequest("URL parametresi gerekli.");

        //    // (Opsiyonel) sadece belirli domainlere izin ver
        //    // var allowedHosts = new[] { "dis-site.com", "example.com" };
        //    // var uri = new Uri(url);
        //    // if (!allowedHosts.Contains(uri.Host))
        //    //     return BadRequest("Bu domain'e izin verilmiyor.");

        //    try
        //    {
        //        // 2️ Dış isteği gönder
        //        var response = await _httpClient.GetAsync(cizim);

        //        if (!response.IsSuccessStatusCode)
        //            return StatusCode((int)response.StatusCode, $"Uzak sunucu hatası: {response.StatusCode}");

        //        // 3️ İçeriği oku
        //        var content = await response.Content.ReadAsStringAsync();

        //        // 4️ Content-Type'ı belirle (HTML, CSS, JS vs olabilir)
        //        var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/html";

        //        // 5️ İçeriği doğrudan döndür (JSON değil!)
        //        return Content(content, contentType, Encoding.UTF8);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Proxy hatası: {ex.Message}");
        //    }
        //}

        //[HttpGet("embed")]
        //public async Task<IActionResult> Embed([FromQuery] int cizimId)
        //{
        //    var cizim = await _cizimAppService.GetCizimUrlById(cizimId);
        //    // 1️⃣ URL doğrulama (güvenlik açısından önemli)
        //    if (string.IsNullOrWhiteSpace(cizim))
        //        return BadRequest("URL parametresi gerekli.");

        //    // (Opsiyonel) sadece belirli domainlere izin ver
        //    // var allowedHosts = new[] { "dis-site.com", "example.com" };
        //    // var uri = new Uri(url);
        //    // if (!allowedHosts.Contains(uri.Host))
        //    //     return BadRequest("Bu domain'e izin verilmiyor.");

        //    try
        //    {
        //        // 1️⃣ Kendi handler'ımızı oluştur
        //        var handler = new HttpClientHandler
        //        {
        //            // Bu satır SSL hatalarını da yoksaymak istersen (isteğe bağlı)
        //            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        //        };

        //        using var client = new HttpClient(handler);
        //        client.DefaultRequestHeaders.Clear();

        //        // 2️⃣ İsteği gönder
        //        var response = await client.GetAsync(cizim, HttpCompletionOption.ResponseHeadersRead);

        //        // 3️⃣ Başarılıysa body’yi oku
        //        var content = await response.Content.ReadAsStringAsync();
        //        var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/html";

        //        // 4️⃣ HTML’i geri döndür
        //        return Content(content, contentType, Encoding.UTF8);
        //    }
        //    catch (HttpRequestException ex)
        //    {
        //        return StatusCode(502, $"Proxy isteği başarısız: {ex.Message}");
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, $"Genel hata: {ex.Message}");
        //    }
        //}
    }

    public class CreateEmbedRequest { public int cizimId { get; set; } }
}
