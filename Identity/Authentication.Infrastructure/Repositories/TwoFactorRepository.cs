using Authentication.Domain.Interfaces;
using Authentication.Domain.Models;
using Microsoft.EntityFrameworkCore;
using OtpNet;
using static SharedKernel.ApplicationConstants;


namespace Authentication.Infrastructure.Repositories
{
    public class TwoFactorRepository : ITwoFactorRepository
    {
        private AuthDbContext _context;
        public TwoFactorRepository(AuthDbContext context)
        {
            _context = context;
        }
        public async Task<string> GetCodeVerification(string userId, string ipAdres)
        {
            var now = DateTime.Now;

            var existOneTimePassword = await _context.OneTimePassword.Where(x => x.EkleyenUserId == userId && (x.Aktif ?? true)).ToListAsync();

            foreach (var otp in existOneTimePassword)
            {
                otp.Aktif = false;
                otp.GuncellemeTarihi = now;
                otp.GuncelleyenUserId = userId;
            }
            await _context.SaveChangesAsync();

            // Kullanıcı için gizli anahtar oluştur ve veritabanında sakla
            var userSecret = KeyGeneration.GenerateRandomKey(20);
            var base32UserSecret = Base32Encoding.ToString(userSecret);
            // OTP oluşturma
            var totp = new Totp(userSecret, step: 120);
            var otpCode = totp.ComputeTotp();

            OneTimePassword oneTimePassword = new OneTimePassword();
            oneTimePassword.Aktif = true;
            oneTimePassword.OtpSecretCode = base32UserSecret;
            oneTimePassword.OtpCode = otpCode;
            oneTimePassword.EklenmeTarihi = now;
            oneTimePassword.EkleyenUserId = userId;
            oneTimePassword.GuncellemeTarihi = now;
            oneTimePassword.GuncelleyenUserId = userId;
            oneTimePassword.Kullanildi = false;
            oneTimePassword.DenemeSayisi = 0;
            oneTimePassword.IP = ipAdres.Trim();
            oneTimePassword.Domain = ipAdres.StartsWith("10.", StringComparison.Ordinal) && ipAdres != "10.1.6.20";

            await _context.OneTimePassword.AddAsync(oneTimePassword);
            await _context.SaveChangesAsync();

            return otpCode;
        }

        public async Task<bool> VerifyOtpCode(string userId, string userOtpCode)
        {
            //veritabanından secret cekilmeli
            var now = DateTime.Now;
            var oneTimePassword = await _context.OneTimePassword.Where(x => x.EkleyenUserId == userId && (x.Aktif ?? true)).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            if (oneTimePassword == null)
            {
                throw new Exception("Bir hata oluştu");
            }
            else
            {
                oneTimePassword.Kullanildi = true;
                oneTimePassword.DenemeSayisi += 1;

                if (oneTimePassword.DenemeSayisi.GetValueOrDefault(0) > 3)
                {
                    oneTimePassword.Aktif = false;
                    oneTimePassword.GuncellemeTarihi = now;
                    oneTimePassword.GuncelleyenUserId = userId;
                    await _context.SaveChangesAsync();
                    throw new Exception("3 başarısız giriş hatası");
                }
                if (oneTimePassword.EklenmeTarihi < now.AddMinutes(-2))
                {
                    oneTimePassword.Aktif = false;
                    oneTimePassword.GuncellemeTarihi = now;
                    oneTimePassword.GuncelleyenUserId = userId;
                    await _context.SaveChangesAsync();
                    throw new Exception("Süresi geçmiş OTP kodu hatası");
                }

                // Veritabanında saklanan gizli anahtarı kullanarak OTP doğrulama
                string base32UserSecret = oneTimePassword.OtpSecretCode;
                var storedSecret = Base32Encoding.ToBytes(base32UserSecret); // Veritabanından alınan gizli anahtar
                var totpStored = new Totp(storedSecret, step: 120);
                bool isValid = totpStored.VerifyTotp(userOtpCode, out long timeStepMatched, new VerificationWindow(previous: 1, future: 1));

                if (isValid)
                {
                    oneTimePassword.Aktif = false;
                    oneTimePassword.GuncellemeTarihi = now;
                    oneTimePassword.GuncelleyenUserId = userId;
                }
                await _context.SaveChangesAsync();

                return isValid;
            }
        }

        //public async Task<bool> CheckOtpRequired(User user, string ipAdres)
        //{
        //    var otpDomainDisi = await _context.GenelParametreler.Where(x => x.Name == OtpParametreler.OtpDomainDisi).Select(x => x.Deger).FirstOrDefaultAsync() == "1" ? true : false;
        //    var otpDomainIci = await _context.GenelParametreler.Where(x => x.Name == OtpParametreler.OtpDomainIci).Select(x => x.Deger).FirstOrDefaultAsync() == "1" ? true : false;
        //    var otpZorunlu = await _context.GenelParametreler.Where(x => x.Name == OtpParametreler.OtpZorunlu).Select(x => x.Deger).FirstOrDefaultAsync() == "1" ? true : false;

        //    var userOTPEnabled = otpDomainIci || ipAdres.Substring(0, 2) != "10" || ipAdres == "10.1.6.20" || ipAdres == "172.16.37.1";

        //    if ((otpDomainDisi && userOTPEnabled && user.RequiresOtp.GetValueOrDefault(false)) || otpZorunlu)
        //        return true;
        //    else
        //        return false;
        //}
    }
}
