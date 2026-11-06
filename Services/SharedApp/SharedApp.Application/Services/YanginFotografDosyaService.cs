using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels.RequestModel;
using System.Text;
using MatliOtomasyon.Shared.Application.Constants;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

namespace SharedApp.Application.Services
{
    public sealed class YanginFotografDosyaService : IYanginFotografDosyaService
    {
        private static readonly IReadOnlyDictionary<string, string> IzinVerilenDosyaTipleri =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["image/jpeg"] = ".jpg",
                ["image/jpg"] = ".jpg",
                ["image/png"] = ".png",
                ["image/webp"] = ".webp"
            };

        //private readonly YanginFotografOptions _options;
        private readonly ILogger<YanginFotografDosyaService> _logger;

        public YanginFotografDosyaService(/*IOptions<YanginFotografOptions> options,*/ ILogger<YanginFotografDosyaService> logger)
        {
            _logger = logger;
            if (string.IsNullOrWhiteSpace(ApiUrls.ViyolYanginDosyalarPath))
            {
                throw new InvalidOperationException("Yangın fotoğraf ana klasörü tanımlanmamış.");
            }
        }

        public async Task<KaydedilenFotografBilgisi> KaydetAsync(string base64Veri, string? contentType, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(base64Veri))
            {
                throw new ArgumentException("Fotoğraf Base64 verisi boş olamaz.", nameof(base64Veri));
            }

            var ayristirilanVeri = Base64VerisiniAyristir(base64Veri, contentType);

            var temizBase64 = BosluklariTemizle(ayristirilanVeri.Base64Veri);

            if (string.IsNullOrWhiteSpace(temizBase64))
                throw new ArgumentException("Fotoğraf Base64 verisi boş.");

            var maksimumBoyutByte = ApplicationConstants.MaksimumDosyaBoyutuMb * 1024L * 1024L;

            /*
             * byte[] oluşturmadan önce yaklaşık/teorik gerçek dosya
             * boyutunu kontrol ediyoruz.
             */
            var hesaplananBoyut = Base64DosyaBoyutunuHesapla(temizBase64);

            if (hesaplananBoyut > maksimumBoyutByte)
            {
                throw new ArgumentException($"Fotoğraf boyutu en fazla " + $"{ApplicationConstants.MaksimumDosyaBoyutuMb} MB olabilir. " + $"Gönderilen dosya yaklaşık " + $"{hesaplananBoyut / 1024d / 1024d:F2} MB.");
            }

            byte[] fotografVerisi;

            try
            {
                fotografVerisi = Convert.FromBase64String(temizBase64);
            }
            catch (FormatException exception)
            {
                throw new ArgumentException("Gönderilen fotoğraf geçerli Base64 formatında değil.", nameof(base64Veri), exception);
            }

            /*
             * Dönüşümden sonra gerçek boyutu tekrar kontrol ediyoruz.
             */
            if (fotografVerisi.LongLength > maksimumBoyutByte)
            {
                throw new ArgumentException($"Fotoğraf boyutu en fazla " + $"{ApplicationConstants.MaksimumDosyaBoyutuMb} MB olabilir.");
            }

            var simdi = DateTime.Now;

            var goreliKlasorYolu = Path.Combine(simdi.ToString("yyyy"), simdi.ToString("MM"), simdi.ToString("dd"));

            var tamKlasorYolu = Path.Combine(ApiUrls.ViyolYanginDosyalarPath, goreliKlasorYolu);

            Directory.CreateDirectory(tamKlasorYolu);

            var dosyaUzantisi = IzinVerilenDosyaTipleri[ayristirilanVeri.ContentType];

            /*
             * Dışarıdan gelen dosya adını diskte kullanmıyoruz.
             * Path traversal ve aynı isimde dosya çakışması önlenmiş olur.
             */
            var dosyaAdi = $"{Guid.NewGuid():N}{dosyaUzantisi}";

            var goreliDosyaYolu = Path.Combine(goreliKlasorYolu, dosyaAdi);

            var tamDosyaYolu = Path.Combine(ApiUrls.ViyolYanginDosyalarPath, goreliDosyaYolu);

            await using var fileStream = new FileStream(tamDosyaYolu, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);

            await fileStream.WriteAsync(fotografVerisi, cancellationToken);

            return new KaydedilenFotografBilgisi
            {
                GoreliDosyaYolu = goreliDosyaYolu,
                TamDosyaYolu = tamDosyaYolu,
                DosyaAdi = dosyaAdi,
                ContentType = ayristirilanVeri.ContentType,
                BoyutByte = fotografVerisi.LongLength
            };
        }

        public Task DosyaSilAsync(string goreliDosyaYolu, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(goreliDosyaYolu))
                return Task.CompletedTask;

            try
            {
                var tamDosyaYolu = TamDosyaYolunuOlustur(goreliDosyaYolu);

                if (File.Exists(tamDosyaYolu))
                    File.Delete(tamDosyaYolu);
            }
            catch (Exception exception)
            {
                /*
                 * Temizleme hatası ana işlemi bozmamalı,
                 * ancak mutlaka loglanmalı.
                 */
                _logger.LogError(exception, "Yangın fotoğraf dosyası silinemedi. Yol: {DosyaYolu}", goreliDosyaYolu);
            }

            return Task.CompletedTask;
        }

        public string TamDosyaYolunuOlustur(string goreliDosyaYolu)
        {
            if (string.IsNullOrWhiteSpace(goreliDosyaYolu))
            {
                throw new ArgumentException("Göreli dosya yolu boş olamaz.", nameof(goreliDosyaYolu));
            }

            var anaKlasorTamYolu = Path.GetFullPath(ApiUrls.ViyolYanginDosyalarPath);

            var tamDosyaYolu = Path.GetFullPath(Path.Combine(anaKlasorTamYolu, goreliDosyaYolu));

            /*
             * Veritabanındaki yol değiştirilirse ana klasörün
             * dışına çıkılmasını engeller.
             */
            var anaKlasorOnEki = anaKlasorTamYolu.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!tamDosyaYolu.StartsWith(anaKlasorOnEki, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Dosya yolu izin verilen klasörün dışında.");
            }

            return tamDosyaYolu;
        }

        private static AyristirilanBase64Veri Base64VerisiniAyristir(string base64Veri, string? requestContentType)
        {
            var veri = base64Veri.Trim();
            var contentType = requestContentType?.Trim();

            /*
             * data:image/jpeg;base64,/9j/4AA...
             */
            if (veri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var virgulIndex = veri.IndexOf(',');

                if (virgulIndex < 0)
                    throw new ArgumentException("Geçersiz data URI formatı.");

                var header = veri[..virgulIndex];

                if (!header.Contains(";base64", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        "Fotoğraf data URI değeri Base64 formatında değil.");
                }

                var noktalıVirgulIndex = header.IndexOf(';');

                if (noktalıVirgulIndex <= "data:".Length)
                    throw new ArgumentException("Fotoğraf içerik tipi bulunamadı.");

                contentType = header["data:".Length..noktalıVirgulIndex];

                veri = veri[(virgulIndex + 1)..];
            }

            /*
             * Ham Base64 gönderilip content type gönderilmezse
             * kamera fotoğrafı için JPEG kabul ediyoruz.
             */
            contentType = string.IsNullOrWhiteSpace(contentType) ? "image/jpeg" : contentType.ToLowerInvariant();

            if (!IzinVerilenDosyaTipleri.ContainsKey(contentType))
            {
                throw new ArgumentException($"Desteklenmeyen fotoğraf tipi: {contentType}. " + "İzin verilen tipler: image/jpeg, image/png, image/webp.");
            }

            return new AyristirilanBase64Veri(veri, contentType);
        }

        private static string BosluklariTemizle(string veri)
        {
            var boslukVarMi = false;

            foreach (var karakter in veri)
            {
                if (!char.IsWhiteSpace(karakter))
                    continue;

                boslukVarMi = true;
                break;
            }

            if (!boslukVarMi)
                return veri;

            var builder = new StringBuilder(veri.Length);

            foreach (var karakter in veri)
            {
                if (!char.IsWhiteSpace(karakter))
                    builder.Append(karakter);
            }

            return builder.ToString();
        }

        private static long Base64DosyaBoyutunuHesapla(string temizBase64)
        {
            if (string.IsNullOrEmpty(temizBase64))
                return 0;

            var paddingSayisi = 0;

            if (temizBase64.EndsWith("==", StringComparison.Ordinal))
            {
                paddingSayisi = 2;
            }
            else if (temizBase64.EndsWith("=", StringComparison.Ordinal))
            {
                paddingSayisi = 1;
            }

            return temizBase64.Length * 3L / 4L - paddingSayisi;
        }

        private sealed record AyristirilanBase64Veri(string Base64Veri, string ContentType);
    }
}
