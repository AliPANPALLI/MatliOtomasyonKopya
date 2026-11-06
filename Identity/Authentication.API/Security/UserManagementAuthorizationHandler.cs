using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static MatliOtomasyon.Shared.Infrastructure.Configuration.ApiUrls;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

namespace Authentication.API.Security
{
    /// <summary>
    /// Kullanıcı yönetimi uçlarına erişim kararını verir. Kullanıcının SharedApp'teki "Kullanıcı Yönetimi"
    /// formuna ait yetkisini, isteği yapan kullanıcının kendi token'ıyla SharedApp'ten okur.
    /// Aynı kullanıcı için sonuç kısa süre bellekte tutulur. Şarta izin veriliyorsa geçerli bir
    /// iç servis anahtarıyla gelen istekleri de kabul eder.
    /// </summary>
    public sealed class UserManagementAuthorizationHandler : AuthorizationHandler<UserManagementRequirement>
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly UserManagementOptions _options;
        private readonly InternalServiceOptions _internalServiceOptions;
        private readonly ILogger<UserManagementAuthorizationHandler> _logger;

        /// <summary>
        /// Handler'ın ihtiyaç duyduğu servisleri alır.
        /// </summary>
        public UserManagementAuthorizationHandler(
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IOptions<UserManagementOptions> options,
            IOptions<InternalServiceOptions> internalServiceOptions,
            ILogger<UserManagementAuthorizationHandler> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _options = options.Value;
            _internalServiceOptions = internalServiceOptions.Value;
            _logger = logger;
        }

        /// <summary>
        /// Şartı değerlendirir. Önce iç servis anahtarına, sonra kullanıcının form yetkisine bakar.
        /// Hiçbiri sağlanmazsa şart başarısız kalır ve istek 401/403 ile reddedilir.
        /// </summary>
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, UserManagementRequirement requirement)
        {
            var httpContext = context.Resource as HttpContext ?? _httpContextAccessor.HttpContext;
            if (httpContext is null)
                return;

            if (requirement.AllowInternalService && HasValidInternalApiKey(httpContext))
            {
                context.Succeed(requirement);
                return;
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (context.User.Identity?.IsAuthenticated != true || string.IsNullOrEmpty(userId))
                return;

            if (string.IsNullOrWhiteSpace(_options.FormLink))
            {
                _logger.LogWarning("UserManagement:FormLink is not configured; user management endpoints are closed for {UserId}.", userId);
                return;
            }

            var permission = await GetFormPermissionAsync(httpContext, userId);
            var isAllowed = requirement.RequiresWrite
                ? permission == FormPermission.Write
                : permission is FormPermission.Read or FormPermission.Write;

            if (isAllowed)
                context.Succeed(requirement);
        }

        /// <summary>
        /// İstekte iç servis anahtarı varsa ve ayarlı anahtarla eşleşiyorsa true döner.
        /// Karşılaştırma, zamanlama saldırılarına karşı sabit sürede yapılır. Anahtar ayarlı değilse
        /// iç servis erişimi kapalıdır.
        /// </summary>
        private bool HasValidInternalApiKey(HttpContext httpContext)
        {
            var expectedKey = _internalServiceOptions.AuthenticationApiKey;
            if (string.IsNullOrWhiteSpace(expectedKey))
                return false;

            if (!httpContext.Request.Headers.TryGetValue(InternalServiceOptions.ApiKeyHeaderName, out var suppliedKeys) || suppliedKeys.Count != 1)
                return false;

            var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedKey));
            var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(suppliedKeys[0] ?? string.Empty));
            return CryptographicOperations.FixedTimeEquals(expectedHash, suppliedHash);
        }

        /// <summary>
        /// Kullanıcının kullanıcı yönetimi formundaki yetkisini döner. Sonuç
        /// <see cref="UserManagementOptions.PermissionCacheSeconds"/> süresince bellekte tutulur.
        /// SharedApp'e ulaşılamazsa sonuç cache'lenmez ve yetki yok sayılır.
        /// </summary>
        private async Task<FormPermission> GetFormPermissionAsync(HttpContext httpContext, string userId)
        {
            var cacheKey = $"user-management-permission:{userId}";
            if (_cache.TryGetValue(cacheKey, out FormPermission cachedPermission))
                return cachedPermission;

            var permission = await FetchFormPermissionAsync(httpContext);
            if (permission is null)
                return FormPermission.None;

            _cache.Set(cacheKey, permission.Value, TimeSpan.FromSeconds(_options.PermissionCacheSeconds));
            return permission.Value;
        }

        /// <summary>
        /// SharedApp'in form yetkisi ucunu, gelen isteğin Authorization header'ıyla çağırır.
        /// SharedApp kullanıcıyı token'daki NameIdentifier'dan bulduğu için token aynen iletilir.
        /// </summary>
        /// <returns>Kullanıcının yetkisi; SharedApp'e ulaşılamazsa null.</returns>
        private async Task<FormPermission?> FetchFormPermissionAsync(HttpContext httpContext)
        {
            var uri = $"{SharedAppBaseUrl}{SharedAppUrls.GetUserFormYetkiByUserIdAndFormLink}?formLink={Uri.EscapeDataString(_options.FormLink)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (AuthenticationHeaderValue.TryParse(httpContext.Request.Headers.Authorization.ToString(), out var authorizationHeader))
                request.Headers.Authorization = authorizationHeader;

            try
            {
                using var response = await _httpClientFactory.CreateClient().SendAsync(request, httpContext.RequestAborted);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("SharedApp form permission check failed with status {StatusCode}.", (int)response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync(httpContext.RequestAborted);
                if (string.IsNullOrWhiteSpace(content))
                    return FormPermission.None;

                var formPermission = JsonSerializer.Deserialize<FormPermissionResponse>(content, JsonOptions);
                if (formPermission is null)
                    return FormPermission.None;

                return formPermission.Yazma == true ? FormPermission.Write : FormPermission.Read;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "SharedApp form permission check could not reach SharedApp.");
                return null;
            }
        }

        /// <summary>Kullanıcının form üzerindeki yetki seviyesi.</summary>
        private enum FormPermission
        {
            None,
            Read,
            Write
        }

        /// <summary>
        /// SharedApp'in vwUserFormYetki cevabından kullanılan tek alan.
        /// Alan adı SharedApp'in mevcut JSON sözleşmesinden geliyor, Adım 1.10'da birlikte çevrilecek.
        /// </summary>
        private sealed class FormPermissionResponse
        {
            /// <summary>Kullanıcının formda yazma yetkisi olup olmadığı.</summary>
            public bool? Yazma { get; set; }
        }
    }
}
