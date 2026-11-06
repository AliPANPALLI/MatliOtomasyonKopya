using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ProjeTakip.Application.Dtos;
using ProjeTakip.Application.Exceptions;
using ProjeTakip.Domain.Entities;
using ProjeTakip.Infrastructure.Persistence;

namespace ProjeTakip.API.Security;

public sealed class ProjeTakipAccessService(ProjeTakipDbContext database, IHttpContextAccessor httpContextAccessor)
{
    public async Task<ProjeTakipYetkileriDto> MevcutYetkilerAsync(CancellationToken cancellationToken)
    {
        var (userId, userName) = CurrentUser();
        var permission = await database.ProjeTakipYetkileri.AsNoTracking()
            .SingleOrDefaultAsync(x => x.KullaniciId == userId, cancellationToken);
        if (permission is not null) return Map(permission);

        if (await database.ProjeTakipYetkileri.AnyAsync(cancellationToken)) return ProjeTakipYetkileriDto.Yetkisiz;

        permission = new ProjeTakipYetkisi
        {
            KullaniciId = userId,
            KullaniciAdi = userName,
            Goruntule = true,
            Duzenle = true,
            MaliyetIcmalGor = true,
            IcmalDuzenle = true,
            SilArsivle = true,
            YetkiYonet = true,
            NotDuzenle = true,
            OlusturulmaUtc = DateTime.UtcNow
        };
        database.ProjeTakipYetkileri.Add(permission);
        await database.SaveChangesAsync(cancellationToken);
        await DenetimEkleAsync("ILK_YONETICI", "ProjeTakipYetkisi", permission.Id.ToString(),
            "İzin tablosu boş olduğu için ilk doğrulanmış kullanıcı tam yetkili yönetici olarak atandı.", cancellationToken);
        return ProjeTakipYetkileriDto.TamYetki;
    }

    public async Task GoruntulemeDogrulaAsync(CancellationToken cancellationToken) =>
        await YetkiDogrulaAsync(x => x.Goruntule, "Proje Takip modülünü görüntüleme yetkiniz yok.", cancellationToken);

    public async Task DuzenlemeDogrulaAsync(CancellationToken cancellationToken) =>
        await YetkiDogrulaAsync(x => x.Duzenle, "Bu kaydı düzenleme yetkiniz yok.", cancellationToken);

    public async Task NotDuzenlemeDogrulaAsync(CancellationToken cancellationToken) =>
        await YetkiDogrulaAsync(x => x.Duzenle || x.NotDuzenle, "Not girme yetkiniz yok.", cancellationToken);

    public async Task SilmeArsivlemeDogrulaAsync(CancellationToken cancellationToken) =>
        await YetkiDogrulaAsync(x => x.SilArsivle, "Silme veya arşivleme yetkiniz yok.", cancellationToken);

    public async Task DokumanSilmeDogrulaAsync(CancellationToken cancellationToken) =>
        await YetkiDogrulaAsync(x => x.Duzenle || x.SilArsivle, "Dosya silme yetkiniz yok.", cancellationToken);

    public async Task IcmalGoruntulemeDogrulaAsync(CancellationToken cancellationToken) =>
        await YetkiDogrulaAsync(x => x.MaliyetIcmalGor, "Maliyet ve icmal bilgilerini görüntüleme yetkiniz yok.", cancellationToken);

    public async Task IcmalDuzenlemeDogrulaAsync(CancellationToken cancellationToken)
    {
        var permissions = await MevcutYetkilerAsync(cancellationToken);
        if (!permissions.MaliyetIcmalGor || !permissions.IcmalDuzenle)
            throw new ProjeTakipForbiddenException("İcmal ve maliyet bilgilerini düzenleme yetkiniz yok.");
    }

    public async Task YetkiYonetimiDogrulaAsync(CancellationToken cancellationToken) =>
        await YetkiDogrulaAsync(x => x.YetkiYonet, "Yetki ve denetim kayıtlarını yönetme yetkiniz yok.", cancellationToken);

    public Task<bool> DokumanIcmalMiAsync(int id, CancellationToken cancellationToken) =>
        database.ProjeDokumanlari.AsNoTracking().Where(x => x.Id == id).Select(x => x.Tur == "icmal").SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyCollection<ProjeTakipYetkiKaydiDto>> YetkileriGetirAsync(CancellationToken cancellationToken) =>
        await database.ProjeTakipYetkileri.AsNoTracking().OrderBy(x => x.KullaniciAdi)
            .Select(x => new ProjeTakipYetkiKaydiDto(x.KullaniciId, x.KullaniciAdi, x.Goruntule, x.Duzenle,
                x.MaliyetIcmalGor, x.IcmalDuzenle, x.SilArsivle, x.YetkiYonet, x.NotDuzenle))
            .ToArrayAsync(cancellationToken);

    public async Task<ProjeTakipYetkiKaydiDto> YetkiKaydetAsync(string userId, ProjeTakipYetkiKaydetDto dto, CancellationToken cancellationToken)
    {
        userId = userId.Trim();
        if (string.IsNullOrWhiteSpace(userId)) throw new ProjeTakipValidationException("Kullanıcı kimliği zorunludur.");
        var maliyetIcmalGor = dto.MaliyetIcmalGor || dto.IcmalDuzenle;
        var goruntule = dto.Goruntule || dto.Duzenle || maliyetIcmalGor || dto.SilArsivle || dto.YetkiYonet || dto.NotDuzenle;
        var entity = await database.ProjeTakipYetkileri.SingleOrDefaultAsync(x => x.KullaniciId == userId, cancellationToken);
        if (entity?.YetkiYonet == true && !dto.YetkiYonet
            && await database.ProjeTakipYetkileri.CountAsync(x => x.YetkiYonet, cancellationToken) <= 1)
            throw new ProjeTakipConflictException("Sistemde en az bir yetki yöneticisi bulunmalıdır.");
        if (entity is null)
        {
            entity = new ProjeTakipYetkisi { KullaniciId = userId, OlusturulmaUtc = DateTime.UtcNow };
            database.ProjeTakipYetkileri.Add(entity);
        }
        entity.KullaniciAdi = string.IsNullOrWhiteSpace(dto.KullaniciAdi) ? userId : dto.KullaniciAdi.Trim();
        entity.Goruntule = goruntule;
        entity.Duzenle = dto.Duzenle;
        entity.MaliyetIcmalGor = maliyetIcmalGor;
        entity.IcmalDuzenle = dto.IcmalDuzenle;
        entity.SilArsivle = dto.SilArsivle;
        entity.YetkiYonet = dto.YetkiYonet;
        entity.NotDuzenle = dto.NotDuzenle;
        entity.GuncellemeUtc = DateTime.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        await DenetimEkleAsync("YETKI_GUNCELLE", "ProjeTakipYetkisi", entity.Id.ToString(),
            $"{entity.KullaniciAdi} ({entity.KullaniciId}) kullanıcısının Proje Takip izinleri güncellendi.", cancellationToken);
        return new(entity.KullaniciId, entity.KullaniciAdi, entity.Goruntule, entity.Duzenle,
            entity.MaliyetIcmalGor, entity.IcmalDuzenle, entity.SilArsivle, entity.YetkiYonet, entity.NotDuzenle);
    }

    public async Task<IReadOnlyCollection<ProjeTakipDenetimDto>> DenetimKayitlariniGetirAsync(int take, CancellationToken cancellationToken) =>
        await database.ProjeTakipDenetimKayitlari.AsNoTracking().OrderByDescending(x => x.IslemUtc).Take(Math.Clamp(take, 1, 500))
            .Select(x => new ProjeTakipDenetimDto(x.Id, x.KullaniciId, x.KullaniciAdi, x.Islem,
                x.KayitTuru, x.KayitId, x.Aciklama, x.IpAdresi, x.IslemUtc))
            .ToArrayAsync(cancellationToken);

    public async Task DenetimEkleAsync(string operation, string recordType, string recordId, string description, CancellationToken cancellationToken)
    {
        var (userId, userName) = CurrentUser();
        database.ProjeTakipDenetimKayitlari.Add(new ProjeTakipDenetimKaydi
        {
            KullaniciId = userId,
            KullaniciAdi = userName,
            Islem = operation,
            KayitTuru = recordType,
            KayitId = recordId,
            Aciklama = description,
            IpAdresi = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            IslemUtc = DateTime.UtcNow
        });
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task YetkiDogrulaAsync(Func<ProjeTakipYetkileriDto, bool> predicate, string message, CancellationToken cancellationToken)
    {
        if (!predicate(await MevcutYetkilerAsync(cancellationToken))) throw new ProjeTakipForbiddenException(message);
    }

    private (string Id, string Name) CurrentUser()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var id = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(id)) throw new ProjeTakipForbiddenException("Doğrulanmış kullanıcı bilgisi bulunamadı.");
        var name = string.Join(' ', new[] { user?.FindFirstValue(ClaimTypes.Name), user?.FindFirstValue(ClaimTypes.Surname) }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        return (id, string.IsNullOrWhiteSpace(name) ? user?.FindFirstValue(ClaimTypes.GivenName) ?? id : name);
    }

    private static ProjeTakipYetkileriDto Map(ProjeTakipYetkisi value) =>
        new(value.Goruntule, value.Duzenle, value.MaliyetIcmalGor, value.IcmalDuzenle, value.SilArsivle, value.YetkiYonet,
            value.NotDuzenle);
}
