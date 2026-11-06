using ClosedXML.Excel;
using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.RequestModels;
using PoultryHouseScada.Application.Services;
using PoultryHouseScada.Domain.Models.Excel;

namespace PoultryHouseScada.Infrastructure.Excel
{
    public sealed class VGYumurtlamaExcelSourceService
        : IVGYumurtlamaExcelSourceService
    {
        private readonly IVGExcelImportService _gunlukYemHesaplariImportService;

        private readonly IVGVeriGirisTablosuExcelReader _veriGirisTablosuReader;

        private readonly IVGKumesTasnifExcelReader _tasnifReader;

        public VGYumurtlamaExcelSourceService(
            IVGExcelImportService gunlukYemHesaplariImportService,
            IVGVeriGirisTablosuExcelReader veriGirisTablosuReader,
            IVGKumesTasnifExcelReader tasnifReader)
        {
            _gunlukYemHesaplariImportService = gunlukYemHesaplariImportService;
            _veriGirisTablosuReader = veriGirisTablosuReader;
            _tasnifReader = tasnifReader;
        }

        public async Task<VGYumurtlamaExcelSourceResult> ReadAsync(
            Stream primaryStream,
            string primaryFileName,
            Stream? tasnifStream,
            string? tasnifFileName,
            int? year,
            CancellationToken cancellationToken = default)
        {
            byte[] primaryBytes;

            using (var buffer = new MemoryStream())
            {
                await primaryStream.CopyToAsync(buffer, cancellationToken);
                primaryBytes = buffer.ToArray();
            }

            var result = new VGYumurtlamaExcelSourceResult();

            if (IsVeriGirisTablosuFormat(primaryBytes))
            {
                using var stream = new MemoryStream(primaryBytes);

                var veriGirisResult =
                    await _veriGirisTablosuReader.ReadAsync(
                        stream,
                        primaryFileName,
                        year,
                        cancellationToken);

                result.Data = veriGirisResult.Data;
                result.Warnings.AddRange(veriGirisResult.Warnings);

                // Bu formatta sağlam/kırık/kirli kırılımı yok; yalnızca
                // ham yumurta adedi var. Kırılım gerektiren hesaplarda
                // (VGYumurtlamaService.Calculate) bu adet toplam olarak
                // kullanılabilsin diye ayrıca taşınmasına gerek yok —
                // VGExcelKumesGunlukData.YumurtaAdeti zaten dolu.

                return result;
            }

            using (var stream = new MemoryStream(primaryBytes))
            {
                var gunlukYemHesaplariResult =
                    await _gunlukYemHesaplariImportService.ImportAsync(
                        new VGExcelImportRequest
                        {
                            FileStream = stream,
                            FileName = primaryFileName,
                            Year = year
                        },
                        cancellationToken);

                var primaryData = gunlukYemHesaplariResult.Data
                    .Select(x => new VGExcelKumesGunlukData
                    {
                        Tarih = x.Tarih,
                        KumesNo = x.KumesNo,
                        Olum = x.Olum,
                        YumurtaAdeti = x.YumurtaAdeti,
                        YumurtaGramaji = x.YumurtaGramaji,
                        CanliAgirlik = x.CanliAgirlik,
                        YemTuketim = x.YemTuketim,
                        YemTuketimHayvanBasi = x.YemTuketimHayvanBasi,
                        SuTuketim = x.SuTuketim,
                        Randiman = x.Randiman,
                        VbapRandiman = x.VbapRandiman,
                        Uygulama = x.Uygulama,
                        SaglamYumurta = x.SaglamYumurta,
                        KirikYumurta = x.KirikYumurta,
                        KirliYumurta = x.KirliYumurta,
                        KucukKirik = x.KucukKirik,
                        Fire = x.Fire,
                        GizliCatlak = x.GizliCatlak
                    })
                    .ToList();

                result.Warnings.AddRange(gunlukYemHesaplariResult.Warnings);

                if (tasnifStream != null && !string.IsNullOrWhiteSpace(tasnifFileName))
                {
                    var tasnifResult =
                        await _tasnifReader.ReadAsync(
                            tasnifStream,
                            tasnifFileName,
                            year,
                            cancellationToken);

                    result.Warnings.AddRange(tasnifResult.Warnings);

                    result.Data = VGExcelKumesGunlukMerger.Merge(
                        primaryData,
                        tasnifResult.Data);
                }
                else
                {
                    result.Data = primaryData;
                }
            }

            return result;
        }

        private static bool IsVeriGirisTablosuFormat(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            using var workbook = new XLWorkbook(stream);

            return VGVeriGirisTablosuExcelReader.Matches(workbook);
        }
    }
}
