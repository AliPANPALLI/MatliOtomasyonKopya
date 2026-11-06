using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using ProjectTracking.Application.Dtos;
using ProjectTracking.Application.Interfaces;
using ProjectTracking.API.Security;

namespace ProjectTracking.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
[Route("api/proje-takip")]
public sealed class ProjeTakipController(IProjeTakipService service, ProjeTakipAccessService access) : ControllerBase
{
    [HttpGet("bootstrap")]
    public async Task<ProjeTakipBootstrapDto> Bootstrap([FromQuery] bool includeDocuments = true, CancellationToken cancellationToken = default)
    {
        var permissions = await access.MevcutYetkilerAsync(cancellationToken);
        if (!permissions.Goruntule) await access.GoruntulemeDogrulaAsync(cancellationToken);
        var result = await service.BootstrapAsync(includeDocuments, cancellationToken);
        if (includeDocuments && !permissions.MaliyetIcmalGor)
        {
            result = result with
            {
                Documents = result.Documents.Where(x => x.Tur != "icmal")
                    .Select(x => x with { Tutar = null, IcmalKalemleri = [] }).ToArray()
            };
        }
        return result with { Access = permissions };
    }

    [HttpGet("documents")]
    public async Task<IReadOnlyCollection<ProjeDokumaniDto>> Dokumanlar(CancellationToken cancellationToken)
    {
        var permissions = await access.MevcutYetkilerAsync(cancellationToken);
        if (!permissions.Goruntule) await access.GoruntulemeDogrulaAsync(cancellationToken);
        var documents = await service.DokumanlariGetirAsync(cancellationToken);
        if (!permissions.MaliyetIcmalGor)
        {
            documents = documents.Where(x => x.Tur != "icmal")
                .Select(x => x with { Tutar = null, IcmalKalemleri = [] }).ToArray();
        }
        return documents;
    }

    [HttpGet("references")]
    public async Task<ProjeReferanslariDto> Referanslar(CancellationToken cancellationToken)
    {
        await access.GoruntulemeDogrulaAsync(cancellationToken);
        return await service.ReferanslariGetirAsync(cancellationToken);
    }

    [HttpGet("companies")]
    public async Task<IReadOnlyCollection<SirketDto>> Sirketler(CancellationToken cancellationToken)
    {
        await access.GoruntulemeDogrulaAsync(cancellationToken);
        return await service.SirketleriGetirAsync(cancellationToken);
    }

    [HttpGet("facilities")]
    public async Task<IReadOnlyCollection<TesisDto>> Tesisler(CancellationToken cancellationToken)
    {
        await access.GoruntulemeDogrulaAsync(cancellationToken);
        return await service.TesisleriGetirAsync(cancellationToken);
    }

    [HttpGet("permissions")]
    public async Task<IReadOnlyCollection<ProjeTakipYetkiKaydiDto>> Yetkiler(CancellationToken cancellationToken)
    {
        await access.YetkiYonetimiDogrulaAsync(cancellationToken);
        return await access.YetkileriGetirAsync(cancellationToken);
    }

    [HttpPut("permissions/{userId}")]
    public async Task<ProjeTakipYetkiKaydiDto> YetkiKaydet(string userId, ProjeTakipYetkiKaydetDto dto, CancellationToken cancellationToken)
    {
        await access.YetkiYonetimiDogrulaAsync(cancellationToken);
        return await access.YetkiKaydetAsync(userId, dto, cancellationToken);
    }

    [HttpGet("audit")]
    public async Task<IReadOnlyCollection<ProjeTakipDenetimDto>> DenetimKayitlari([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        await access.YetkiYonetimiDogrulaAsync(cancellationToken);
        return await access.DenetimKayitlariniGetirAsync(take, cancellationToken);
    }

    [HttpPost("companies")]
    public async Task<ActionResult<SirketDto>> SirketEkle(SirketKaydetDto dto, CancellationToken cancellationToken)
    {
        await access.DuzenlemeDogrulaAsync(cancellationToken);
        var result = await service.SirketEkleAsync(dto, cancellationToken);
        await access.DenetimEkleAsync("EKLE", "Sirket", result.Id.ToString(), result.Ad, cancellationToken);
        return Created($"api/companies/{result.Id}", result);
    }

    [HttpPut("companies/{id:int}")]
    public async Task<SirketDto> SirketGuncelle(int id, SirketKaydetDto dto, CancellationToken cancellationToken)
    {
        await access.DuzenlemeDogrulaAsync(cancellationToken);
        var result = await service.SirketGuncelleAsync(id, dto, cancellationToken);
        await access.DenetimEkleAsync("GUNCELLE", "Sirket", id.ToString(), result.Ad, cancellationToken);
        return result;
    }

    [HttpDelete("companies/{id:int}")]
    public async Task<IActionResult> SirketSil(int id, CancellationToken cancellationToken)
    {
        await access.SilmeArsivlemeDogrulaAsync(cancellationToken);
        await service.SirketSilAsync(id, cancellationToken);
        await access.DenetimEkleAsync("SIL", "Sirket", id.ToString(), "Şirket silindi.", cancellationToken);
        return NoContent();
    }

    [HttpPost("facilities")]
    public async Task<ActionResult<TesisDto>> TesisEkle(TesisKaydetDto dto, CancellationToken cancellationToken)
    {
        await access.DuzenlemeDogrulaAsync(cancellationToken);
        var result = await service.TesisEkleAsync(dto, cancellationToken);
        await access.DenetimEkleAsync("EKLE", "Tesis", result.Id.ToString(), result.Ad, cancellationToken);
        return Created($"api/facilities/{result.Id}", result);
    }

    [HttpPut("facilities/{id:int}")]
    public async Task<TesisDto> TesisGuncelle(int id, TesisKaydetDto dto, CancellationToken cancellationToken)
    {
        await access.DuzenlemeDogrulaAsync(cancellationToken);
        var result = await service.TesisGuncelleAsync(id, dto, cancellationToken);
        await access.DenetimEkleAsync("GUNCELLE", "Tesis", id.ToString(), result.Ad, cancellationToken);
        return result;
    }

    [HttpDelete("facilities/{id:int}")]
    public async Task<IActionResult> TesisSil(int id, CancellationToken cancellationToken)
    {
        await access.SilmeArsivlemeDogrulaAsync(cancellationToken);
        await service.TesisSilAsync(id, cancellationToken);
        await access.DenetimEkleAsync("SIL", "Tesis", id.ToString(), "Tesis silindi.", cancellationToken);
        return NoContent();
    }

    [HttpPost("titles")]
    public async Task<ActionResult<AdimBasligiDto>> BaslikEkle(AdimBasligiKaydetDto dto, CancellationToken cancellationToken)
    {
        await access.DuzenlemeDogrulaAsync(cancellationToken);
        var result = await service.BaslikEkleAsync(dto, cancellationToken);
        await access.DenetimEkleAsync("EKLE", "AdimBasligi", result.Id.ToString(), result.Ad, cancellationToken);
        return Created($"api/titles/{result.Id}", result);
    }

    [HttpPut("titles/{id:int}")]
    public async Task<AdimBasligiDto> BaslikGuncelle(int id, AdimBasligiKaydetDto dto, CancellationToken cancellationToken)
    {
        await access.DuzenlemeDogrulaAsync(cancellationToken);
        var result = await service.BaslikGuncelleAsync(id, dto, cancellationToken);
        await access.DenetimEkleAsync("GUNCELLE", "AdimBasligi", id.ToString(), result.Ad, cancellationToken);
        return result;
    }

    [HttpDelete("titles/{id:int}")]
    public async Task<IActionResult> BaslikSil(int id, CancellationToken cancellationToken)
    {
        await access.SilmeArsivlemeDogrulaAsync(cancellationToken);
        await service.BaslikSilAsync(id, cancellationToken);
        await access.DenetimEkleAsync("SIL", "AdimBasligi", id.ToString(), "Adım başlığı silindi.", cancellationToken);
        return NoContent();
    }

    [HttpPost("steps")]
    public async Task<ActionResult<IsAdimiDto>> IsAdimiEkle(IsAdimiKaydetDto dto, CancellationToken cancellationToken)
    {
        await access.DuzenlemeDogrulaAsync(cancellationToken);
        var result = await service.IsAdimiEkleAsync(dto, cancellationToken);
        await access.DenetimEkleAsync("EKLE", "IsAdimi", result.Id.ToString(), $"{result.IsNo} · {result.Ad}", cancellationToken);
        return Created($"api/steps/{result.Id}", result);
    }

    [HttpPut("steps/{id:int}")]
    public async Task<IsAdimiDto> IsAdimiGuncelle(int id, IsAdimiKaydetDto dto, CancellationToken cancellationToken)
    {
        await access.DuzenlemeDogrulaAsync(cancellationToken);
        var result = await service.IsAdimiGuncelleAsync(id, dto, cancellationToken);
        await access.DenetimEkleAsync("GUNCELLE", "IsAdimi", id.ToString(), $"{result.IsNo} · {result.Ad}", cancellationToken);
        return result;
    }

    [HttpPatch("steps/{id:int}/notlar")]
    public async Task<IsAdimiDto> IsAdimiNotGuncelle(int id, IsAdimiNotGuncelleDto dto, CancellationToken cancellationToken)
    {
        await access.NotDuzenlemeDogrulaAsync(cancellationToken);
        var result = await service.IsAdimiNotGuncelleAsync(id, dto, cancellationToken);
        await access.DenetimEkleAsync("NOT_GUNCELLE", "IsAdimi", id.ToString(), $"{result.IsNo} · {result.Ad}", cancellationToken);
        return result;
    }

    [HttpPatch("steps/{id:int}/archive")]
    public async Task<IsAdimiDto> ArsivDurumuGuncelle(int id, ArsivDurumuDto dto, CancellationToken cancellationToken)
    {
        await access.SilmeArsivlemeDogrulaAsync(cancellationToken);
        var result = await service.ArsivDurumuGuncelleAsync(id, dto, cancellationToken);
        await access.DenetimEkleAsync(dto.Aktif ? "AKTIFLESTIR" : "ARSIVLE", "IsAdimi", id.ToString(), $"{result.IsNo} · {result.Ad}", cancellationToken);
        return result;
    }

    [HttpDelete("steps/{id:int}")]
    public async Task<IActionResult> IsAdimiSil(int id, CancellationToken cancellationToken)
    {
        await access.SilmeArsivlemeDogrulaAsync(cancellationToken);
        await service.IsAdimiSilAsync(id, cancellationToken);
        await access.DenetimEkleAsync("SIL", "IsAdimi", id.ToString(), "İş adımı silindi.", cancellationToken);
        return NoContent();
    }

    [HttpPost("documents")]
    [RequestSizeLimit(26_214_400)]
    public async Task<ActionResult<ProjeDokumaniDto>> DokumanEkle([FromForm] ProjeDokumaniKaydetDto dto, [FromForm] string? icmalKalemleriJson, IFormFile? dosya, CancellationToken cancellationToken)
    {
        dto = dto with { IcmalKalemleri = IcmalKalemleriniOku(icmalKalemleriJson) };
        if (dto.Tur == "icmal") await access.IcmalDuzenlemeDogrulaAsync(cancellationToken);
        else await access.DuzenlemeDogrulaAsync(cancellationToken);
        var bytes = await DosyaOku(dosya, cancellationToken);
        var result = await service.DokumanEkleAsync(dto, bytes, dosya?.FileName, dosya?.ContentType, cancellationToken);
        await access.DenetimEkleAsync("EKLE", "ProjeDokumani", result.Id.ToString(), $"{result.Tur} · {result.Ad}", cancellationToken);
        return Created($"api/documents/{result.Id}", result);
    }

    [HttpPut("documents/{id:int}")]
    [RequestSizeLimit(26_214_400)]
    public async Task<ProjeDokumaniDto> DokumanGuncelle(int id, [FromForm] ProjeDokumaniKaydetDto dto, [FromForm] string? icmalKalemleriJson, IFormFile? dosya, CancellationToken cancellationToken)
    {
        dto = dto with { IcmalKalemleri = IcmalKalemleriniOku(icmalKalemleriJson) };
        if (dto.Tur == "icmal" || await access.DokumanIcmalMiAsync(id, cancellationToken))
            await access.IcmalDuzenlemeDogrulaAsync(cancellationToken);
        else await access.DuzenlemeDogrulaAsync(cancellationToken);
        var bytes = await DosyaOku(dosya, cancellationToken);
        var result = await service.DokumanGuncelleAsync(id, dto, bytes, dosya?.FileName, dosya?.ContentType, cancellationToken);
        await access.DenetimEkleAsync("GUNCELLE", "ProjeDokumani", id.ToString(), $"{result.Tur} · {result.Ad}", cancellationToken);
        return result;
    }

    [HttpGet("documents/{id:int}/file")]
    public async Task<IActionResult> DokumanIndir(int id, CancellationToken cancellationToken)
    {
        await access.GoruntulemeDogrulaAsync(cancellationToken);
        if (await access.DokumanIcmalMiAsync(id, cancellationToken)) await access.IcmalGoruntulemeDogrulaAsync(cancellationToken);
        var result = await service.DokumanIndirAsync(id, cancellationToken);
        return File(result.Icerik, result.IcerikTipi, result.DosyaAdi);
    }

    [HttpDelete("documents/{id:int}")]
    public async Task<IActionResult> DokumanSil(int id, CancellationToken cancellationToken)
    {
        await access.DokumanSilmeDogrulaAsync(cancellationToken);
        if (await access.DokumanIcmalMiAsync(id, cancellationToken)) await access.IcmalDuzenlemeDogrulaAsync(cancellationToken);
        await service.DokumanSilAsync(id, cancellationToken);
        await access.DenetimEkleAsync("SIL", "ProjeDokumani", id.ToString(), "Doküman silindi.", cancellationToken);
        return NoContent();
    }

    private static async Task<byte[]?> DosyaOku(IFormFile? dosya, CancellationToken cancellationToken)
    {
        if (dosya is null || dosya.Length == 0) return null;
        await using var stream = new MemoryStream();
        await dosya.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }

    private static IReadOnlyCollection<IcmalKalemiKaydetDto>? IcmalKalemleriniOku(string? json)
    {
        if (json is null) return null;
        try
        {
            return JsonSerializer.Deserialize<IReadOnlyCollection<IcmalKalemiKaydetDto>>(
                json, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
        }
        catch (JsonException exception)
        {
            throw new BadHttpRequestException($"İcmal kalemleri geçerli biçimde gönderilmedi: {exception.Message}", exception);
        }
    }
}
