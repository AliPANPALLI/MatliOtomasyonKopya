using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using MobileUpdate.Infrastructure.Depolama;
using System.Net.Http.Headers;
using System.Text.Json;
using static MatliOtomasyon.Shared.Infrastructure.Configuration.ApiUrls;

namespace MobileUpdate.API.Yetki
{
    public enum FormYetkiTuru
    {
        /// <summary>Kullanıcının forma ait UserFormYetki kaydı olması yeterli (sayfayı görebilen herkes).</summary>
        Okuma,

        /// <summary>Kayıtta Yazma işaretli olmalı.</summary>
        Yazma
    }

    /// <summary>
    /// Admin endpoint'lerini SharedApp'teki form yetkisine bağlar. Yeni bir yetki tablosu veya kuralı
    /// yoktur: "Mobil Sürüm Yönetimi" sayfası SharedApp'te bir Form olarak tanımlanır, kullanıcıya
    /// UserFormYetki ile Okuma/Yazma verilir. Bu filtre aynı kaydı SharedApp'in
    /// Yetki/GetUserFormYetkiByUserIdAndFormLink endpoint'inden, isteği yapan kullanıcının token'ıyla okur.
    /// Yetki yoksa 403 döner.
    /// </summary>
    public class FormYetkiAttribute : TypeFilterAttribute
    {
        public FormYetkiAttribute(FormYetkiTuru yetkiTuru) : base(typeof(FormYetkiFilter))
        {
            Arguments = new object[] { yetkiTuru };
        }
    }

    public class FormYetkiFilter : IAsyncAuthorizationFilter
    {
        private readonly FormYetkiTuru _yetkiTuru;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MobilGuncellemeAyar _ayar;

        public FormYetkiFilter(FormYetkiTuru yetkiTuru, IHttpClientFactory httpClientFactory, IOptions<MobilGuncellemeAyar> ayar)
        {
            _yetkiTuru = yetkiTuru;
            _httpClientFactory = httpClientFactory;
            _ayar = ayar.Value;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.HttpContext.User.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            if (string.IsNullOrWhiteSpace(_ayar.FormLink))
                throw new InvalidOperationException("MobilGuncelleme:FormLink ayarı tanımlı değil.");

            var yetki = await UserFormYetkiGetirAsync(context.HttpContext);

            // Frontend ile aynı kural: forma ait kayıt varsa sayfa görüntülenir (general.guard),
            // düzenleme için kayıtta Yazma işaretli olmalı (isEditable = yetki.yazma).
            var yetkili = _yetkiTuru == FormYetkiTuru.Yazma ? yetki?.Yazma == true : yetki is not null;
            if (!yetkili)
                context.Result = new ForbidResult();
        }

        private async Task<UserFormYetkiSonuc?> UserFormYetkiGetirAsync(HttpContext httpContext)
        {
            var uri = $"{SharedAppBaseUrl}{SharedAppUrls.GetUserFormYetkiByUserIdAndFormLink}?formLink={Uri.EscapeDataString(_ayar.FormLink)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);

            // SharedApp kullanıcıyı token'daki NameIdentifier'dan bulduğu için gelen token aynen iletilir.
            var authorization = httpContext.Request.Headers.Authorization.ToString();
            if (AuthenticationHeaderValue.TryParse(authorization, out var header))
                request.Headers.Authorization = header;

            using var response = await _httpClientFactory.CreateClient().SendAsync(request, httpContext.RequestAborted);
            response.EnsureSuccessStatusCode();

            // Kullanıcının bu forma yetkisi yoksa SharedApp boş gövde (204) döner.
            var icerik = await response.Content.ReadAsStringAsync(httpContext.RequestAborted);
            if (string.IsNullOrWhiteSpace(icerik))
                return null;

            return JsonSerializer.Deserialize<UserFormYetkiSonuc>(icerik, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        /// <summary>SharedApp vwUserFormYetki cevabından kullanılan alan.</summary>
        private class UserFormYetkiSonuc
        {
            public bool? Yazma { get; set; }
        }
    }
}
