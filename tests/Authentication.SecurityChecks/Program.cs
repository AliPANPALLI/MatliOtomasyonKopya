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
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Configurations;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;

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
    "GetUserByName" => Task.FromResult((string)a![0]! == "alice" ? user : null!),
    "CheckPasswordAsync" => Task.FromResult(true),
    "AddConnectSiteLog" => Task.CompletedTask,
    "IncorrectLoginNumberUpdate" => Task.CompletedTask,
    _ => throw new Exception("Unexpected user service call: " + m.Name)
});
var repository = Stub<IUserRepository>((m,a) => m.Name switch {
    "GetUserById" => Task.FromResult((string)a![0]! == "u" ? user : null!),
    "TryResetPassword" => ResetPassword(a!),
    _ => throw new Exception("Unexpected user repository call: " + m.Name)
});
Task<bool> ResetPassword(object?[] a) { calls.Add("reset:" + a[0]); return Task.FromResult(true); }
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
var controller = new UserController(users, tokens, otp, null!, null!, new HttpContextAccessor { HttpContext = context }, null!, null!, challenges, options, repository) {
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
Console.WriteLine($"{passed} security checks passed.");

static T Stub<T>(Func<MethodInfo, object?[]?, object?> invoke) where T:class {
    var proxy = DispatchProxy.Create<T, CallProxy>(); ((CallProxy)(object)proxy).Handler = invoke; return proxy;
}
public class CallProxy : DispatchProxy {
    public Func<MethodInfo, object?[]?, object?> Handler = null!;
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
}
