using Authentication.API.Controllers;
using Authentication.API.Security;
using Authentication.Application.Interfaces;
using Authentication.Application.Services;
using Authentication.Application.ViewModels;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using MatliOtomasyon.Shared.Infrastructure.Configuration;
using MatliOtomasyon.Shared.Web.Http;

var passed = 0;
async Task Check(string name, Func<Task> test) { await test(); Console.WriteLine("PASS " + name); passed++; }
void Assert(bool condition, string message = "Assertion failed") { if (!condition) throw new Exception(message); }
using var cache = new MemoryCache(new MemoryCacheOptions());
var challenges = new AuthChallenges(cache);
await Check("challenge purpose, attempts and replay", () => {
    var id = challenges.Issue("reset", "u", "hash", "123456", TimeSpan.FromMinutes(1));
    Assert(!challenges.TryConsume("login", id, "123456", out _, out _));
    Assert(!challenges.TryConsume("reset", id, "000000", out _, out _));
    Assert(challenges.TryConsume("reset", id, "123456", out var userId, out var stamp));
    Assert(userId == "u" && stamp == AuthChallenges.Fingerprint("hash"));
    Assert(!challenges.TryConsume("reset", id, "123456", out _, out _));
    id = challenges.Issue("reset", "u", "hash", "123456", TimeSpan.FromMinutes(1));
    for (var i = 0; i < 3; i++) Assert(!challenges.TryConsume("reset", id, "000000", out _, out _));
    Assert(!challenges.TryConsume("reset", id, "123456", out _, out _));
    return Task.CompletedTask;
});
await Check("new challenge invalidates previous challenge", () => {
    var old = challenges.Issue("login", "u", "hash", "123456", TimeSpan.FromMinutes(1));
    var current = challenges.Issue("login", "u", "hash", "234567", TimeSpan.FromMinutes(1));
    Assert(!challenges.TryConsume("login", old, "123456", out _, out _));
    Assert(challenges.TryConsume("login", current, "234567", out _, out _));
    return Task.CompletedTask;
});
await Check("expired challenge rejected", async () => {
    var id = challenges.Issue("reset", "u", "hash", "123456", TimeSpan.FromMilliseconds(1));
    await Task.Delay(30);
    Assert(!challenges.TryConsume("reset", id, "123456", out _, out _));
});
await Check("concurrent challenge consumption has one winner", async () => {
    var id = challenges.Issue("reset", "u", "hash", "123456", TimeSpan.FromMinutes(1));
    var outcomes = await Task.WhenAll(Enumerable.Range(0, 32).Select(index => Task.Run(() => challenges.TryConsume("reset", id, "123456", out _, out _))));
    Assert(outcomes.Count(x => x) == 1);
});
await Check("account limit independent of casing", () => {
    Assert(challenges.Allow("test", "ALICE", 2, TimeSpan.FromMinutes(1)));
    Assert(challenges.Allow("test", "alice", 2, TimeSpan.FromMinutes(1)));
    Assert(!challenges.Allow("test", " Alice ", 2, TimeSpan.FromMinutes(1)));
    return Task.CompletedTask;
});
var options = Options.Create(new CustomTokenOption { SecurityKey = new string('k', 64), Issuer = "test-issuer", Audience = ["test-audience"], AccessTokenExpiration = 5 });
var user = new User { Id = "u", UserName = "alice", Name = "Alice", Surname = "Test", Email = "alice@example.invalid", PasswordHash = "hash", PhoneNumber = "5551234567" };
var calls = new List<string>();
var users = Stub<IUserService>((m, a) => m.Name switch {
    "GetUserByName" or "GetUserBySicilNo" => Task.FromResult((string)a![0]! == "alice" ? user : null!),
    "CheckPasswordAsync" => Task.FromResult(true),
    "AddConnectSiteLog" => Task.CompletedTask,
    "IncorrectLoginNumberUpdate" => Task.CompletedTask,
    "AddPageExitMain" => RecordPageExit(a!),
    _ => throw new Exception("Unexpected user service call: " + m.Name)
});
var repository = Stub<IUserRepository>((m,a) => m.Name switch {
    "GetUserById" => Task.FromResult((string)a![0]! == "u" ? user : null!),
    "TryResetPassword" => ResetPassword(a!),
    _ => throw new Exception("Unexpected user repository call: " + m.Name)
});
Task<bool> ResetPassword(object?[] a) { calls.Add("reset:" + a[0]); return Task.FromResult(true); }
/// <summary>Sekme kapanışı kaydını, kaydın hangi kullanıcı adına atıldığını doğrulamak için not eder.</summary>
Task RecordPageExit(object?[] a) { calls.Add("page-exit:" + a[0]); return Task.CompletedTask; }
var tokens = Stub<ITokenService>((m,a) => {
    calls.Add(m.Name + ":" + (a?[0] is User u ? u.Id : a?[0]));
    return m.Name switch {
        "CreateNewRefreshToken" or "CreateToken" => Task.FromResult(new TokenViewModel { AccessToken = "access", RefreshToken = "refresh" }),
        "RemoveToken" or "RemoveUserHeartBeat" => Task.CompletedTask,
        _ => throw new Exception("Unexpected token service call: " + m.Name)
    };
});
var otp = Stub<ITwoFactorService>((m,a) => {
    if(m.Name == "CheckOtpRequired") { calls.Add("otp-ip:" + a![1]); return Task.FromResult(false); }
    throw new Exception("Unexpected OTP service call: " + m.Name);
});
var context = new DefaultHttpContext();
context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.5");
var authorizationAllowed = false;
var authorizationService = Stub<IAuthorizationService>((m, a) =>
    Task.FromResult(authorizationAllowed ? AuthorizationResult.Success() : AuthorizationResult.Failed()));
var responseEncryptor = new ResponseEncryptor(Options.Create(new ResponseEncryptionOptions { Key = new string('r', 32) }));
var controller = new UserController(users, tokens, otp, null!, null!, new HttpContextAccessor { HttpContext = context }, challenges, options, repository, authorizationService, responseEncryptor) {
    ControllerContext = new ControllerContext { HttpContext = context }
};
string Jwt(string issuer = "test-issuer", string audience = "test-audience", string? key = null, bool unsigned = false, bool legacyAlgorithm = false) =>
    new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, audience,
        [new Claim(ClaimTypes.NameIdentifier, "u")], DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(-1),
        unsigned ? null : new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key ?? options.Value.SecurityKey)), legacyAlgorithm ? SecurityAlgorithms.HmacSha256Signature : SecurityAlgorithms.HmacSha256)));
await Check("client-supplied forwarded IP cannot disable OTP", async () => {
    calls.Clear();
    context.Request.Headers["X-Forwarded-For"] = "10.1.2.3";
    context.Request.Headers["X-Real-IP"] = "10.1.2.3";
    Assert(await controller.SignIn(new LoginViewModel { Name = "alice", Password = "password" }) is OkObjectResult);
    Assert(calls.Contains("otp-ip:unknown") && !calls.Contains("otp-ip:10.1.2.3"));
    calls.Clear();
    context.Request.Headers.Remove("X-Forwarded-For");
    context.Request.Headers.Remove("X-Real-IP");
    Assert(await controller.SignIn(new LoginViewModel { Name = "alice", Password = "password" }) is OkObjectResult);
    Assert(calls.Contains("otp-ip:203.0.113.5"));
});
await Check("logout requires authorization", () => {
    var method = typeof(UserController).GetMethod("SignOut", [typeof(SignOutViewModel)])!;
    Assert(method.GetCustomAttribute<AuthorizeAttribute>() != null);
    Assert(method.GetCustomAttribute<AllowAnonymousAttribute>() == null);
    return Task.CompletedTask;
});
await Check("logout derives identity from authenticated claims", async () => {
    calls.Clear();
    context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "owner")], "test"));
    Assert(await controller.SignOut(new SignOutViewModel()) is OkResult);
    Assert(calls.Contains("RemoveToken:owner") && !calls.Contains("RemoveToken:u"));
    context.User = new ClaimsPrincipal(new ClaimsIdentity());
    Assert(await controller.SignOut(new SignOutViewModel()) is UnauthorizedResult);
});
await Check("forged signature, issuer, audience and unsigned JWT rejected", async () => {
    foreach (var jwt in new[] { Jwt(key: new string('x',64)), Jwt(issuer: "wrong"), Jwt(audience: "wrong"), Jwt(unsigned:true), "garbage" }) {
        calls.Clear();
        Assert(await controller.NewToken(new NewTokenViewModel { JwtToken = jwt, RefreshToken = "refresh" }) is UnauthorizedObjectResult);
        Assert(calls.Count == 0);
    }
});
await Check("expired signed JWT may refresh; locked/deleted accounts cannot", async () => {
    Assert(await controller.NewToken(new NewTokenViewModel { JwtToken = Jwt(), RefreshToken = "refresh" }) is OkObjectResult);
    Assert(await controller.NewToken(new NewTokenViewModel { JwtToken = Jwt(legacyAlgorithm: true), RefreshToken = "refresh" }) is OkObjectResult);
    user.IsLocked = true;
    Assert(await controller.NewToken(new NewTokenViewModel { JwtToken = Jwt(), RefreshToken = "refresh" }) is UnauthorizedObjectResult);
    user.IsLocked = false; user.Silindi = true;
    Assert(await controller.NewToken(new NewTokenViewModel { JwtToken = Jwt(), RefreshToken = "refresh" }) is UnauthorizedObjectResult);
    user.Silindi = false;
});
await Check("reset requests do not change passwords or disclose account existence", async () => {
    calls.Clear();
    foreach (var name in new[] { "alice", "missing" }) {
        var result = await controller.ResetPassword(new ResetPasswordViewModel { UserName = name, Method = "email", Detail = "wrong@example.invalid" });
        Assert(result is OkObjectResult);
        var value = ((OkObjectResult)result).Value!;
        Assert(((string)value.GetType().GetProperty("ChallengeId")!.GetValue(value)!).Length == 64);
    }
    Assert(calls.Count == 0);
});
await Check("reset rejects invalid code and changed password; successful reset revokes session", async () => {
    const string password = "NewPassword123!";
    CompletePasswordResetViewModel Request(string id, string code) => new() { ChallengeId = id, Code = code, NewPassword = password, NewPasswordAgain = password };
    calls.Clear();
    var id = challenges.Issue("reset", user.Id, user.PasswordHash, "123456", TimeSpan.FromMinutes(1));
    Assert(await controller.CompletePasswordReset(Request(id, "000000")) is BadRequestObjectResult);
    Assert(calls.Count == 0);
    user.PasswordHash = "changed";
    Assert(await controller.CompletePasswordReset(Request(id, "123456")) is BadRequestObjectResult);
    Assert(calls.Count == 0);
    id = challenges.Issue("reset", user.Id, user.PasswordHash, "123456", TimeSpan.FromMinutes(1));
    Assert(await controller.CompletePasswordReset(Request(id, "123456")) is OkObjectResult);
    Assert(calls.Contains("reset:u") && calls.Contains("RemoveUserHeartBeat:u"));
    Assert(await controller.CompletePasswordReset(Request(id, "123456")) is BadRequestObjectResult);
});
await Check("OTP needs the password-verified challenge", async () => {
    Assert(await controller.CheckTwoFactorAuth(new OtpViewModel { ChallengeId = "", OtpCode = "123456" }) is UnauthorizedObjectResult);
    var id = challenges.Issue("login", user.Id, user.PasswordHash, "123456", TimeSpan.FromMinutes(1));
    user.IsLocked = true;
    Assert(await controller.CheckTwoFactorAuth(new OtpViewModel { ChallengeId = id, OtpCode = "123456" }) is UnauthorizedObjectResult);
    user.IsLocked = false;
});
await Check("refresh rotation invalidates replay and preserves absolute expiry", async () => {
    var originalExpiry = DateTime.Now.AddMinutes(20);
    var stored = new UserRefreshToken { UserId = "u", Code = "legacy-token", Expiration = originalExpiry };
    var sync = new object();
    var tokenRepo = Stub<ITokenRepository>((m,a) => {
        if (m.Name == "GetRefreshToken") { lock(sync) return Task.FromResult(new UserRefreshToken { UserId=stored.UserId, Code=stored.Code, Expiration=stored.Expiration }); }
        if (m.Name == "RotateRefreshToken") { lock(sync) { var valid = stored.Code == (string)a![1]!; if(valid) stored.Code = (string)a[2]!; return Task.FromResult(valid); } }
        throw new Exception("Unexpected token repository call: " + m.Name);
    });
    var service = new TokenService(repository, tokenRepo, options, new HttpClient());
    var refreshed = await service.CreateNewRefreshToken(user, "legacy-token");
    Assert(refreshed.RefreshToken != "legacy-token" && refreshed.RefreshTokenExpiration == originalExpiry);
    Assert(await controller.NewToken(new NewTokenViewModel { JwtToken = refreshed.AccessToken, RefreshToken = refreshed.RefreshToken }) is OkObjectResult);
    Assert(stored.Code.StartsWith("sha256:") && stored.Code != refreshed.RefreshToken);
    try { await service.CreateNewRefreshToken(user, "legacy-token"); throw new Exception("Replay accepted"); } catch (UnauthorizedAccessException) { }
    var next = await service.CreateNewRefreshToken(user, refreshed.RefreshToken);
    Assert(next.RefreshToken != refreshed.RefreshToken);
    var results = await Task.WhenAll(Enumerable.Range(0,16).Select(_ => Task.Run(async () => {
        try { await service.CreateNewRefreshToken(user,next.RefreshToken); return true; } catch(UnauthorizedAccessException) { return false; }
    })));
    Assert(results.Count(x=>x)==1);
});
await Check("password reset enforces 6-24 character boundaries", async () => {
    foreach (var length in new[] { 5, 6, 24, 25 }) {
        var password = "Aa1!" + new string('x', length - 4);
        var challenge = challenges.Issue("reset", user.Id, user.PasswordHash, "123456", TimeSpan.FromMinutes(1));
        var result = await controller.CompletePasswordReset(new CompletePasswordResetViewModel {
            ChallengeId = challenge, Code = "123456", NewPassword = password, NewPasswordAgain = password
        });
        Assert(length is 6 or 24 ? result is OkObjectResult : result is BadRequestObjectResult,
            "Unexpected result for password length " + length);
    }
});
await Check("only allowlisted endpoints are anonymous", () => {
    var expected = new[] { "SignIn", "CheckTwoFactorAuth", "NewToken", "ResetPassword", "CompletePasswordReset", "TabClose", "LogHeartbeat", "LogPageVisit" }
        .OrderBy(x => x).ToList();
    var actual = typeof(UserController).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
        .Where(x => x.GetCustomAttribute<AllowAnonymousAttribute>() != null).Select(x => x.Name).OrderBy(x => x).ToList();
    Assert(expected.SequenceEqual(actual), "Anonymous endpoints: " + string.Join(", ", actual));
    return Task.CompletedTask;
});
await Check("user management endpoints require user management policies", () => {
    var expected = new Dictionary<string, string> {
        ["CreateUser"] = UserManagementPolicies.Write,
        ["EditUser"] = UserManagementPolicies.Write,
        ["DeleteUser"] = UserManagementPolicies.Write,
        ["SendEmailForNewUser"] = UserManagementPolicies.Write,
        ["SendSmsForNewUser"] = UserManagementPolicies.Write,
        ["GetAllUserConnectSiteLogs"] = UserManagementPolicies.Read,
        ["GetAllvwPageVisits"] = UserManagementPolicies.Read,
        ["GetAllUsers"] = UserManagementPolicies.ReadOrInternalService,
    };
    foreach (var (name, policy) in expected)
        Assert(typeof(UserController).GetMethod(name)!.GetCustomAttribute<AuthorizeAttribute>()?.Policy == policy, name + " must require " + policy);
    return Task.CompletedTask;
});
await Check("test SMS and dead OTP endpoints are removed", () => {
    Assert(typeof(UserController).GetMethod("PostSms") == null && typeof(UserController).GetMethod("GetUserForOtp") == null);
    Assert(typeof(IUserService).GetMethod("PostSms") == null);
    return Task.CompletedTask;
});
await Check("users can read only their own record without user management permission", async () => {
    authorizationAllowed = false;
    context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u"), new Claim(ClaimTypes.GivenName, "ALICE")], "test"));
    Assert(await controller.GetUser("alice") is OkObjectResult);
    Assert(await controller.GetUser("bob") is ForbidResult);
    context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "admin"), new Claim(ClaimTypes.GivenName, "admin")], "test"));
    Assert(await controller.GetUser("alice") is ForbidResult);
    authorizationAllowed = true;
    Assert(await controller.GetUser("alice") is OkObjectResult);
    authorizationAllowed = false;
    context.User = new ClaimsPrincipal(new ClaimsIdentity());
});
await Check("tab-close records exits only for validly signed tokens", async () => {
    async Task TabClose(string token) {
        var body = System.Text.Json.JsonSerializer.Serialize(new { TabId = "t", CurrentPage = "/kumes/rapor", Token = token });
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        Assert(await controller.TabClose() is OkResult);
    }
    calls.Clear();
    foreach (var forged in new[] { Jwt(key: new string('x', 64)), Jwt(issuer: "wrong"), Jwt(unsigned: true), "garbage" })
        await TabClose(forged);
    Assert(!calls.Any(x => x.StartsWith("page-exit:")), "Forged token produced a page exit record");
    await TabClose(Jwt());
    Assert(calls.Contains("page-exit:u"));
});
UserManagementAuthorizationHandler PermissionHandler(string apiKey, Func<HttpResponseMessage>? sharedAppResponse) =>
    new(new HttpContextAccessor(), new StubHttpClientFactory(sharedAppResponse), new MemoryCache(new MemoryCacheOptions()),
        Options.Create(new UserManagementOptions { FormLink = "/yonetim/kullanici-yonetimi" }),
        Options.Create(new InternalServiceOptions { AuthenticationApiKey = apiKey }),
        NullLogger<UserManagementAuthorizationHandler>.Instance);
async Task<bool> Evaluate(UserManagementAuthorizationHandler handler, bool requiresWrite, bool allowInternalService, ClaimsPrincipal principal, HttpContext http) {
    var authorizationContext = new AuthorizationHandlerContext([new UserManagementRequirement(requiresWrite, allowInternalService)], principal, http);
    await handler.HandleAsync(authorizationContext);
    return authorizationContext.HasSucceeded;
}
HttpContext InternalRequest(string? key) {
    var http = new DefaultHttpContext();
    if (key != null) http.Request.Headers[InternalServiceOptions.ApiKeyHeaderName] = key;
    return http;
}
await Check("internal service key is accepted only when configured, matching and allowed", async () => {
    var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
    Assert(await Evaluate(PermissionHandler("internal-key", null), false, true, anonymous, InternalRequest("internal-key")));
    Assert(!await Evaluate(PermissionHandler("internal-key", null), false, true, anonymous, InternalRequest("wrong-key")));
    Assert(!await Evaluate(PermissionHandler("internal-key", null), false, true, anonymous, InternalRequest(null)));
    Assert(!await Evaluate(PermissionHandler("", null), false, true, anonymous, InternalRequest("")));
    Assert(!await Evaluate(PermissionHandler("internal-key", null), false, false, anonymous, InternalRequest("internal-key")));
});
await Check("form permission decides read and write access", async () => {
    var signedIn = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "u")], "test"));
    HttpContext UserRequest() { var http = new DefaultHttpContext(); http.Request.Headers.Authorization = "Bearer token"; return http; }
    Func<HttpResponseMessage> Respond(HttpStatusCode status, string body = "") => () => new HttpResponseMessage(status) { Content = new StringContent(body) };
    var writer = Respond(HttpStatusCode.OK, "{\"yazma\":true}");
    var reader = Respond(HttpStatusCode.OK, "{\"yazma\":false}");
    Assert(await Evaluate(PermissionHandler("", writer), true, false, signedIn, UserRequest()));
    Assert(await Evaluate(PermissionHandler("", reader), false, false, signedIn, UserRequest()));
    Assert(!await Evaluate(PermissionHandler("", reader), true, false, signedIn, UserRequest()));
    Assert(!await Evaluate(PermissionHandler("", Respond(HttpStatusCode.NoContent)), false, false, signedIn, UserRequest()));
    Assert(!await Evaluate(PermissionHandler("", Respond(HttpStatusCode.InternalServerError)), false, false, signedIn, UserRequest()));
});
await Check("secret placeholders resolve from environment and fail fast when missing", () => {
    Environment.SetEnvironmentVariable("MATLI_TEST_DB_PASSWORD", "s3cr3t;=value");
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> {
            ["ConnectionStrings:SqlServer"] = "Server=db;User Id=app;Password={MATLI_TEST_DB_PASSWORD};",
            ["Routes:0:UpstreamPathTemplate"] = "/kumesscada/{everything}",
            ["Serilog:OutputTemplate"] = "{Timestamp} {Level:u3} {ID}",
        })
        .AddEnvironmentPlaceholders()
        .Build();
    Assert(configuration.GetConnectionString("SqlServer") == "Server=db;User Id=app;Password=s3cr3t;=value;");
    Assert(configuration["Routes:0:UpstreamPathTemplate"] == "/kumesscada/{everything}", "Ocelot template must stay untouched");
    Assert(configuration["Serilog:OutputTemplate"] == "{Timestamp} {Level:u3} {ID}", "Serilog template must stay untouched");

    Environment.SetEnvironmentVariable("MATLI_TEST_DB_PASSWORD", null);
    try {
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:SqlServer"] = "Password={MATLI_TEST_DB_PASSWORD};" })
            .AddEnvironmentPlaceholders()
            .Build();
        throw new Exception("Missing environment variable was not reported");
    } catch (InvalidOperationException ex) {
        Assert(ex.Message.Contains("MATLI_TEST_DB_PASSWORD"), "Error must name the missing variable");
    }
    return Task.CompletedTask;
});
await Check("secret placeholders resolve in WebApplication and generic host builders", () => {
    Environment.SetEnvironmentVariable("MATLI_TEST_JWT_KEY", "host-value");
    var values = new Dictionary<string, string?> { ["TokenOption:SecurityKey"] = "{MATLI_TEST_JWT_KEY}" };

    var webBuilder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
    webBuilder.Configuration.AddInMemoryCollection(values);
    webBuilder.Configuration.AddEnvironmentPlaceholders();
    Assert(webBuilder.Configuration["TokenOption:SecurityKey"] == "host-value", "WebApplication builder did not resolve");

    using var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(configuration => configuration.AddInMemoryCollection(values))
        .ConfigureAppConfiguration(configuration => configuration.AddEnvironmentPlaceholders())
        .Build();
    var hostConfiguration = (IConfiguration)host.Services.GetService(typeof(IConfiguration))!;
    Assert(hostConfiguration["TokenOption:SecurityKey"] == "host-value", "Generic host builder did not resolve");

    Environment.SetEnvironmentVariable("MATLI_TEST_JWT_KEY", null);
    return Task.CompletedTask;
});
await Check("response encryptor stays byte-compatible with the frontend's legacy AES format", () => {
    const string key = "0123456789abcdef0123456789abcdef";
    static string LegacyAesEncrypt(string plainText, string key) {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = new byte[16];
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new System.Security.Cryptography.CryptoStream(ms, encryptor, System.Security.Cryptography.CryptoStreamMode.Write);
        using (var sw = new StreamWriter(cs)) sw.Write(plainText);
        return Convert.ToBase64String(ms.ToArray());
    }
    var encryptor = new ResponseEncryptor(Options.Create(new ResponseEncryptionOptions { Key = key }));
    foreach (var sample in new[] { "", "[]", "{\"Ad\":\"Kümes Şubesi\",\"Id\":42}", new string('x', 1000) })
        Assert(encryptor.Encrypt(sample) == LegacyAesEncrypt(sample, key), "Output differs from legacy format for: " + sample);
    return Task.CompletedTask;
});
await Check("response encryption key is validated at startup", () => {
    foreach (var invalidKey in new[] { "", "too-short", new string('k', 33) }) {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ResponseEncryption:Key"] = invalidKey }).Build();
        using var provider = new Microsoft.Extensions.DependencyInjection.ServiceCollection().AddResponseEncryption(configuration).BuildServiceProvider();
        try { _ = ((IOptions<ResponseEncryptionOptions>)provider.GetService(typeof(IOptions<ResponseEncryptionOptions>))!).Value; throw new Exception("Invalid key accepted: '" + invalidKey + "'"); }
        catch (OptionsValidationException) { }
    }
    return Task.CompletedTask;
});
await Check("error responses never contain stack traces", async () => {
    var http = new DefaultHttpContext();
    http.Request.Path = "/api/test";
    http.Response.Body = new MemoryStream();
    Exception thrown;
    try { throw new InvalidOperationException("Kullanıcı bulunamadı"); } catch (Exception ex) { thrown = ex; }
    http.Features.Set<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>(new Microsoft.AspNetCore.Diagnostics.ExceptionHandlerFeature { Error = thrown, Path = "/api/test" });
    await new MatliOtomasyon.Shared.Web.Errors.ExceptionMiddleware(_ => Task.CompletedTask, NullLogger<MatliOtomasyon.Shared.Web.Errors.ExceptionMiddleware>.Instance).Invoke(http);
    http.Response.Body.Position = 0;
    var body = await new StreamReader(http.Response.Body).ReadToEndAsync();
    Assert(http.Response.StatusCode == 500);
    Assert(!body.Contains("StackTrace") && !body.Contains(" at "), "Stack trace leaked: " + body);
    Assert(body.Contains("Kullanıcı bulunamadı") && body.Contains("\"Guid\""), "Error message or id missing: " + body);
});
Console.WriteLine($"{passed} security checks passed.");

static T Stub<T>(Func<MethodInfo, object?[]?, object?> invoke) where T:class {
    var proxy = DispatchProxy.Create<T, CallProxy>(); ((CallProxy)(object)proxy).Handler = invoke; return proxy;
}
public class CallProxy : DispatchProxy {
    public Func<MethodInfo, object?[]?, object?> Handler = null!;
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
}

/// <summary>
/// SharedApp'in form yetkisi cevabını taklit eden HttpClient üretir. Cevap verilmemişse SharedApp'e
/// hiç gidilmemesi beklenir; gidilirse test hata verir.
/// </summary>
public sealed class StubHttpClientFactory(Func<HttpResponseMessage>? response) : IHttpClientFactory {
    /// <summary>Her çağrıda sahte cevap dönen yeni bir HttpClient oluşturur.</summary>
    public HttpClient CreateClient(string name) => new(new StubHttpMessageHandler(response));
}

/// <summary>Verilen fonksiyonun ürettiği cevabı dönen, ağa çıkmayan HTTP handler'ı.</summary>
public sealed class StubHttpMessageHandler(Func<HttpResponseMessage>? response) : HttpMessageHandler {
    /// <summary>İsteği ağa göndermeden sahte cevabı döner.</summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        response == null ? throw new InvalidOperationException("SharedApp must not be called for this request.") : Task.FromResult(response());
}
