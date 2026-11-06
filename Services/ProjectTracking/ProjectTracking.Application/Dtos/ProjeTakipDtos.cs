namespace ProjectTracking.Application.Dtos;

public sealed record ProjeTakipBootstrapDto(
    string Source,
    DateTime ImportedAt,
    IReadOnlyCollection<string> Responsibles,
    IReadOnlyCollection<SirketDto> Companies,
    IReadOnlyCollection<TesisDto> Facilities,
    IReadOnlyCollection<AdimBasligiDto> Titles,
    IReadOnlyCollection<IsAdimiDto> Steps,
    IReadOnlyCollection<ProjeDokumaniDto> Documents)
{
    public ProjeTakipYetkileriDto Access { get; init; } = ProjeTakipYetkileriDto.Yetkisiz;
}

public sealed record ProjeTakipYetkileriDto(
    bool Goruntule, bool Duzenle, bool MaliyetIcmalGor, bool IcmalDuzenle, bool SilArsivle, bool YetkiYonet,
    bool NotDuzenle = false)
{
    public static ProjeTakipYetkileriDto Yetkisiz => new(false, false, false, false, false, false, false);
    public static ProjeTakipYetkileriDto TamYetki => new(true, true, true, true, true, true, true);
}

public sealed record ProjeTakipYetkiKaydiDto(
    string KullaniciId, string KullaniciAdi, bool Goruntule, bool Duzenle,
    bool MaliyetIcmalGor, bool IcmalDuzenle, bool SilArsivle, bool YetkiYonet, bool NotDuzenle);

public sealed record ProjeTakipYetkiKaydetDto(
    string KullaniciAdi, bool Goruntule, bool Duzenle,
    bool MaliyetIcmalGor, bool IcmalDuzenle, bool SilArsivle, bool YetkiYonet, bool NotDuzenle = false);

public sealed record ProjeTakipDenetimDto(
    long Id, string KullaniciId, string KullaniciAdi, string Islem,
    string KayitTuru, string KayitId, string Aciklama, string IpAdresi, DateTime IslemUtc);

public sealed record SirketDto(int Id, string Ad, bool Aktif, byte[] RowVersion);

public sealed record TesisDto(int Id, int SirketId, string Ad, string Slug, int? IlKodu, int Sira, bool Aktif, byte[] RowVersion);

public sealed record ProjeReferanslariDto(
    IReadOnlyCollection<SirketDto> Companies,
    IReadOnlyCollection<TesisDto> Facilities);

public sealed record AdimBasligiDto(int Id, string Ad, byte[] RowVersion);

public sealed record IsAdimiDto(
    int Id,
    int TesisId,
    int? UstId,
    string IsNo,
    string Ad,
    string Durum,
    string Oncelik,
    DateOnly? Baslangic,
    DateOnly? HedefBitis,
    int Ilerleme,
    string Notlar,
    string Risk,
    string SonrakiAdim,
    IReadOnlyCollection<string> Sorumlular,
    bool Aktif,
    DateOnly? Guncellendi,
    byte[] RowVersion);

public sealed record SirketKaydetDto(string Ad, bool Aktif = true, byte[]? RowVersion = null);

public sealed record TesisKaydetDto(int SirketId, string Ad, int? IlKodu, int Sira, bool Aktif = true, byte[]? RowVersion = null);

public sealed record AdimBasligiKaydetDto(string Ad, byte[]? RowVersion = null);

public sealed record IsAdimiKaydetDto(
    int TesisId,
    int? UstId,
    string Ad,
    string Durum,
    string Oncelik,
    DateOnly? Baslangic,
    DateOnly? HedefBitis,
    int Ilerleme,
    string? Notlar,
    string? Risk,
    string? SonrakiAdim,
    IReadOnlyCollection<string>? Sorumlular,
    bool Aktif = true,
    byte[]? RowVersion = null);

public sealed record ArsivDurumuDto(bool Aktif, byte[]? RowVersion = null);

public sealed record IsAdimiNotGuncelleDto(string? Notlar, byte[]? RowVersion = null);

public sealed record ProjeDokumaniDto(
    int Id, int ProjeId, string Tur, string Ad, string BelgeNo, string Revizyon, string Durum,
    DateOnly? BelgeTarihi, DateOnly? GecerlilikTarihi, decimal? Tutar, string Aciklama,
    string? DosyaAdi, string? IcerikTipi, long DosyaBoyutu, DateTime OlusturulmaUtc, DateTime? GuncellemeUtc,
    IReadOnlyCollection<IcmalKalemiDto> IcmalKalemleri, byte[] RowVersion);

public sealed record ProjeDokumaniKaydetDto(
    int ProjeId, string Tur, string Ad, string? BelgeNo, string? Revizyon, string Durum,
    DateOnly? BelgeTarihi, DateOnly? GecerlilikTarihi, decimal? Tutar, string? Aciklama,
    IReadOnlyCollection<IcmalKalemiKaydetDto>? IcmalKalemleri = null, byte[]? RowVersion = null);

public sealed record IcmalKalemiDto(
    int Id, int ProjeDokumaniId, int? IsAdimiId, int SiraNo, string PozNo, string Tanim,
    string Birim, decimal Miktar, decimal BirimFiyat, string ParaBirimi, decimal Tutar, string Aciklama);

public sealed record IcmalKalemiKaydetDto(
    int? IsAdimiId, int SiraNo, string? PozNo, string Tanim, string Birim,
    decimal Miktar, decimal BirimFiyat, string ParaBirimi, string? Aciklama);

public sealed record ProjeDokumaniDosyaDto(byte[] Icerik, string DosyaAdi, string IcerikTipi);
