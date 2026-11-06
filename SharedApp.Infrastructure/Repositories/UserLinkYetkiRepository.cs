using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;
using SharedKernel.Menus;

namespace SharedApp.Infrastructure.Repositories
{
    public class UserLinkYetkiRepository : Repository<UserLinkYetki>, IUserLinkYetkiRepository
    {
        private readonly SharedDbContext _context;
        public UserLinkYetkiRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<vwUserLinkYetki>> GetAllUserLinkYetkiForTesisScada()
        {
            //return await _context.vwUserLinkYetki.ToListAsync();
            var rows = await _context.vwUserLinkYetki.AsNoTracking().Where(x => x.ModulId == 11).ToListAsync();

            var map = MenuPathHelper.BuildLinkPathMap(AppMenus.TesisScada);

            return rows.Select(r =>
            {
                var key = MenuPathHelper.NormalizeLink(r.Link);
                var path = map.TryGetValue(key, out var txt) ? txt : r.Link;

                return new vwUserLinkYetki
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserFullName = r.UserFullName,
                    UserName = r.UserName,
                    ModulId = r.ModulId,
                    ModulName = r.ModulName,
                    FormId = r.FormId,
                    FormName = r.FormName,
                    FormAciklama = r.FormAciklama,
                    Link = r.Link,
                    LinkPathText = path,
                    Read = r.Read,
                    Write = r.Write,
                    SorumluMu = r.SorumluMu,
                    Silindi = r.Silindi,
                    SilinmeZamani = r.SilinmeZamani,
                    Aktif = r.Aktif
                };
            }).ToList();
        }

        public async Task<List<vwUserLinkYetki>> GetAllUserLinkYetkiForSunucuScada()
        {
            //return await _context.vwUserLinkYetki.ToListAsync();
            var rows = await _context.vwUserLinkYetki.AsNoTracking().Where(x => x.ModulId == 10).ToListAsync();

            var map = MenuPathHelper.BuildLinkPathMap(AppMenus.SunucuMcc);

            return rows.Select(r =>
            {
                var key = MenuPathHelper.NormalizeLink(r.Link);
                var path = map.TryGetValue(key, out var txt) ? txt : r.Link;

                return new vwUserLinkYetki
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserFullName = r.UserFullName,
                    UserName = r.UserName,
                    ModulId = r.ModulId,
                    ModulName = r.ModulName,
                    FormId = r.FormId,
                    FormName = r.FormName,
                    FormAciklama = r.FormAciklama,
                    Link = r.Link,
                    LinkPathText = path,
                    Read = r.Read,
                    Write = r.Write,
                    SorumluMu = r.SorumluMu,
                    Silindi = r.Silindi,
                    SilinmeZamani = r.SilinmeZamani,
                    Aktif = r.Aktif
                };
            }).ToList();
        }

        public async Task AddUserLinkYetki(List<string> secilenKullanicilar, List<string> secilenLinks, int formId, bool okumaYetkisi, bool yazmaYetkisi, bool sorumluMu, bool silindi, bool aktif)
        {
            // 1) Temizle / normalize / uniq
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

            // 2) Mevcut kayıtları tek seferde çek
            var existing = await _context.Set<UserLinkYetki>()
                .Where(x => x.FormId == formId
                         && userIds.Contains(x.UserId)
                         && links.Contains(x.Link))
                .ToListAsync();

            var map = existing.ToDictionary(x => (x.UserId, x.FormId, x.Link));

            var now = DateTime.Now;

            // 3) Upsert
            foreach (var userId in userIds)
            {
                foreach (var link in links)
                {
                    var key = (userId, formId, link);

                    if (!map.TryGetValue(key, out var row))
                    {
                        row = new UserLinkYetki
                        {
                            UserId = userId,
                            FormId = formId,
                            Link = link,
                            Read = okumaYetkisi,
                            Write = yazmaYetkisi,
                            SorumluMu = sorumluMu,
                            Silindi = silindi,
                            SilinmeZamani = silindi ? now : null,
                            Aktif = silindi ? false : aktif
                        };

                        _context.Set<UserLinkYetki>().Add(row);
                    }
                    else
                    {
                        row.Read = okumaYetkisi;
                        row.Write = yazmaYetkisi;
                        row.SorumluMu = sorumluMu;

                        row.Silindi = silindi;
                        row.SilinmeZamani = silindi ? now : null;

                        row.Aktif = silindi ? false : aktif;
                    }
                }
            }
            await _context.SaveChangesAsync();
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