using Authentication.Application.Interfaces;
using Authentication.Application.ViewModels;
using Authentication.Domain.Interfaces;
using Authentication.Domain.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedApp.Domain.DbViewModels;
using SharedKernel.Configurations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using static SharedKernel.ApiUrls;
using static SharedKernel.ViewModels.UserYetkiClaimViewModel;


namespace Authentication.Application.Services
{
    public class TokenService : ITokenService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly CustomTokenOption _tokenOption;
        private readonly HttpClient _httpClient;

        public TokenService(IUserRepository userRepository, ITokenRepository tokenRepository, IOptions<CustomTokenOption> tokenOption, HttpClient httpClient)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _tokenOption = tokenOption.Value;
            _httpClient = httpClient;
        }

        public async Task RemoveUserHeartBeat(string userId)
        {
            await _tokenRepository.RemoveUserHeartBeat(userId);
        }
        public async Task RemoveToken(string userId)
        {
            await _tokenRepository.RemoveToken(userId);
        }
        public async Task<TokenViewModel> CreateNewRefreshToken(User user, string refreshToken)
        {
            if (user.Silindi == true || user.IsLocked == true || user.IncorrectLoginNumber.GetValueOrDefault() >= 5)
                throw new UnauthorizedAccessException("Invalid session");
            var existing = await _tokenRepository.GetRefreshToken(user.Id);
            if (existing == null || existing.Expiration <= DateTime.Now || !RefreshTokenMatches(existing.Code, refreshToken))
                throw new UnauthorizedAccessException("Invalid refresh token");
            var token = await CreateToken(user, true);
            token.RefreshTokenExpiration = existing.Expiration;
            if (!await _tokenRepository.RotateRefreshToken(user.Id, existing.Code, HashRefreshToken(token.RefreshToken), user.PasswordHash))
                throw new UnauthorizedAccessException("Refresh token already consumed");
            return token;
        }
        private static string HashRefreshToken(string token) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        private static bool RefreshTokenMatches(string stored, string supplied)
        {
            // Existing sessions migrate from plaintext to hashed storage on their first rotation.
            var candidate = stored.StartsWith("sha256:", StringComparison.Ordinal) ? HashRefreshToken(supplied) : supplied;
            return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(stored), Encoding.UTF8.GetBytes(candidate));
        }
        private string CreateRefreshToken()
        {
            var numberByte = new byte[32];
            using var rnd = RandomNumberGenerator.Create();
            rnd.GetBytes(numberByte);

            return Convert.ToBase64String(numberByte);
        }
        private async Task<IEnumerable<Claim>> GetClaims(User user, List<string> audiences)
        {
            var userList = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier,user.Id),
            new Claim(JwtRegisteredClaimNames.Email,user.Email),
            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid ().ToString()),
            new Claim(ClaimTypes.GivenName,user.UserName),
            new Claim(ClaimTypes.Name,user.Name),
            new Claim(ClaimTypes.Surname,user.Surname),
            new Claim("position", user.Position == null ? "": user.Position),
            new Claim("lokasyonId", user.LokasyonId == null ? "": user.LokasyonId.ToString()),
            new Claim("tedarikci", user.Tedarikci.GetValueOrDefault(false).ToString().ToLower())
            };
            // components/UserPermissions claim'leri kasıtlı olarak JWT'ye eklenmiyor:
            // token boyutu kullanıcının yetki sayısıyla orantılı büyüyüp IIS'in header boyutu
            // limitini aşıyordu. Frontend artık bu veriyi SharedApp.API'nin
            // Yetki/GetUserPermissions ve Yetki/GetUserFormYetkiListByUserId endpoint'lerinden
            // kendi token'ıyla çekiyor (bkz. PermissionsService, front-end reposu).
            userList.AddRange(audiences.Select(x => new Claim(JwtRegisteredClaimNames.Aud, x)));
            return userList;
        }
        public async Task<TokenViewModel> CreateToken(User user, bool isRefreshing)
        {
            var accessTokenExpiration = DateTime.Now.AddMinutes(_tokenOption.AccessTokenExpiration);
            var refreshTokenExpiration = DateTime.Now.AddMinutes(user.TokenExpirationTime.GetValueOrDefault(180));
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_tokenOption.SecurityKey));
            SigningCredentials signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            JwtSecurityToken jwtSecurityToken = new JwtSecurityToken(
                issuer: _tokenOption.Issuer,
                expires: accessTokenExpiration,
                notBefore: DateTime.Now,
                claims: await GetClaims(user, _tokenOption.Audience),
                signingCredentials: signingCredentials);

            // Token oluşturma
            var handler = new JwtSecurityTokenHandler();
            var token = handler.WriteToken(jwtSecurityToken);
            var tokenDto = new TokenViewModel
            {
                AccessToken = token,
                RefreshToken = CreateRefreshToken(),
                AccessTokenExpiration = accessTokenExpiration,
                RefreshTokenExpiration = refreshTokenExpiration,
            };

            if (!isRefreshing)
            {
                var existRefreshToken = await _tokenRepository.GetRefreshToken(user.Id);
                if (existRefreshToken == null)
                {
                    UserRefreshToken newUserRefreshToken = new UserRefreshToken()
                    {
                        UserId = user.Id,
                        Code = HashRefreshToken(tokenDto.RefreshToken),
                        Expiration = tokenDto.RefreshTokenExpiration,
                    };
                    await _tokenRepository.AddUserRefreshToken(newUserRefreshToken);
                }
                else
                {
                    await _tokenRepository.UpdateUserRefreshToken(existRefreshToken, HashRefreshToken(tokenDto.RefreshToken), tokenDto.RefreshTokenExpiration);
                }
            }
            return tokenDto;
        }
    }
}
