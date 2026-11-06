using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.RequestModels;
using PoultryHouseScada.Application.ViewModels;
using PoultryHouseScada.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace PoultryHouseScada.Infrastructure.Services.VG
{
    public class VgYumurtlamaDonemAppService : IVgYumurtlamaDonemAppService
    {
        private readonly KumesScadaDbContext _db;

        public VgYumurtlamaDonemAppService(KumesScadaDbContext db)
        {
            _db = db;
        }

        public async Task<VgYumurtlamaDonemViewModel> SaveAsync(
            VgYumurtlamaDonemSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new Exception("Kayıt isteği boş olamaz.");

            if (string.IsNullOrWhiteSpace(request.KumesNo))
                throw new Exception("Kümes numarası zorunludur.");

            if (request.GirisTarihi == default)
                throw new Exception("Kümese giriş tarihi zorunludur.");

            if (request.BaslangicHayvanSayisi <= 0)
                throw new Exception("Başlangıç hayvan sayısı sıfırdan büyük olmalıdır.");

            var kumesNo = request.KumesNo.Trim();

            var kumesler = await _db.Set<Kumesler>()
                .FirstOrDefaultAsync(x =>
                    x.KumesNo != null && x.KumesNo.Trim().ToUpper() == kumesNo.ToUpper(),
                    cancellationToken);

            if (kumesler == null)
                throw new Exception(
                    $"\"{kumesNo}\" kodlu kümes 'Kumesler' tablosunda bulunamadı.");

            var girisTarihi = request.GirisTarihi.Date;

            var mevcut = await _db.Set<YumurtlamaDonem>()
                .FirstOrDefaultAsync(x =>
                    x.KumeslerId == kumesler.Id && x.GirisTarihi == girisTarihi,
                    cancellationToken);

            if (mevcut == null)
            {
                mevcut = new YumurtlamaDonem
                {
                    KumeslerId = kumesler.Id,
                    GirisTarihi = girisTarihi
                };

                _db.Set<YumurtlamaDonem>().Add(mevcut);
            }

            mevcut.BaslangicHayvanSayisi = request.BaslangicHayvanSayisi;
            mevcut.Irk = request.Irk?.Trim();
            mevcut.GirisYasiGun = request.GirisYasiGun;
            mevcut.GirisHaftasi = request.GirisHaftasi;
            mevcut.GuncellemeTarihi = DateTime.Now;

            await _db.SaveChangesAsync(cancellationToken);

            return new VgYumurtlamaDonemViewModel
            {
                KumesNo = kumesNo,
                GirisTarihi = mevcut.GirisTarihi,
                BaslangicHayvanSayisi = mevcut.BaslangicHayvanSayisi,
                Irk = mevcut.Irk,
                GirisYasiGun = mevcut.GirisYasiGun,
                GirisHaftasi = mevcut.GirisHaftasi
            };
        }

        public async Task<VgYumurtlamaDonemViewModel?> GetLatestByKumesNo(
            string kumesNo,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(kumesNo))
                return null;

            kumesNo = kumesNo.Trim();

            var donem = await _db.Set<YumurtlamaDonem>()
                .Include(x => x.Kumesler)
                .Where(x =>
                    x.Kumesler.KumesNo != null &&
                    x.Kumesler.KumesNo.Trim().ToUpper() == kumesNo.ToUpper())
                .OrderByDescending(x => x.GirisTarihi)
                .FirstOrDefaultAsync(cancellationToken);

            if (donem == null)
                return null;

            return new VgYumurtlamaDonemViewModel
            {
                KumesNo = kumesNo,
                GirisTarihi = donem.GirisTarihi,
                BaslangicHayvanSayisi = donem.BaslangicHayvanSayisi,
                Irk = donem.Irk,
                GirisYasiGun = donem.GirisYasiGun,
                GirisHaftasi = donem.GirisHaftasi
            };
        }

        public async Task<VgYumurtlamaDonemViewModel?> GetByKumesNoAndGirisTarihi(
            string kumesNo,
            DateTime girisTarihi,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(kumesNo))
                return null;

            kumesNo = kumesNo.Trim();
            var tarih = girisTarihi.Date;

            var donem = await _db.Set<YumurtlamaDonem>()
                .Include(x => x.Kumesler)
                .FirstOrDefaultAsync(x =>
                    x.Kumesler.KumesNo != null &&
                    x.Kumesler.KumesNo.Trim().ToUpper() == kumesNo.ToUpper() &&
                    x.GirisTarihi == tarih,
                    cancellationToken);

            if (donem == null)
                return null;

            return new VgYumurtlamaDonemViewModel
            {
                KumesNo = kumesNo,
                GirisTarihi = donem.GirisTarihi,
                BaslangicHayvanSayisi = donem.BaslangicHayvanSayisi,
                Irk = donem.Irk,
                GirisYasiGun = donem.GirisYasiGun,
                GirisHaftasi = donem.GirisHaftasi
            };
        }

        public async Task<DateTime?> GetNextGirisTarihiByKumesNo(
            string kumesNo,
            DateTime girisTarihi,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(kumesNo))
                return null;

            kumesNo = kumesNo.Trim();
            var tarih = girisTarihi.Date;

            return await _db.Set<YumurtlamaDonem>()
                .Include(x => x.Kumesler)
                .Where(x =>
                    x.Kumesler.KumesNo != null &&
                    x.Kumesler.KumesNo.Trim().ToUpper() == kumesNo.ToUpper() &&
                    x.GirisTarihi > tarih)
                .OrderBy(x => x.GirisTarihi)
                .Select(x => (DateTime?)x.GirisTarihi)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
