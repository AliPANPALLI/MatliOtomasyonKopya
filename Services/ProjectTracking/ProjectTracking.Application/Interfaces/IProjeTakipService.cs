using ProjectTracking.Application.Dtos;

namespace ProjectTracking.Application.Interfaces;

public interface IProjeTakipService
{
    Task<ProjeTakipBootstrapDto> BootstrapAsync(bool includeDocuments, CancellationToken cancellationToken);
    Task<ProjeReferanslariDto> ReferanslariGetirAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProjeDokumaniDto>> DokumanlariGetirAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<SirketDto>> SirketleriGetirAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TesisDto>> TesisleriGetirAsync(CancellationToken cancellationToken);
    Task<SirketDto> SirketEkleAsync(SirketKaydetDto dto, CancellationToken cancellationToken);
    Task<SirketDto> SirketGuncelleAsync(int id, SirketKaydetDto dto, CancellationToken cancellationToken);
    Task SirketSilAsync(int id, CancellationToken cancellationToken);
    Task<TesisDto> TesisEkleAsync(TesisKaydetDto dto, CancellationToken cancellationToken);
    Task<TesisDto> TesisGuncelleAsync(int id, TesisKaydetDto dto, CancellationToken cancellationToken);
    Task TesisSilAsync(int id, CancellationToken cancellationToken);
    Task<AdimBasligiDto> BaslikEkleAsync(AdimBasligiKaydetDto dto, CancellationToken cancellationToken);
    Task<AdimBasligiDto> BaslikGuncelleAsync(int id, AdimBasligiKaydetDto dto, CancellationToken cancellationToken);
    Task BaslikSilAsync(int id, CancellationToken cancellationToken);
    Task<IsAdimiDto> IsAdimiEkleAsync(IsAdimiKaydetDto dto, CancellationToken cancellationToken);
    Task<IsAdimiDto> IsAdimiGuncelleAsync(int id, IsAdimiKaydetDto dto, CancellationToken cancellationToken);
    Task<IsAdimiDto> IsAdimiNotGuncelleAsync(int id, IsAdimiNotGuncelleDto dto, CancellationToken cancellationToken);
    Task<IsAdimiDto> ArsivDurumuGuncelleAsync(int id, ArsivDurumuDto dto, CancellationToken cancellationToken);
    Task IsAdimiSilAsync(int id, CancellationToken cancellationToken);
    Task<ProjeDokumaniDto> DokumanEkleAsync(ProjeDokumaniKaydetDto dto, byte[]? dosya, string? dosyaAdi, string? icerikTipi, CancellationToken cancellationToken);
    Task<ProjeDokumaniDto> DokumanGuncelleAsync(int id, ProjeDokumaniKaydetDto dto, byte[]? dosya, string? dosyaAdi, string? icerikTipi, CancellationToken cancellationToken);
    Task<ProjeDokumaniDosyaDto> DokumanIndirAsync(int id, CancellationToken cancellationToken);
    Task DokumanSilAsync(int id, CancellationToken cancellationToken);
}
