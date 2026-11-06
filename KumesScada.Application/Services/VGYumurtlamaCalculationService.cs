using KumesScada.Application.Interfaces;
using KumesScada.Application.RequestModels;
using KumesScada.Application.ViewModels;

namespace KumesScada.Application.Services
{
    public sealed class VGYumurtlamaCalculationService
        : IVGYumurtlamaCalculationService
    {
        private readonly IVGYumurtlamaExcelSourceService _sourceService;

        private readonly IVGYumurtlamaService _calculator;

        public VGYumurtlamaCalculationService(
            IVGYumurtlamaExcelSourceService sourceService,
            IVGYumurtlamaService calculator)
        {
            _sourceService = sourceService;
            _calculator = calculator;
        }

        public async Task<VGYumurtlamaViewModel> CalculateAsync(
            Stream yemHesaplariStream,
            string yemHesaplariFileName,
            Stream? tasnifStream,
            string? tasnifFileName,
            int year,
            string kumesNo,
            string? irk,
            DateTime kumeseGirisTarihi,
            decimal baslangicHayvanSayisi,
            string? icSiparisNo,
            int? girisYasiGun = null,
            CancellationToken cancellationToken = default)
        {
            var source = await _sourceService.ReadAsync(
                yemHesaplariStream,
                yemHesaplariFileName,
                tasnifStream,
                tasnifFileName,
                year,
                cancellationToken);

            var data = source.Data
                .Where(x =>
                    x.Tarih.Year == year &&
                    string.Equals(
                        x.KumesNo,
                        kumesNo,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Tarih)
                .ToList();

            if (data.Count == 0)
            {
                throw new Exception(
                    $"{kumesNo} kümesi için {year} yılına ait veri bulunamadı.");
            }

            var calculateRequest = new VGYumurtlamaRequest
            {
                KumesNo = kumesNo,
                Irk = irk,
                KumeseGirisTarihi = kumeseGirisTarihi,
                BaslangicHayvanSayisi = baslangicHayvanSayisi,
                IcSiparisNo = icSiparisNo,
                GirisYasiGun = girisYasiGun
            };

            foreach (var item in data)
            {
                calculateRequest.Veriler.Add(
                    new VGYumurtlamaKaynakSatirRequest
                    {
                        Tarih = item.Tarih,
                        GunlukOlum = item.Olum,
                        KabukDirenci = item.KabukDirenci,
                        GizliCatlak = item.GizliCatlak,
                        SaglamYumurta = item.SaglamYumurta,
                        KirikYumurta = item.KirikYumurta,
                        KirliYumurta = item.KirliYumurta,
                        KucukKirik = item.KucukKirik,
                        Fire = item.Fire,
                        ToplamYumurtaAdedi = item.YumurtaAdeti,
                        YumurtaGramaji = item.YumurtaGramaji,
                        CanliAgirlik = item.CanliAgirlik,
                        YemKg = item.YemTuketim,
                        SuLitre = item.SuTuketim,
                        Hu = null,
                        TekSiraRandiman = item.Randiman,
                        VbapRandiman = item.VbapRandiman,
                        Uygulama = item.Uygulama
                    });
            }

            return _calculator.Calculate(calculateRequest);
        }
    }
}
