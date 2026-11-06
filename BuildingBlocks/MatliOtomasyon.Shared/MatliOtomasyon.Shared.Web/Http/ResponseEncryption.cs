using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace MatliOtomasyon.Shared.Web.Http
{
    /// <summary>
    /// Bazı API cevaplarını (menü, lokasyon, ticket listeleri) frontend'in beklediği biçimde AES ile kodlayan
    /// servisin ayarları. Bu kodlama gerçek bir güvenlik sağlamaz: aynı anahtar frontend'de de bulunur.
    /// Anahtar yine de repoda tutulmaz; <c>ResponseEncryption__Key</c> ortam değişkeninden gelir.
    /// Kodlama frontend ile birlikte kaldırılacak (Adım 1.10).
    /// </summary>
    public sealed class ResponseEncryptionOptions
    {
        /// <summary>Ayarların okunduğu konfigürasyon bölümünün adı.</summary>
        public const string SectionName = "ResponseEncryption";

        /// <summary>Frontend ile paylaşılan AES anahtarı (16, 24 veya 32 bayt).</summary>
        public string Key { get; set; } = string.Empty;
    }

    /// <summary>
    /// API cevaplarını frontend'in çözebildiği biçimde kodlar.
    /// </summary>
    public interface IResponseEncryptor
    {
        /// <summary>
        /// Metni AES-CBC (sıfır IV, PKCS7) ile şifreleyip Base64 olarak döner. Biçim frontend ile uyumlu olmak zorunda olduğu için değiştirilmemelidir.
        /// </summary>
        string Encrypt(string plainText);
    }

    /// <summary>
    /// <see cref="IResponseEncryptor"/>'ın, eski controller'lardaki <c>AesEncrypt</c> kopyalarıyla birebir aynı çıktıyı üreten uygulaması.
    /// </summary>
    public sealed class ResponseEncryptor : IResponseEncryptor
    {
        private readonly byte[] _key;

        /// <summary>
        /// Anahtarı ayarlardan alır.
        /// </summary>
        public ResponseEncryptor(IOptions<ResponseEncryptionOptions> options)
        {
            _key = Encoding.UTF8.GetBytes(options.Value.Key);
        }

        /// <inheritdoc />
        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = new byte[16];

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var stream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(stream, encryptor, CryptoStreamMode.Write))
            using (var writer = new StreamWriter(cryptoStream))
            {
                writer.Write(plainText);
            }

            return Convert.ToBase64String(stream.ToArray());
        }
    }

    /// <summary>
    /// Cevap kodlama servisini kaydeden extension.
    /// </summary>
    public static class ResponseEncryptionServiceCollectionExtensions
    {
        /// <summary>
        /// <see cref="IResponseEncryptor"/>'ı kaydeder ve anahtarın geçerli bir AES uzunluğunda olduğunu uygulama
        /// açılışında doğrular; anahtar eksik veya hatalıysa uygulama açılmaz.
        /// </summary>
        public static IServiceCollection AddResponseEncryption(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddOptions<ResponseEncryptionOptions>()
                .Bind(configuration.GetSection(ResponseEncryptionOptions.SectionName))
                .Validate(options => Encoding.UTF8.GetByteCount(options.Key) is 16 or 24 or 32,
                    "ResponseEncryption:Key 16, 24 veya 32 bayt olmalı (ResponseEncryption__Key ortam değişkeni).")
                .ValidateOnStart();
            services.AddSingleton<IResponseEncryptor, ResponseEncryptor>();
            return services;
        }
    }
}
