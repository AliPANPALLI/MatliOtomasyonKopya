using Microsoft.Extensions.Options;
using MobilGuncelleme.Application.Interfaces;
using System.Security.Cryptography;

namespace MobilGuncelleme.Infrastructure.Depolama
{
    /// <summary>appsettings'teki MobilGuncelleme bölümü.</summary>
    public class MobilGuncellemeAyar
    {
        public const string Bolum = "MobilGuncelleme";

        /// <summary>APK'ların durduğu klasör, ör. E:\MatliOtomasyon\MobilSurumler.</summary>
        public string ApkKlasoru { get; set; } = null!;

        /// <summary>
        /// "Mobil Sürüm Yönetimi" sayfasının SharedApp'teki Modul + Form linki (Angular route'u).
        /// Admin endpoint'leri kullanıcının bu forma ait UserFormYetki kaydına (Okuma/Yazma) bakar.
        /// </summary>
        public string FormLink { get; set; } = null!;
    }

    /// <summary>
    /// APK'ları sunucu diskinde saklar. Dosya önce .tmp adıyla yazılır, SHA-256 yazma sırasında
    /// hesaplanır, yazma bitince asıl adına taşınır; böylece yarım dosya hiçbir zaman indirilmez.
    /// </summary>
    public class DiskApkDepo : IApkDepo
    {
        private readonly string _klasor;

        public DiskApkDepo(IOptions<MobilGuncellemeAyar> ayar)
        {
            _klasor = ayar.Value.ApkKlasoru;
            if (string.IsNullOrWhiteSpace(_klasor))
                throw new InvalidOperationException("MobilGuncelleme:ApkKlasoru ayarı tanımlı değil.");
        }

        public async Task<ApkKayitSonucu> KaydetAsync(string dosyaAdi, Stream icerik, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(_klasor);

            var hedef = TamYol(dosyaAdi);
            var gecici = hedef + ".tmp";

            try
            {
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                long boyut = 0;
                var tampon = new byte[81920];

                await using (var dosya = new FileStream(gecici, FileMode.Create, FileAccess.Write, FileShare.None, tampon.Length, useAsync: true))
                {
                    int okunan;
                    while ((okunan = await icerik.ReadAsync(tampon, cancellationToken)) > 0)
                    {
                        hash.AppendData(tampon, 0, okunan);
                        await dosya.WriteAsync(tampon.AsMemory(0, okunan), cancellationToken);
                        boyut += okunan;
                    }
                }

                File.Move(gecici, hedef, overwrite: true);

                var sha = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
                return new ApkKayitSonucu(sha, boyut);
            }
            catch
            {
                if (File.Exists(gecici))
                    File.Delete(gecici);
                throw;
            }
        }

        public string TamYol(string dosyaAdi)
        {
            // Dosya adı her zaman sunucuda üretilir; yine de klasör dışına çıkılmasın.
            var klasor = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_klasor)) + Path.DirectorySeparatorChar;
            var yol = Path.GetFullPath(Path.Combine(klasor, dosyaAdi));
            if (!yol.StartsWith(klasor, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Geçersiz dosya adı.");
            return yol;
        }

        public bool VarMi(string dosyaAdi) => File.Exists(TamYol(dosyaAdi));

        public void Sil(string dosyaAdi)
        {
            var yol = TamYol(dosyaAdi);
            if (File.Exists(yol))
                File.Delete(yol);
        }
    }
}
