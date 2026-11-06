using Microsoft.EntityFrameworkCore;
using ProjectTracking.Application.Exceptions;
using ProjectTracking.Domain.Entities;
using ProjectTracking.Domain.Interfaces;
using ProjectTracking.Infrastructure.Persistence;

namespace ProjectTracking.Infrastructure.Repositories;

public sealed class ProjeTakipRepository(ProjeTakipDbContext context) : IProjeTakipRepository
{
    public Task<List<Sirket>> SirketleriGetirAsync(CancellationToken cancellationToken) =>
        context.Sirketler.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Sirket?> SirketGetirAsync(int id, CancellationToken cancellationToken) =>
        context.Sirketler.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<Tesis>> TesisleriGetirAsync(CancellationToken cancellationToken) =>
        context.Tesisler.AsNoTracking().ToListAsync(cancellationToken);

    public Task<Tesis?> TesisGetirAsync(int id, CancellationToken cancellationToken) =>
        context.Tesisler.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<AdimBasligi>> BasliklariGetirAsync(CancellationToken cancellationToken) =>
        context.AdimBasliklari.AsNoTracking().ToListAsync(cancellationToken);

    public Task<AdimBasligi?> BaslikGetirAsync(int id, CancellationToken cancellationToken) =>
        context.AdimBasliklari.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<Sorumlu>> SorumlulariGetirAsync(CancellationToken cancellationToken) =>
        context.Sorumlular.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<Sorumlu?> SorumluGetirAsync(string adSoyad, CancellationToken cancellationToken)
    {
        var tracked = context.Sorumlular.Local.FirstOrDefault(x =>
            string.Equals(x.AdSoyad, adSoyad, StringComparison.CurrentCultureIgnoreCase));
        return tracked ?? await context.Sorumlular.FirstOrDefaultAsync(x => x.AdSoyad == adSoyad, cancellationToken);
    }

    public Task<List<IsAdimi>> IsAdimlariniGetirAsync(CancellationToken cancellationToken) =>
        context.IsAdimlari.AsNoTracking()
            .Include(x => x.IsAdimiSorumlular).ThenInclude(x => x.Sorumlu)
            .ToListAsync(cancellationToken);

    public Task<IsAdimi?> IsAdimiGetirAsync(int id, CancellationToken cancellationToken) =>
        context.IsAdimlari
            .Include(x => x.IsAdimiSorumlular).ThenInclude(x => x.Sorumlu)
            .Include(x => x.IcmalKalemleri)
            .Include(x => x.AltAdimlar).ThenInclude(x => x.IcmalKalemleri)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<ProjeDokumani>> DokumanlariGetirAsync(CancellationToken cancellationToken) =>
        context.ProjeDokumanlari.AsNoTracking()
            .Select(x => new ProjeDokumani
            {
                Id = x.Id, ProjeId = x.ProjeId, Tur = x.Tur, Ad = x.Ad, BelgeNo = x.BelgeNo,
                Revizyon = x.Revizyon, Durum = x.Durum, BelgeTarihi = x.BelgeTarihi,
                GecerlilikTarihi = x.GecerlilikTarihi, Tutar = x.Tutar, Aciklama = x.Aciklama,
                DosyaAdi = x.DosyaAdi, IcerikTipi = x.IcerikTipi, DosyaBoyutu = x.DosyaBoyutu,
                OlusturulmaUtc = x.OlusturulmaUtc, GuncellemeUtc = x.GuncellemeUtc,
                IcmalKalemleri = x.IcmalKalemleri.OrderBy(y => y.SiraNo).Select(y => new IcmalKalemi
                {
                    Id = y.Id, ProjeDokumaniId = y.ProjeDokumaniId, IsAdimiId = y.IsAdimiId,
                    SiraNo = y.SiraNo, PozNo = y.PozNo, Tanim = y.Tanim, Birim = y.Birim,
                    Miktar = y.Miktar, BirimFiyat = y.BirimFiyat, ParaBirimi = y.ParaBirimi,
                    Tutar = y.Tutar, Aciklama = y.Aciklama
                }).ToList()
            }).ToListAsync(cancellationToken);

    public Task<ProjeDokumani?> DokumanGetirAsync(int id, bool icerikle, CancellationToken cancellationToken) =>
        icerikle
            ? context.ProjeDokumanlari.Include(x => x.IcmalKalemleri).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            : context.ProjeDokumanlari.Include(x => x.IcmalKalemleri).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<string> SiradakiIsNoAsync(CancellationToken cancellationToken)
    {
        var numaralar = await context.IsAdimlari.AsNoTracking().Select(x => x.IsNo).ToListAsync(cancellationToken);
        var maksimum = numaralar.Select(x => x.StartsWith("K-") && int.TryParse(x[2..], out var value) ? value : 0).DefaultIfEmpty().Max();
        return $"K-{maksimum + 1:0000}";
    }

    public void Ekle<T>(T entity) where T : class => context.Set<T>().Add(entity);
    public void Sil<T>(T entity) where T : class => context.Set<T>().Remove(entity);

    public void SetOriginalRowVersion<T>(T entity, byte[] rowVersion) where T : class =>
        context.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;

    public async Task<int> KaydetAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Formun yüklendiği andaki RowVersion (SetOriginalRowVersion ile
            // set edilmiş) artık DB'deki değerle eşleşmiyor - kayıt siz
            // düzenlerken başka biri tarafından güncellenmiş demektir.
            throw new ProjeTakipConflictException(
                "Bu kayıt siz düzenlerken başka bir kullanıcı tarafından güncellendi. Lütfen sayfayı yenileyip tekrar deneyin.");
        }
    }
}
