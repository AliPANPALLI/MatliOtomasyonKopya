using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class UserYetkiRepository : Repository<UserYetki>, IUserYetkiRepository
    {
        private readonly SharedDbContext _context;

        public UserYetkiRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

        // UserLinkYetkiRepository.AddUserLinkYetki ile birebir aynı upsert
        // mantığı — bkz. o metot üzerindeki not.
        public async Task AddUserYetki(List<string> secilenKullanicilar, List<string> secilenLinks, int formId, bool okumaYetkisi, bool yazmaYetkisi, bool degistirmeYetkisi, bool silmeYetkisi, bool onaylayabilirMi, bool sorumluMu, bool silindi, bool aktif)
        {
            var userIds = (secilenKullanicilar ?? new())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            var links = (secilenLinks ?? new())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(NormalizeLink)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (userIds.Count == 0 || links.Count == 0)
                return;

            var existing = await _context.Set<UserYetki>()
                .Where(x => x.FormId == formId
                         && userIds.Contains(x.UserId)
                         && links.Contains(x.Link))
                .ToListAsync();

            var map = existing.ToDictionary(x => (x.UserId, x.FormId, x.Link));

            var now = DateTime.Now;

            foreach (var userId in userIds)
            {
                foreach (var link in links)
                {
                    var key = (userId, formId, link);

                    if (!map.TryGetValue(key, out var row))
                    {
                        row = new UserYetki
                        {
                            UserId = userId,
                            FormId = formId,
                            Link = link,
                            Okuma = okumaYetkisi,
                            Yazma = yazmaYetkisi,
                            Degistirme = degistirmeYetkisi,
                            Silme = silmeYetkisi,
                            Onaylayabilir = onaylayabilirMi,
                            SorumluMu = sorumluMu,
                            Silindi = silindi,
                            SilinmeZamani = silindi ? now : null,
                            Aktif = silindi ? false : aktif,
                        };

                        _context.Set<UserYetki>().Add(row);
                    }
                    else
                    {
                        row.Okuma = okumaYetkisi;
                        row.Yazma = yazmaYetkisi;
                        row.Degistirme = degistirmeYetkisi;
                        row.Silme = silmeYetkisi;
                        row.Onaylayabilir = onaylayabilirMi;
                        row.SorumluMu = sorumluMu;

                        row.Silindi = silindi;
                        row.SilinmeZamani = silindi ? now : null;

                        row.Aktif = silindi ? false : aktif;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> TransferUserYetkiByModul(string kaynakUserId, List<string> hedefUserIdler, int modulId)
        {
            var hedefler = (hedefUserIdler ?? new())
                .Where(x => !string.IsNullOrWhiteSpace(x) && x != kaynakUserId)
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            if (string.IsNullOrWhiteSpace(kaynakUserId) || hedefler.Count == 0)
                return 0;

            var formIdsInModul = await _context.Form
                .Where(f => f.ModulId == modulId && f.Silindi != true)
                .Select(f => f.Id)
                .ToListAsync();

            var kaynakSatirlari = await _context.Set<UserYetki>()
                .Where(x => x.UserId == kaynakUserId && x.Silindi != true && formIdsInModul.Contains(x.FormId))
                .ToListAsync();

            if (kaynakSatirlari.Count == 0)
                return 0;

            var existing = await _context.Set<UserYetki>()
                .Where(x => hedefler.Contains(x.UserId) && formIdsInModul.Contains(x.FormId))
                .ToListAsync();

            var map = existing.ToDictionary(x => (x.UserId, x.FormId, x.Link));

            var now = DateTime.Now;
            var kopyalanan = 0;

            foreach (var hedefUserId in hedefler)
            {
                foreach (var kaynak in kaynakSatirlari)
                {
                    var key = (hedefUserId, kaynak.FormId, kaynak.Link);

                    if (!map.TryGetValue(key, out var row))
                    {
                        row = new UserYetki { UserId = hedefUserId, FormId = kaynak.FormId, Link = kaynak.Link };
                        _context.Set<UserYetki>().Add(row);
                    }

                    row.Okuma = kaynak.Okuma;
                    row.Yazma = kaynak.Yazma;
                    row.Degistirme = kaynak.Degistirme;
                    row.Silme = kaynak.Silme;
                    row.Onaylayabilir = kaynak.Onaylayabilir;
                    row.SorumluMu = kaynak.SorumluMu;
                    row.Silindi = false;
                    row.SilinmeZamani = null;
                    row.Aktif = kaynak.Aktif ?? true;

                    kopyalanan++;
                }
            }

            await _context.SaveChangesAsync();
            return kopyalanan;
        }

        public async Task<List<UserYetki>> GetAllByUserId(string userId)
        {
            return await _context.Set<UserYetki>()
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.Silindi != true && x.Aktif != false)
                .ToListAsync();
        }

        public async Task<List<UserYetki>> GetKantarYetkiByUserId(string userId)
        {
            return await _context.Set<UserYetki>()
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.Silindi != true && x.Aktif != false && x.FormId == 54)
                .ToListAsync();
        }

        public async Task<List<UserYetki>> GetAllByFormId(int formId)
        {
            return await _context.Set<UserYetki>()
                .AsNoTracking()
                .Where(x => x.FormId == formId && x.Silindi != true)
                .ToListAsync();
        }

        public async Task<List<UserYetkiDetayModel>> GetAllByModulId(int modulId)
        {
            var query =
                from uy in _context.Set<UserYetki>().AsNoTracking()
                join f in _context.Form.AsNoTracking() on uy.FormId equals f.Id
                join m in _context.Modul.AsNoTracking() on f.ModulId equals m.Id
                where m.Id == modulId && f.Silindi != true && m.Silindi != true
                select new UserYetkiDetayModel
                {
                    Id = uy.Id,
                    UserId = uy.UserId,
                    ModulId = m.Id,
                    ModulName = m.Name,
                    FormId = f.Id,
                    FormName = f.Name,
                    Link = uy.Link,
                    Okuma = uy.Okuma,
                    Yazma = uy.Yazma,
                    Degistirme = uy.Degistirme,
                    Silme = uy.Silme,
                    Onaylayabilir = uy.Onaylayabilir,
                    SorumluMu = uy.SorumluMu,
                    Silindi = uy.Silindi,
                    SilinmeZamani = uy.SilinmeZamani,
                    Aktif = uy.Aktif,
                };

            return await query.ToListAsync();
        }

        public async Task<List<UserYetkiDetayModel>> GetAllEnriched()
        {
            var query =
                from uy in _context.Set<UserYetki>().AsNoTracking()
                where uy.Silindi != true
                join f in _context.Form.AsNoTracking() on uy.FormId equals f.Id
                join m in _context.Modul.AsNoTracking() on f.ModulId equals m.Id
                where f.Silindi != true && m.Silindi != true
                select new UserYetkiDetayModel
                {
                    Id = uy.Id,
                    UserId = uy.UserId,
                    ModulId = m.Id,
                    ModulName = m.Name,
                    FormId = f.Id,
                    FormName = f.Name,
                    Link = uy.Link,
                    Okuma = uy.Okuma,
                    Yazma = uy.Yazma,
                    Degistirme = uy.Degistirme,
                    Silme = uy.Silme,
                    Onaylayabilir = uy.Onaylayabilir,
                    SorumluMu = uy.SorumluMu,
                    Silindi = uy.Silindi,
                    SilinmeZamani = uy.SilinmeZamani,
                    Aktif = uy.Aktif,
                };

            return await query.ToListAsync();
        }

        private static string NormalizeLink(string link)
        {
            var x = (link ?? "").Trim();
            if (string.IsNullOrWhiteSpace(x)) return "";
            if (!x.StartsWith("/")) x = "/" + x;
            if (x.Length > 1 && x.EndsWith("/")) x = x[..^1];
            return x.ToLowerInvariant();
        }
    }
}
