using Microsoft.EntityFrameworkCore;
using MobilGuncelleme.Application.Dtos;
using MobilGuncelleme.Application.Interfaces;
using MobilGuncelleme.Domain.Entities;
using SharedKernel.Extensions;

namespace MobilGuncelleme.Application.Services
{
    /// <summary>
    /// Sürüm kontrolü, APK yükleme ve dağıtım kararlarının tek yeri. Controller'lar yalnızca
    /// HTTP'yi çözer, karar burada verilir.
    /// </summary>
    public class MobilSurumAppService : IMobilSurumAppService
    {
        private readonly IMobilGuncellemeDbContext _db;
        private readonly IApkDepo _apkDepo;

        public MobilSurumAppService(IMobilGuncellemeDbContext db, IApkDepo apkDepo)
        {
            _db = db;
            _apkDepo = apkDepo;
        }

        public async Task<SurumKontrolSonucDto> KontrolAsync(CihazBildirDto dto, string? kullaniciId)
        {
            await CihazKaydetVeyaGuncelleAsync(dto, kullaniciId);

            var son = await SonAktifSurumAsync();

            // Her güncelleme zorunludur: kurulu sürüm son aktif sürümden küçükse güncelleme vardır.
            if (son is null || dto.VersionCode >= son.VersionCode)
                return new SurumKontrolSonucDto { GuncellemeVar = false };

            return new SurumKontrolSonucDto
            {
                GuncellemeVar = true,
                SurumId = son.Id,
                VersionCode = son.VersionCode,
                VersionName = son.VersionName,
                SurumNotu = son.SurumNotu,
                Sha256 = son.Sha256,
                Boyut = son.Boyut
            };
        }

        public async Task<ApkDosyaDto?> IndirmeDosyasiAsync(int surumId)
        {
            var surum = await _db.UygulamaSurum.AsNoTracking().FirstOrDefaultAsync(x => x.Id == surumId);

            // Pasif sürüm indirilemez: hatalı sürüm pasife alındığında yarım kalan indirmeler de durur.
            if (surum is null || !surum.Aktif || !_apkDepo.VarMi(surum.DosyaAdi))
                return null;

            return new ApkDosyaDto
            {
                Yol = _apkDepo.TamYol(surum.DosyaAdi),
                IndirmeAdi = $"matli-{surum.VersionName}.apk"
            };
        }

        public async Task<SurumDto> YukleAsync(SurumYukleDto dto, string? yukleyen, CancellationToken cancellationToken = default)
        {
            if (dto.Apk is null || dto.Apk.Length == 0)
                throw new BusinessException("APK dosyası boş.");

            if (!string.Equals(Path.GetExtension(dto.Apk.FileName), ".apk", StringComparison.OrdinalIgnoreCase))
                throw new BusinessException("Yalnızca .apk dosyası yüklenebilir.");

            // Android düşük veya eşit versionCode'u güncelleme olarak kurmaz; burada da kabul edilmez.
            var sonCode = await _db.UygulamaSurum.MaxAsync(x => (int?)x.VersionCode, cancellationToken) ?? 0;
            if (dto.VersionCode <= sonCode)
                throw new BusinessException($"versionCode {sonCode} değerinden büyük olmalı.");

            var dosyaAdi = $"matli-{dto.VersionName}-{dto.VersionCode}.apk";

            ApkKayitSonucu kayit;
            await using (var akis = dto.Apk.OpenReadStream())
                kayit = await _apkDepo.KaydetAsync(dosyaAdi, akis, cancellationToken);

            var surum = new UygulamaSurum
            {
                VersionCode = dto.VersionCode,
                VersionName = dto.VersionName,
                DosyaAdi = dosyaAdi,
                Sha256 = kayit.Sha256,
                Boyut = kayit.Boyut,
                SurumNotu = dto.SurumNotu,
                Aktif = dto.Aktif,
                YuklenmeTarihi = DateTime.UtcNow,
                Yukleyen = yukleyen
            };

            try
            {
                _db.UygulamaSurum.Add(surum);
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Aynı anda iki yükleme aynı versionCode'u aldıysa unique index ikincisini reddeder.
                // Kaydı olmayan dosya diskte kalmasın.
                _apkDepo.Sil(dosyaAdi);
                throw new BusinessException($"versionCode {dto.VersionCode} kaydedilemedi, başka bir yükleme aynı kodu kullanmış olabilir.");
            }

            return SurumDtoOlustur(surum);
        }

        public async Task<List<SurumDto>> SurumleriListeleAsync()
        {
            var surumler = await _db.UygulamaSurum.AsNoTracking()
                .OrderByDescending(x => x.VersionCode)
                .ToListAsync();

            return surumler.Select(SurumDtoOlustur).ToList();
        }

        public async Task<SurumDto> AktiflikAyarlaAsync(int surumId, bool aktif)
        {
            var surum = await _db.UygulamaSurum.FirstOrDefaultAsync(x => x.Id == surumId)
                ?? throw new BusinessException("Sürüm bulunamadı.");

            surum.Aktif = aktif;
            await _db.SaveChangesAsync();

            return SurumDtoOlustur(surum);
        }

        public async Task<List<CihazDto>> CihazlariListeleAsync()
        {
            var sonCode = (await SonAktifSurumAsync())?.VersionCode;

            return await _db.Cihaz.AsNoTracking()
                .OrderByDescending(x => x.SonGorulme)
                .Select(x => new CihazDto
                {
                    CihazId = x.CihazId,
                    KullaniciId = x.KullaniciId,
                    KuruluVersionCode = x.KuruluVersionCode,
                    CihazModeli = x.CihazModeli,
                    AndroidSurumu = x.AndroidSurumu,
                    SonGorulme = x.SonGorulme,
                    GuncelMi = sonCode == null || x.KuruluVersionCode >= sonCode
                })
                .ToListAsync();
        }

        private Task<UygulamaSurum?> SonAktifSurumAsync()
        {
            return _db.UygulamaSurum.AsNoTracking()
                .Where(x => x.Aktif)
                .OrderByDescending(x => x.VersionCode)
                .FirstOrDefaultAsync();
        }

        private async Task CihazKaydetVeyaGuncelleAsync(CihazBildirDto dto, string? kullaniciId)
        {
            var cihaz = await _db.Cihaz.FirstOrDefaultAsync(x => x.CihazId == dto.CihazId);
            if (cihaz is null)
            {
                cihaz = new Cihaz { CihazId = dto.CihazId };
                _db.Cihaz.Add(cihaz);
            }

            cihaz.KullaniciId = kullaniciId;
            cihaz.KuruluVersionCode = dto.VersionCode;
            cihaz.CihazModeli = dto.CihazModeli;
            cihaz.AndroidSurumu = dto.AndroidSurumu;
            cihaz.SonGorulme = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        private static SurumDto SurumDtoOlustur(UygulamaSurum x) => new()
        {
            Id = x.Id,
            VersionCode = x.VersionCode,
            VersionName = x.VersionName,
            DosyaAdi = x.DosyaAdi,
            Sha256 = x.Sha256,
            Boyut = x.Boyut,
            SurumNotu = x.SurumNotu,
            Aktif = x.Aktif,
            YuklenmeTarihi = x.YuklenmeTarihi,
            Yukleyen = x.Yukleyen
        };
    }
}
