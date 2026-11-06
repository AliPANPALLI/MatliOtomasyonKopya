using System.Globalization;
using System.Text;
using ProjectTracking.Application.Dtos;
using ProjectTracking.Application.Exceptions;
using ProjectTracking.Application.Interfaces;
using ProjectTracking.Domain.Entities;
using ProjectTracking.Domain.Interfaces;

namespace ProjectTracking.Application.Services;

public sealed class ProjeTakipService(IProjeTakipRepository repository) : IProjeTakipService
{
    private static readonly HashSet<string> Durumlar =
        ["guncellenecek", "baslamadi", "devam", "beklemede", "tamamlandi", "iptal"];
    private static readonly HashSet<string> Oncelikler = ["dusuk", "orta", "yuksek", "kritik"];
    private static readonly HashSet<string> DokumanTurleri = ["icmal", "sartname", "sozlesme", "hakedis", "proje", "tutanak", "diger"];
    private static readonly HashSet<string> DokumanDurumlari = ["taslak", "incelemede", "onaylandi", "reddedildi", "suresi-doldu"];
    private static readonly HashSet<string> IcmalBirimleri = ["adet", "takım", "set", "paket", "mm", "cm", "m", "m²", "m³", "kg", "ton", "lt", "kW", "kVA", "saat", "gün", "ay"];
    private static readonly HashSet<string> ParaBirimleri = ["TRY", "USD", "EUR", "GBP"];

    public async Task<ProjeTakipBootstrapDto> BootstrapAsync(bool includeDocuments, CancellationToken cancellationToken)
    {
        var sirketler = await repository.SirketleriGetirAsync(cancellationToken);
        var tesisler = await repository.TesisleriGetirAsync(cancellationToken);
        var basliklar = await repository.BasliklariGetirAsync(cancellationToken);
        var sorumlular = await repository.SorumlulariGetirAsync(cancellationToken);
        var adimlar = await repository.IsAdimlariniGetirAsync(cancellationToken);
        // Dökümanlar (ve icmal kalemleri) yalnızca gerçekten ihtiyaç duyulduğunda çekiliyor:
        // özet/gantt ekranları bu veriyi hiç kullanmıyor, tesisler/iş planı ekranları isteği
        // includeDocuments=true ile açıyor. Bu, ilk açılışta gereksiz büyük bir veri setinin
        // taşınmasını engelliyor.
        var dokumanlar = includeDocuments
            ? await repository.DokumanlariGetirAsync(cancellationToken)
            : [];

        return new ProjeTakipBootstrapDto(
            "mssql",
            DateTime.UtcNow,
            sorumlular.Where(x => x.Aktif).OrderBy(x => x.AdSoyad).Select(x => x.AdSoyad).ToArray(),
            sirketler.OrderBy(x => x.Ad).Select(Map).ToArray(),
            tesisler.OrderBy(x => x.Sira).ThenBy(x => x.Ad).Select(Map).ToArray(),
            basliklar.OrderBy(x => x.Ad).Select(Map).ToArray(),
            adimlar.OrderBy(x => x.IsNo).Select(Map).ToArray(),
            dokumanlar.OrderByDescending(x => x.GuncellemeUtc ?? x.OlusturulmaUtc).Select(Map).ToArray());
    }

    public async Task<IReadOnlyCollection<ProjeDokumaniDto>> DokumanlariGetirAsync(CancellationToken cancellationToken)
    {
        var dokumanlar = await repository.DokumanlariGetirAsync(cancellationToken);
        return dokumanlar.OrderByDescending(x => x.GuncellemeUtc ?? x.OlusturulmaUtc).Select(Map).ToArray();
    }

    public async Task<IReadOnlyCollection<SirketDto>> SirketleriGetirAsync(CancellationToken cancellationToken) =>
        (await repository.SirketleriGetirAsync(cancellationToken)).OrderBy(x => x.Ad).Select(Map).ToArray();

    public async Task<IReadOnlyCollection<TesisDto>> TesisleriGetirAsync(CancellationToken cancellationToken) =>
        (await repository.TesisleriGetirAsync(cancellationToken)).OrderBy(x => x.Sira).ThenBy(x => x.Ad).Select(Map).ToArray();

    public async Task<ProjeReferanslariDto> ReferanslariGetirAsync(CancellationToken cancellationToken)
    {
        var sirketler = await repository.SirketleriGetirAsync(cancellationToken);
        var tesisler = await repository.TesisleriGetirAsync(cancellationToken);
        return new ProjeReferanslariDto(
            sirketler.OrderBy(x => x.Ad).Select(Map).ToArray(),
            tesisler.OrderBy(x => x.Sira).ThenBy(x => x.Ad).Select(Map).ToArray());
    }

    public async Task<SirketDto> SirketEkleAsync(SirketKaydetDto dto, CancellationToken cancellationToken)
    {
        var ad = ZorunluMetin(dto.Ad, "Şirket adı");
        var sirketler = await repository.SirketleriGetirAsync(cancellationToken);
        if (sirketler.Any(x => string.Equals(x.Ad, ad, StringComparison.CurrentCultureIgnoreCase)))
            throw new ProjeTakipConflictException("Bu şirket zaten kayıtlı.");
        var entity = new Sirket { Ad = ad, Aktif = dto.Aktif, OlusturulmaUtc = DateTime.UtcNow };
        repository.Ekle(entity);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<SirketDto> SirketGuncelleAsync(int id, SirketKaydetDto dto, CancellationToken cancellationToken)
    {
        var entity = await repository.SirketGetirAsync(id, cancellationToken)
            ?? throw new ProjeTakipNotFoundException("Şirket bulunamadı.");
        var ad = ZorunluMetin(dto.Ad, "Şirket adı");
        var sirketler = await repository.SirketleriGetirAsync(cancellationToken);
        if (sirketler.Any(x => x.Id != id && string.Equals(x.Ad, ad, StringComparison.CurrentCultureIgnoreCase)))
            throw new ProjeTakipConflictException("Bu şirket adı başka bir kayıtta kullanılıyor.");

        entity.Ad = ad;
        entity.Aktif = dto.Aktif;
        entity.GuncellemeUtc = DateTime.UtcNow;
        if (dto.RowVersion is { Length: > 0 }) repository.SetOriginalRowVersion(entity, dto.RowVersion);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task SirketSilAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await repository.SirketGetirAsync(id, cancellationToken)
            ?? throw new ProjeTakipNotFoundException("Şirket bulunamadı.");
        var bagliTesisSayisi = (await repository.TesisleriGetirAsync(cancellationToken)).Count(x => x.SirketId == id);
        if (bagliTesisSayisi > 0)
            throw new ProjeTakipConflictException($"Bu şirkete bağlı {bagliTesisSayisi} tesis var. Önce tesisleri başka bir şirkete taşıyın veya silin.");

        repository.Sil(entity);
        await repository.KaydetAsync(cancellationToken);
    }

    public async Task<TesisDto> TesisEkleAsync(TesisKaydetDto dto, CancellationToken cancellationToken)
    {
        var ad = ZorunluMetin(dto.Ad, "Tesis adı");
        await SirketiDogrulaAsync(dto.SirketId, cancellationToken);
        IlKodunuDogrula(dto.IlKodu);
        var tesisler = await repository.TesisleriGetirAsync(cancellationToken);
        TesisTekrariniDogrula(tesisler, ad);
        var entity = new Tesis
        {
            SirketId = dto.SirketId,
            Ad = ad,
            Slug = BenzersizSlug(ad, tesisler),
            IlKodu = dto.IlKodu,
            Sira = dto.Sira,
            Aktif = dto.Aktif,
            OlusturulmaUtc = DateTime.UtcNow
        };
        repository.Ekle(entity);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<TesisDto> TesisGuncelleAsync(int id, TesisKaydetDto dto, CancellationToken cancellationToken)
    {
        var entity = await repository.TesisGetirAsync(id, cancellationToken)
            ?? throw new ProjeTakipNotFoundException("Tesis bulunamadı.");
        var ad = ZorunluMetin(dto.Ad, "Tesis adı");
        await SirketiDogrulaAsync(dto.SirketId, cancellationToken);
        IlKodunuDogrula(dto.IlKodu);
        var tesisler = await repository.TesisleriGetirAsync(cancellationToken);
        TesisTekrariniDogrula(tesisler, ad, id);
        if (!string.Equals(entity.Ad, ad, StringComparison.CurrentCulture))
            entity.Slug = BenzersizSlug(ad, tesisler.Where(x => x.Id != id));
        entity.Ad = ad;
        entity.SirketId = dto.SirketId;
        entity.IlKodu = dto.IlKodu;
        entity.Sira = dto.Sira;
        entity.Aktif = dto.Aktif;
        entity.GuncellemeUtc = DateTime.UtcNow;
        if (dto.RowVersion is { Length: > 0 }) repository.SetOriginalRowVersion(entity, dto.RowVersion);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task TesisSilAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await repository.TesisGetirAsync(id, cancellationToken)
            ?? throw new ProjeTakipNotFoundException("Tesis bulunamadı.");
        var adimlar = await repository.IsAdimlariniGetirAsync(cancellationToken);
        if (adimlar.Any(x => x.TesisId == id))
            throw new ProjeTakipConflictException("Bu tesise bağlı iş adımları var. Tesisi pasife alabilirsiniz.");
        repository.Sil(entity);
        await repository.KaydetAsync(cancellationToken);
    }

    public async Task<AdimBasligiDto> BaslikEkleAsync(AdimBasligiKaydetDto dto, CancellationToken cancellationToken)
    {
        var ad = ZorunluMetin(dto.Ad, "Başlık");
        var basliklar = await repository.BasliklariGetirAsync(cancellationToken);
        BaslikTekrariniDogrula(basliklar, ad);
        var entity = new AdimBasligi { Ad = ad, OlusturulmaUtc = DateTime.UtcNow };
        repository.Ekle(entity);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<AdimBasligiDto> BaslikGuncelleAsync(int id, AdimBasligiKaydetDto dto, CancellationToken cancellationToken)
    {
        var entity = await repository.BaslikGetirAsync(id, cancellationToken)
            ?? throw new ProjeTakipNotFoundException("Başlık bulunamadı.");
        var ad = ZorunluMetin(dto.Ad, "Başlık");
        BaslikTekrariniDogrula(await repository.BasliklariGetirAsync(cancellationToken), ad, id);
        entity.Ad = ad;
        entity.GuncellemeUtc = DateTime.UtcNow;
        if (dto.RowVersion is { Length: > 0 }) repository.SetOriginalRowVersion(entity, dto.RowVersion);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task BaslikSilAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await repository.BaslikGetirAsync(id, cancellationToken)
            ?? throw new ProjeTakipNotFoundException("Başlık bulunamadı.");
        repository.Sil(entity);
        await repository.KaydetAsync(cancellationToken);
    }

    public async Task<IsAdimiDto> IsAdimiEkleAsync(IsAdimiKaydetDto dto, CancellationToken cancellationToken)
    {
        await IsAdimiDogrulaAsync(dto, null, cancellationToken);
        var entity = new IsAdimi
        {
            IsNo = await repository.SiradakiIsNoAsync(cancellationToken),
            OlusturulmaUtc = DateTime.UtcNow
        };
        DtoUygula(entity, dto);
        repository.Ekle(entity);
        await SorumlulariUygulaAsync(entity, dto.Sorumlular, cancellationToken);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IsAdimiDto> IsAdimiGuncelleAsync(int id, IsAdimiKaydetDto dto, CancellationToken cancellationToken)
    {
        var entity = await repository.IsAdimiGetirAsync(id, cancellationToken)
            ?? throw new ProjeTakipNotFoundException("İş adımı bulunamadı.");
        await IsAdimiDogrulaAsync(dto, id, cancellationToken);
        var ustProjeTesisiDegisti = entity.UstId is null && entity.TesisId != dto.TesisId;
        DtoUygula(entity, dto);
        entity.GuncellemeUtc = DateTime.UtcNow;
        if (ustProjeTesisiDegisti)
        {
            foreach (var altAdim in entity.AltAdimlar)
            {
                altAdim.TesisId = dto.TesisId;
                altAdim.GuncellemeUtc = DateTime.UtcNow;
            }
        }
        await SorumlulariUygulaAsync(entity, dto.Sorumlular, cancellationToken);
        if (dto.RowVersion is { Length: > 0 }) repository.SetOriginalRowVersion(entity, dto.RowVersion);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IsAdimiDto> IsAdimiNotGuncelleAsync(int id, IsAdimiNotGuncelleDto dto, CancellationToken cancellationToken)
    {
        var entity = await repository.IsAdimiGetirAsync(id, cancellationToken)
            ?? throw new ProjeTakipNotFoundException("İş adımı bulunamadı.");
        entity.Notlar = (dto.Notlar ?? string.Empty).Trim();
        entity.GuncellemeUtc = DateTime.UtcNow;
        if (dto.RowVersion is { Length: > 0 }) repository.SetOriginalRowVersion(entity, dto.RowVersion);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<IsAdimiDto> ArsivDurumuGuncelleAsync(int id, ArsivDurumuDto dto, CancellationToken cancellationToken)
    {
        var entity = await repository.IsAdimiGetirAsync(id, cancellationToken)
            ?? throw new ProjeTakipNotFoundException("İş adımı bulunamadı.");
        entity.Aktif = dto.Aktif;
        entity.GuncellemeUtc = DateTime.UtcNow;
        if (dto.RowVersion is { Length: > 0 }) repository.SetOriginalRowVersion(entity, dto.RowVersion);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task IsAdimiSilAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await repository.IsAdimiGetirAsync(id, cancellationToken);
        if (entity is null) return;
        if (entity.UstId is null && entity.AltAdimlar.Any(x => x.Aktif))
            throw new ProjeTakipConflictException("Bu projenin aktif alt adımları var. Önce alt adımları arşivleyin veya silin.");
        foreach (var kalem in entity.IcmalKalemleri) kalem.IsAdimiId = null;
        foreach (var child in entity.AltAdimlar)
            foreach (var kalem in child.IcmalKalemleri) kalem.IsAdimiId = null;
        foreach (var child in entity.AltAdimlar.ToArray()) repository.Sil(child);
        repository.Sil(entity);
        await repository.KaydetAsync(cancellationToken);
    }

    public async Task<ProjeDokumaniDto> DokumanEkleAsync(ProjeDokumaniKaydetDto dto, byte[]? dosya, string? dosyaAdi, string? icerikTipi, CancellationToken cancellationToken)
    {
        var dokumanSahibi = await DokumanDogrulaAsync(dto, dosya, cancellationToken);
        var entity = new ProjeDokumani { OlusturulmaUtc = DateTime.UtcNow };
        DokumanDtoUygula(entity, dto);
        DosyaUygula(entity, dosya, dosyaAdi, icerikTipi);
        IcmalKalemleriniUygula(entity, dto, dokumanSahibi.UstId is not null ? dokumanSahibi.Id : null);
        repository.Ekle(entity);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ProjeDokumaniDto> DokumanGuncelleAsync(int id, ProjeDokumaniKaydetDto dto, byte[]? dosya, string? dosyaAdi, string? icerikTipi, CancellationToken cancellationToken)
    {
        var entity = await repository.DokumanGetirAsync(id, true, cancellationToken)
            ?? throw new ProjeTakipNotFoundException("Doküman bulunamadı.");
        var dokumanSahibi = await DokumanDogrulaAsync(dto, dosya, cancellationToken);
        DokumanDtoUygula(entity, dto);
        if (dosya is not null) DosyaUygula(entity, dosya, dosyaAdi, icerikTipi);
        IcmalKalemleriniUygula(entity, dto, dokumanSahibi.UstId is not null ? dokumanSahibi.Id : null);
        entity.GuncellemeUtc = DateTime.UtcNow;
        if (dto.RowVersion is { Length: > 0 }) repository.SetOriginalRowVersion(entity, dto.RowVersion);
        await repository.KaydetAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ProjeDokumaniDosyaDto> DokumanIndirAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await repository.DokumanGetirAsync(id, true, cancellationToken)
            ?? throw new ProjeTakipNotFoundException("Doküman bulunamadı.");
        if (entity.DosyaIcerigi is null || string.IsNullOrWhiteSpace(entity.DosyaAdi))
            throw new ProjeTakipNotFoundException("Bu dokümana yüklenmiş dosya bulunmuyor.");
        return new ProjeDokumaniDosyaDto(entity.DosyaIcerigi, entity.DosyaAdi, entity.IcerikTipi ?? "application/octet-stream");
    }

    public async Task DokumanSilAsync(int id, CancellationToken cancellationToken)
    {
        var entity = await repository.DokumanGetirAsync(id, true, cancellationToken);
        if (entity is null) return;
        repository.Sil(entity);
        await repository.KaydetAsync(cancellationToken);
    }

    private async Task IsAdimiDogrulaAsync(IsAdimiKaydetDto dto, int? id, CancellationToken cancellationToken)
    {
        _ = ZorunluMetin(dto.Ad, "Adım adı");
        if (dto.Ilerleme is < 0 or > 100) throw new ProjeTakipValidationException("İlerleme 0 ile 100 arasında olmalıdır.");
        if (!Durumlar.Contains(dto.Durum)) throw new ProjeTakipValidationException("Geçersiz durum değeri.");
        if (!Oncelikler.Contains(dto.Oncelik)) throw new ProjeTakipValidationException("Geçersiz öncelik değeri.");
        if (dto.Baslangic.HasValue && dto.HedefBitis.HasValue && dto.HedefBitis < dto.Baslangic)
            throw new ProjeTakipValidationException("Hedef bitiş başlangıç tarihinden önce olamaz.");
        if (await repository.TesisGetirAsync(dto.TesisId, cancellationToken) is null)
            throw new ProjeTakipValidationException("Tesis seçilmelidir.");
        if (dto.UstId is null) return;
        if (dto.UstId == id) throw new ProjeTakipValidationException("Bir adım kendi üst adımı olamaz.");
        var parent = await repository.IsAdimiGetirAsync(dto.UstId.Value, cancellationToken);
        if (parent is null || parent.UstId is not null)
            throw new ProjeTakipValidationException("Alt adım yalnızca bir üst adıma bağlanabilir.");
        if (parent.TesisId != dto.TesisId)
            throw new ProjeTakipValidationException("Alt adım ve üst adım aynı tesiste olmalıdır.");
    }

    private async Task SorumlulariUygulaAsync(IsAdimi entity, IReadOnlyCollection<string>? adlar, CancellationToken cancellationToken)
    {
        var istenenler = (adlar ?? []).Select(x => x.Trim()).Where(x => x.Length > 0)
            .Distinct(StringComparer.CurrentCultureIgnoreCase).ToArray();
        foreach (var mevcut in entity.IsAdimiSorumlular.ToArray())
            if (!istenenler.Contains(mevcut.Sorumlu.AdSoyad, StringComparer.CurrentCultureIgnoreCase))
                repository.Sil(mevcut);
        foreach (var ad in istenenler)
        {
            if (entity.IsAdimiSorumlular.Any(x => string.Equals(x.Sorumlu.AdSoyad, ad, StringComparison.CurrentCultureIgnoreCase))) continue;
            var sorumlu = await repository.SorumluGetirAsync(ad, cancellationToken);
            if (sorumlu is null)
            {
                sorumlu = new Sorumlu { AdSoyad = ad, Aktif = true };
                repository.Ekle(sorumlu);
            }
            entity.IsAdimiSorumlular.Add(new IsAdimiSorumlu { IsAdimi = entity, Sorumlu = sorumlu, AtanmaUtc = DateTime.UtcNow });
        }
    }

    private static void DtoUygula(IsAdimi entity, IsAdimiKaydetDto dto)
    {
        entity.TesisId = dto.TesisId;
        entity.UstId = dto.UstId;
        entity.Ad = dto.Ad.Trim();
        entity.Durum = dto.Durum;
        entity.Oncelik = dto.Oncelik;
        entity.Baslangic = dto.Baslangic;
        entity.HedefBitis = dto.HedefBitis;
        entity.Ilerleme = (byte)dto.Ilerleme;
        entity.Notlar = Temizle(dto.Notlar);
        entity.Risk = Temizle(dto.Risk);
        entity.SonrakiAdim = Temizle(dto.SonrakiAdim);
        entity.Aktif = dto.Aktif;
    }

    private async Task<IsAdimi> DokumanDogrulaAsync(ProjeDokumaniKaydetDto dto, byte[]? dosya, CancellationToken cancellationToken)
    {
        _ = ZorunluMetin(dto.Ad, "Doküman adı");
        if (!DokumanTurleri.Contains(dto.Tur)) throw new ProjeTakipValidationException("Geçersiz doküman türü.");
        if (!DokumanDurumlari.Contains(dto.Durum)) throw new ProjeTakipValidationException("Geçersiz doküman durumu.");
        var dokumanSahibi = await repository.IsAdimiGetirAsync(dto.ProjeId, cancellationToken)
            ?? throw new ProjeTakipValidationException("Dokümanın bağlanacağı proje veya alt adım bulunamadı.");
        if (dto.Tutar < 0) throw new ProjeTakipValidationException("Tutar negatif olamaz.");
        if (dosya?.LongLength > 25 * 1024 * 1024) throw new ProjeTakipValidationException("Dosya boyutu 25 MB'ı aşamaz.");
        if (dto.Tur != "icmal" && dto.IcmalKalemleri?.Count > 0)
            throw new ProjeTakipValidationException("İcmal kalemleri yalnızca icmal türündeki dokümanlara eklenebilir.");
        if (dto.IcmalKalemleri is null) return dokumanSahibi;

        var adimlar = await repository.IsAdimlariniGetirAsync(cancellationToken);
        foreach (var kalem in dto.IcmalKalemleri)
        {
            _ = ZorunluMetin(kalem.Tanim, "Kalem tanımı");
            if (!IcmalBirimleri.Contains(kalem.Birim))
                throw new ProjeTakipValidationException($"‘{kalem.Tanim}’ kaleminin birimi standart listede değil.");
            if (!ParaBirimleri.Contains(kalem.ParaBirimi))
                throw new ProjeTakipValidationException($"‘{kalem.Tanim}’ kaleminin para birimi desteklenmiyor.");
            if (kalem.Miktar <= 0) throw new ProjeTakipValidationException($"‘{kalem.Tanim}’ kaleminin miktarı sıfırdan büyük olmalıdır.");
            if (kalem.BirimFiyat < 0) throw new ProjeTakipValidationException($"‘{kalem.Tanim}’ kaleminin birim fiyatı negatif olamaz.");
            if (kalem.IsAdimiId.HasValue)
            {
                var gecerliBaglanti = dokumanSahibi.UstId is null
                    ? adimlar.Any(x => x.Id == kalem.IsAdimiId && x.UstId == dokumanSahibi.Id)
                    : kalem.IsAdimiId == dokumanSahibi.Id;
                if (!gecerliBaglanti)
                    throw new ProjeTakipValidationException($"‘{kalem.Tanim}’ kalemi yalnızca dokümanın ait olduğu alt adıma bağlanabilir.");
            }
        }
        return dokumanSahibi;
    }

    private static void DokumanDtoUygula(ProjeDokumani entity, ProjeDokumaniKaydetDto dto)
    {
        entity.ProjeId = dto.ProjeId;
        entity.Tur = dto.Tur;
        entity.Ad = dto.Ad.Trim();
        entity.BelgeNo = Temizle(dto.BelgeNo);
        entity.Revizyon = Temizle(dto.Revizyon);
        entity.Durum = dto.Durum;
        entity.BelgeTarihi = dto.BelgeTarihi;
        entity.GecerlilikTarihi = dto.GecerlilikTarihi;
        entity.Tutar = dto.Tutar;
        entity.Aciklama = Temizle(dto.Aciklama);
    }

    private static void DosyaUygula(ProjeDokumani entity, byte[]? dosya, string? dosyaAdi, string? icerikTipi)
    {
        if (dosya is null) return;
        entity.DosyaIcerigi = dosya;
        entity.DosyaBoyutu = dosya.LongLength;
        entity.DosyaAdi = Path.GetFileName(dosyaAdi ?? "dokuman");
        entity.IcerikTipi = string.IsNullOrWhiteSpace(icerikTipi) ? "application/octet-stream" : icerikTipi;
    }

    private void IcmalKalemleriniUygula(ProjeDokumani entity, ProjeDokumaniKaydetDto dto, int? zorunluAltAdimId)
    {
        if (dto.Tur == "icmal" && dto.IcmalKalemleri is null) return;

        foreach (var mevcut in entity.IcmalKalemleri.ToArray()) repository.Sil(mevcut);
        entity.IcmalKalemleri.Clear();
        if (dto.Tur != "icmal") return;

        var sira = 1;
        foreach (var kalem in dto.IcmalKalemleri ?? [])
        {
            var miktar = decimal.Round(kalem.Miktar, 3, MidpointRounding.AwayFromZero);
            var birimFiyat = decimal.Round(kalem.BirimFiyat, 4, MidpointRounding.AwayFromZero);
            entity.IcmalKalemleri.Add(new IcmalKalemi
            {
                IsAdimiId = zorunluAltAdimId ?? kalem.IsAdimiId,
                SiraNo = sira++,
                PozNo = Temizle(kalem.PozNo),
                Tanim = kalem.Tanim.Trim(),
                Birim = kalem.Birim,
                Miktar = miktar,
                BirimFiyat = birimFiyat,
                ParaBirimi = kalem.ParaBirimi,
                Tutar = decimal.Round(miktar * birimFiyat, 2, MidpointRounding.AwayFromZero),
                Aciklama = Temizle(kalem.Aciklama),
                OlusturulmaUtc = DateTime.UtcNow
            });
        }
    }

    private static SirketDto Map(Sirket x) => new(x.Id, x.Ad, x.Aktif, x.RowVersion);
    private static TesisDto Map(Tesis x) => new(x.Id, x.SirketId, x.Ad, x.Slug, x.IlKodu, x.Sira, x.Aktif, x.RowVersion);
    private static AdimBasligiDto Map(AdimBasligi x) => new(x.Id, x.Ad, x.RowVersion);
    private static IsAdimiDto Map(IsAdimi x) => new(
        x.Id, x.TesisId, x.UstId, x.IsNo, x.Ad, x.Durum, x.Oncelik, x.Baslangic, x.HedefBitis,
        x.Ilerleme, x.Notlar, x.Risk, x.SonrakiAdim,
        x.IsAdimiSorumlular.Select(y => y.Sorumlu.AdSoyad).OrderBy(y => y).ToArray(), x.Aktif,
        DateOnly.FromDateTime(x.GuncellemeUtc ?? x.OlusturulmaUtc), x.RowVersion);
    private static ProjeDokumaniDto Map(ProjeDokumani x) => new(
        x.Id, x.ProjeId, x.Tur, x.Ad, x.BelgeNo, x.Revizyon, x.Durum, x.BelgeTarihi,
        x.GecerlilikTarihi, x.Tutar, x.Aciklama, x.DosyaAdi, x.IcerikTipi, x.DosyaBoyutu,
        x.OlusturulmaUtc, x.GuncellemeUtc,
        x.IcmalKalemleri.OrderBy(y => y.SiraNo).Select(Map).ToArray(), x.RowVersion);
    private static IcmalKalemiDto Map(IcmalKalemi x) => new(
        x.Id, x.ProjeDokumaniId, x.IsAdimiId, x.SiraNo, x.PozNo, x.Tanim, x.Birim,
        x.Miktar, x.BirimFiyat, x.ParaBirimi, x.Tutar, x.Aciklama);

    private static string ZorunluMetin(string? value, string alan)
    {
        var result = value?.Trim();
        if (string.IsNullOrWhiteSpace(result)) throw new ProjeTakipValidationException($"{alan} boş bırakılamaz.");
        return result;
    }

    private static string Temizle(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static void IlKodunuDogrula(int? ilKodu)
    {
        if (ilKodu is null or < 1 or > 81) throw new ProjeTakipValidationException("Geçerli bir il seçilmelidir.");
    }

    private async Task SirketiDogrulaAsync(int sirketId, CancellationToken cancellationToken)
    {
        var sirket = await repository.SirketGetirAsync(sirketId, cancellationToken);
        if (sirket is null || !sirket.Aktif)
            throw new ProjeTakipValidationException("Aktif bir şirket seçilmelidir.");
    }

    private static void TesisTekrariniDogrula(IEnumerable<Tesis> items, string ad, int? haricId = null)
    {
        if (items.Any(x => x.Id != haricId && string.Equals(x.Ad, ad, StringComparison.CurrentCultureIgnoreCase)))
            throw new ProjeTakipConflictException("Bu tesis zaten kayıtlı.");
    }

    private static void BaslikTekrariniDogrula(IEnumerable<AdimBasligi> items, string ad, int? haricId = null)
    {
        if (items.Any(x => x.Id != haricId && string.Equals(x.Ad, ad, StringComparison.CurrentCultureIgnoreCase)))
            throw new ProjeTakipConflictException("Bu başlık zaten kayıtlı.");
    }

    private static string BenzersizSlug(string ad, IEnumerable<Tesis> tesisler)
    {
        var normalized = ad.ToLower(new CultureInfo("tr-TR")).Normalize(NormalizationForm.FormD);
        var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var root = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        root = root.Replace('ı', 'i');
        if (root.Length == 0) root = "tesis";
        var used = tesisler.Select(x => x.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var slug = root;
        for (var suffix = 2; used.Contains(slug); suffix++) slug = $"{root}-{suffix}";
        return slug;
    }
}
