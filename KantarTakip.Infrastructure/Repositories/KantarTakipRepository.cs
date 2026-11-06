
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using KantarTakip.Domain.Enums;
using KantarTakip.Domain.Exceptions;
using KantarTakip.Domain.Helpers;
using KantarTakip.Domain.Interfaces;
using KantarTakip.Domain.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KantarTakip.Infrastructure.Repositories
{
    public class KantarTakipRepository : IKantarTakipRepository
    {
        private readonly KantarTakipDbContext _context;

        public KantarTakipRepository(KantarTakipDbContext context)
        {
            _context = context;
        }

        #region Firma

        public async Task<List<Firma>> GetAllFirma()
        {
            return await _context.Firma
                .AsNoTracking()
                .Where(x => x.Silindi != true)
                .ToListAsync();
        }

        public async Task<Firma?> GetFirmaById(int id)
        {
            return await _context.Firma
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Silindi != true);
        }

        public async Task AddFirma(Firma firma)
        {
            await _context.Firma.AddAsync(firma);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateFirma(Firma firma)
        {
            _context.Firma.Update(firma);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteFirma(int id)
        {
            var firma = await _context.Firma
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Silindi != true);

            if (firma == null)
                throw new KayitBulunamadiException("Firma bulunamadı.");

            firma.Silindi = true;

            await _context.SaveChangesAsync();
        }

        #endregion

        #region KaliteKontrolPersonel

        public async Task<List<KaliteKontrolPersonel>> GetAllKaliteKontrolPersonel()
        {
            return await _context.KaliteKontrolPersonel
                .AsNoTracking()
                .Where(x => x.Silindi != true)
                .OrderBy(x => x.AdSoyad)
                .ToListAsync();
        }

        public async Task<KaliteKontrolPersonel?> GetKaliteKontrolPersonelById(int id)
        {
            return await _context.KaliteKontrolPersonel
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Silindi != true);
        }

        public async Task AddKaliteKontrolPersonel(KaliteKontrolPersonel personel)
        {
            await _context.KaliteKontrolPersonel.AddAsync(personel);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateKaliteKontrolPersonel(KaliteKontrolPersonel personel)
        {
            _context.KaliteKontrolPersonel.Update(personel);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteKaliteKontrolPersonel(int id)
        {
            var personel = await _context.KaliteKontrolPersonel
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Silindi != true);

            if (personel == null)
                throw new KayitBulunamadiException("Kalite kontrol personeli bulunamadı.");

            personel.Silindi = true;

            await _context.SaveChangesAsync();
        }

        #endregion

        #region KantarGuvenlik

        public async Task<List<KantarGuvenlik>> GetAllKantarGuvenlik(
            DateTime? begin = null,
            DateTime? endExclusive = null)
        {
            var query = _context.KantarGuvenlik
                .AsNoTracking()
                .Where(x => x.Silindi != true);

            if (begin.HasValue)
                query = query.Where(x => x.KayitZamani >= begin.Value);

            if (endExclusive.HasValue)
                query = query.Where(x => x.KayitZamani < endExclusive.Value);

            return await query
                .OrderByDescending(x => x.KayitZamani)
                .ToListAsync();
        }

        public async Task<KantarGuvenlik?> GetKantarGuvenlikById(int id)
        {
            return await _context.KantarGuvenlik
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Silindi != true);
        }

        public async Task<List<KantarGuvenlik>> GetKantarGuvenlikByPlaka(
            string plaka)
        {
            var normalizedPlate = PlakaHelper.Normalize(plaka);

            if (normalizedPlate.Length == 0)
                return new List<KantarGuvenlik>();

            return await _context.KantarGuvenlik
                .AsNoTracking()
                .Where(x =>
                    x.Silindi != true &&
                    x.PlakaNormalized == normalizedPlate)
                .OrderByDescending(x => x.KayitZamani)
                .ToListAsync();
        }

        public async Task<KantarGuvenlik?> GetKantarGuvenlikByZiyaretciKayitId(
            string ziyaretciKayitId)
        {
            var kod = ziyaretciKayitId.Trim();

            return await _context.KantarGuvenlik
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Silindi != true &&
                    x.ZiyaretciKayitId == kod);
        }

        public async Task<List<KantarGuvenlik>> GetKantarGuvenlikByKayitAraligi(
            DateTime begin,
            DateTime endExclusive,
            CancellationToken cancellationToken = default)
        {
            return await _context.KantarGuvenlik
                .AsNoTracking()
                .Include(x => x.Firma)
                .Where(x =>
                    x.Silindi != true &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani >= begin &&
                    x.KayitZamani < endExclusive)
                .ToListAsync(cancellationToken);
        }

        public async Task AddKantarGuvenlik(
            KantarGuvenlik kantarGuvenlik)
        {
            await ZiyaretciKayitIdBenzersizMiKontrolEt(
                kantarGuvenlik.ZiyaretciKayitId, null);

            kantarGuvenlik.Plaka = kantarGuvenlik.Plaka?.Trim();
            kantarGuvenlik.PlakaNormalized =
                NormalizeOrNull(kantarGuvenlik.Plaka);
            kantarGuvenlik.Tarih = kantarGuvenlik.Tarih.Date;

            kantarGuvenlik.KaliteTamamlandi = false;
            kantarGuvenlik.KaliteTamamlanmaZamani = null;
            kantarGuvenlik.KaliteTamamlayanUserId = null;
            kantarGuvenlik.KaliteSonucu = null;

            await _context.KantarGuvenlik
                .AddAsync(kantarGuvenlik);

            await SaveChangesWithUniqueCheckAsync();
        }

        // Genel kontrol alanları güncellenir.
        // Giriş zamanı ve kalite tamamlama alanları korunur.
        public async Task UpdateKantarGuvenlik(
            KantarGuvenlik kantarGuvenlik)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var existing = await KaliteSureciAcikGuvenlikKilitle(
                kantarGuvenlik.Id);

            await ZiyaretciKayitIdBenzersizMiKontrolEt(
                kantarGuvenlik.ZiyaretciKayitId, existing.Id);

            existing.ZiyaretciKayitId = kantarGuvenlik.ZiyaretciKayitId;
            existing.Plaka = kantarGuvenlik.Plaka?.Trim();
            existing.PlakaNormalized = NormalizeOrNull(existing.Plaka);
            existing.Tarih = kantarGuvenlik.Tarih.Date;
            existing.UrunAdi = kantarGuvenlik.UrunAdi;
            existing.FirmaId = kantarGuvenlik.FirmaId;
            existing.IrsaliyeNo = kantarGuvenlik.IrsaliyeNo;
            existing.SonUrun = kantarGuvenlik.SonUrun;
            existing.GuncelleyenUserId = kantarGuvenlik.GuncelleyenUserId;

            await SaveChangesWithUniqueCheckAsync();

            await transaction.CommitAsync();
        }

        public async Task DeleteKantarGuvenlik(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var guvenlik = await KaliteSureciAcikGuvenlikKilitle(id);

            guvenlik.Silindi = true;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        #endregion

        #region KantarKalite

        public async Task<List<KantarKalite>> GetAllKantarKalite(
            DateTime? begin = null,
            DateTime? endExclusive = null)
        {
            var query = _context.KantarKalite
                .AsNoTracking()
                .Where(x => x.Silindi != true);

            if (begin.HasValue)
                query = query.Where(x => x.KayitZamani >= begin.Value);

            if (endExclusive.HasValue)
                query = query.Where(x => x.KayitZamani < endExclusive.Value);

            return await query
                .OrderByDescending(x => x.KayitZamani)
                .ToListAsync();
        }

        public async Task<KantarKalite?> GetKantarKaliteById(
            int id)
        {
            return await _context.KantarKalite
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Silindi != true);
        }

        public async Task<List<KantarKalite>>
            GetKantarKaliteByKantarGuvenlikId(
                int kantarGuvenlikId)
        {
            return await _context.KantarKalite
                .AsNoTracking()
                .Where(x =>
                    x.KantarGuvenlikId ==
                        kantarGuvenlikId &&
                    x.Silindi != true)
                .OrderByDescending(x => x.KayitZamani)
                .ToListAsync();
        }

        public async Task<List<KantarKalite>> GetKantarKaliteByKantarGuvenlikIds(
            IReadOnlyCollection<int> kantarGuvenlikIds,
            CancellationToken cancellationToken = default)
        {
            if (kantarGuvenlikIds.Count == 0)
                return new List<KantarKalite>();

            return await _context.KantarKalite
                .AsNoTracking()
                .Where(x =>
                    x.Silindi != true &&
                    x.KantarGuvenlikId.HasValue &&
                    kantarGuvenlikIds.Contains(x.KantarGuvenlikId.Value))
                .ToListAsync(cancellationToken);
        }

        // Yeni kalite kaydı
        public async Task AddKantarKalite(
            KantarKalite kantarKalite)
        {
            if (!kantarKalite.KantarGuvenlikId.HasValue)
            {
                throw new GecersizIstekException(
                    "Kalite kaydı için güvenlik ID zorunludur.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            await KaliteSureciAcikGuvenlikKilitle(
                kantarKalite.KantarGuvenlikId.Value);

            kantarKalite.Id = 0;
            kantarKalite.Silindi = false;

            await _context.KantarKalite
                .AddAsync(kantarKalite);

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        // Kalite kaydı düzenleme
        public async Task UpdateKantarKalite(
            KantarKalite kantarKalite)
        {
            if (!kantarKalite.KantarGuvenlikId.HasValue)
            {
                throw new GecersizIstekException(
                    "Güvenlik ID boş olamaz.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var existing = await _context.KantarKalite
                .FirstOrDefaultAsync(x =>
                    x.Id == kantarKalite.Id &&
                    x.Silindi != true);

            if (existing == null)
            {
                throw new KayitBulunamadiException(
                    "Güncellenecek kalite kaydı bulunamadı.");
            }

            if (!existing.KantarGuvenlikId.HasValue)
            {
                throw new IsKuraliException(
                    "Kalite kaydının güvenlik bağlantısı yok.");
            }

            // Mevcut ve (taşınıyorsa) hedef ziyaret açık olmalı.
            // Deadlock olmaması için kilitler hep artan ID sırasıyla alınır.
            var guvenlikIds = new[]
                {
                    existing.KantarGuvenlikId.Value,
                    kantarKalite.KantarGuvenlikId.Value
                }
                .Distinct()
                .OrderBy(x => x);

            foreach (var guvenlikId in guvenlikIds)
            {
                await KaliteSureciAcikGuvenlikKilitle(guvenlikId);
            }

            existing.KantarGuvenlikId =
                kantarKalite.KantarGuvenlikId;

            existing.Nem = kantarKalite.Nem;
            existing.Protein = kantarKalite.Protein;
            existing.Kul = kantarKalite.Kul;
            existing.Seluloz = kantarKalite.Seluloz;
            existing.Yag = kantarKalite.Yag;
            existing.Nisasta = kantarKalite.Nisasta;
            existing.Koh = kantarKalite.Koh;
            existing.Ureaz = kantarKalite.Ureaz;

            existing.Koku = kantarKalite.Koku;
            existing.Tozluluk = kantarKalite.Tozluluk;
            existing.Curuk = kantarKalite.Curuk;
            existing.Kuf = kantarKalite.Kuf;
            existing.Boceklenme = kantarKalite.Boceklenme;

            existing.Sonuc = kantarKalite.Sonuc;
            existing.SonucAciklama = kantarKalite.SonucAciklama;

            existing.KaliteKontrolPersonelId =
                kantarKalite.KaliteKontrolPersonelId;

            existing.GuncelleyenUserId =
                kantarKalite.GuncelleyenUserId;

            // Orijinal kayıt zamanını koruyoruz.
            // existing.KayitZamani DEĞİŞTİRİLMEZ.

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        // Kalite kaydı silme
        public async Task DeleteKantarKalite(int id)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var kalite = await _context.KantarKalite
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.Silindi != true);

            if (kalite == null)
            {
                throw new KayitBulunamadiException(
                    "Kalite kaydı bulunamadı.");
            }

            if (!kalite.KantarGuvenlikId.HasValue)
            {
                throw new IsKuraliException(
                    "Kalite kaydının güvenlik bağlantısı yok.");
            }

            await KaliteSureciAcikGuvenlikKilitle(
                kalite.KantarGuvenlikId.Value);

            kalite.Silindi = true;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        #endregion

        #region Bosaltim

        // Boşaltım kalite kararından sonra yapılır.
        // Tekrar çağrılırsa döküldüğü yer düzeltilir.
        public async Task BosaltimKaydet(
            int kantarGuvenlikId,
            string dokulduguYer,
            string userId)
        {
            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            var guvenlik = await GuvenlikKilitle(kantarGuvenlikId);

            if (!guvenlik.KaliteTamamlandi)
            {
                throw new IsKuraliException(
                    "Boşaltım bilgisi için önce kalite kontrolü tamamlanmalıdır.");
            }

            if (guvenlik.KaliteSonucu == KaliteSonucu.Red)
            {
                throw new IsKuraliException(
                    "Kalite kararı Red olan araç için boşaltım girilemez.");
            }

            guvenlik.DokulduguYer = dokulduguYer;
            guvenlik.BosaltimZamani = DateTime.Now;
            guvenlik.BosaltimYapanUserId = userId;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
        }

        #endregion

        #region Kalite Surec Tamamlama

        public async Task KaliteTamamlaAsync(
            int kantarGuvenlikId,
            string userId,
            CancellationToken cancellationToken = default)
        {
            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(cancellationToken);

            var guvenlik = await KaliteSureciAcikGuvenlikKilitle(
                kantarGuvenlikId,
                "Bu aracın kalite kontrolü zaten tamamlanmıştır.",
                cancellationToken);

            // Ziyaretin nihai kararı en son kalite kaydının sonucudur.
            var sonKalite = await _context.KantarKalite
                .Where(x =>
                    x.KantarGuvenlikId ==
                        kantarGuvenlikId &&
                    x.Silindi != true)
                .OrderByDescending(x => x.KayitZamani)
                .ThenByDescending(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (sonKalite == null)
            {
                throw new IsKuraliException(
                    "Kalite kontrolünü tamamlamak için " +
                    "en az bir kalite kaydı gereklidir.");
            }

            if (!sonKalite.Sonuc.HasValue)
            {
                throw new IsKuraliException(
                    "Kalite kontrolünü tamamlamak için son kalite " +
                    "kaydında Kabul, Şartlı Kabul veya Red seçilmelidir.");
            }

            if (!sonKalite.KaliteKontrolPersonelId.HasValue)
            {
                throw new IsKuraliException(
                    "Kalite kontrolünü tamamlamak için son kalite " +
                    "kaydında kontrol eden personel seçilmelidir.");
            }

            guvenlik.KaliteSonucu = sonKalite.Sonuc;

            guvenlik.KaliteTamamlandi = true;

            guvenlik.KaliteTamamlanmaZamani =
                DateTime.Now;

            guvenlik.KaliteTamamlayanUserId =
                userId;

            await _context.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }

        #endregion

        #region Helpers

        private async Task ZiyaretciKayitIdBenzersizMiKontrolEt(
            string? ziyaretciKayitId,
            int? haricTutulacakId)
        {
            if (string.IsNullOrWhiteSpace(ziyaretciKayitId))
                return;

            var kullanimdaMi = await _context.KantarGuvenlik
                .AnyAsync(x =>
                    x.Silindi != true &&
                    x.ZiyaretciKayitId == ziyaretciKayitId &&
                    (!haricTutulacakId.HasValue || x.Id != haricTutulacakId.Value));

            if (kullanimdaMi)
            {
                throw new IsKuraliException(
                    $"'{ziyaretciKayitId}' ziyaretçi kayıt ID'si " +
                    "başka bir kayıtta kullanılıyor.");
            }
        }

        // Kontrol ile kayıt arasında aynı kod başka istekten
        // girilirse unique index yakalar; 500 yerine 409 dönülür.
        private async Task SaveChangesWithUniqueCheckAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                throw new IsKuraliException(
                    "Ziyaretçi kayıt ID'si başka bir kayıtta kullanılıyor.");
            }
        }

        private static string? NormalizeOrNull(string? plaka)
        {
            var normalized = PlakaHelper.Normalize(plaka);

            return normalized.Length == 0 ? null : normalized;
        }

        // Güvenlik satırını transaction sonuna kadar UPDLOCK ile kilitler.
        // Aynı ziyaret üzerindeki kalite ekleme/düzenleme/silme ve
        // tamamlama işlemleri böylece sırayla çalışır; kontrol ile yazma
        // arasında başka bir işlem araya giremez.
        private async Task<KantarGuvenlik> GuvenlikKilitle(
            int kantarGuvenlikId,
            CancellationToken cancellationToken = default)
        {
            var guvenlik = await _context.KantarGuvenlik
                .FromSqlInterpolated(
                    $"SELECT * FROM KantarGuvenlik WITH (UPDLOCK, ROWLOCK) WHERE Id = {kantarGuvenlikId}")
                .FirstOrDefaultAsync(cancellationToken);

            if (guvenlik == null || guvenlik.Silindi == true)
            {
                throw new KayitBulunamadiException(
                    "Güvenlik kaydı bulunamadı.");
            }

            return guvenlik;
        }

        // GuvenlikKilitle + kalite süreci hâlâ açık mı kontrolü.
        private async Task<KantarGuvenlik> KaliteSureciAcikGuvenlikKilitle(
            int kantarGuvenlikId,
            string? tamamlanmisMesaji = null,
            CancellationToken cancellationToken = default)
        {
            var guvenlik = await GuvenlikKilitle(
                kantarGuvenlikId, cancellationToken);

            if (guvenlik.KaliteTamamlandi)
            {
                throw new IsKuraliException(
                    tamamlanmisMesaji ??
                    "Bu aracın kalite kontrolü " +
                    "tamamlanmıştır. Kayıt eklenemez, " +
                    "güncellenemez veya silinemez.");
            }

            return guvenlik;
        }

        #endregion
    }
}
