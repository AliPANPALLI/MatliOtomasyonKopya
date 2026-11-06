using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace MatliOtomasyon.Shared.Infrastructure.Configuration
{
    /// <summary>
    /// Konfigürasyon değerlerindeki <c>{ORTAM_DEGISKENI}</c> biçimli placeholder'ları açılışta ortam
    /// değişkenleriyle dolduran kaynağı host'a ekler. Parolalar ve anahtarlar repoya girmez; appsettings
    /// dosyalarında sadece hangi ortam değişkeninin gerektiği yazılır. Tanımlı olmayan bir ortam değişkeni
    /// varsa uygulama hangi değişkenlerin eksik olduğunu söyleyerek açılmayı reddeder.
    /// </summary>
    public static class EnvironmentPlaceholderConfigurationExtensions
    {
        /// <summary>
        /// Placeholder çözümleyen kaynağı konfigürasyonun sonuna ekler. Kendisinden önce eklenmiş bütün
        /// kaynakları (appsettings, ortam değişkenleri, komut satırı) okuyup içlerindeki placeholder'ları çözer,
        /// bu yüzden diğer kaynaklardan sonra çağrılmalıdır.
        /// </summary>
        /// <param name="builder">Host'un konfigürasyon builder'ı.</param>
        /// <returns>Zincirleme çağrı için aynı builder.</returns>
        public static IConfigurationBuilder AddEnvironmentPlaceholders(this IConfigurationBuilder builder)
        {
            builder.Add(new EnvironmentPlaceholderConfigurationSource());
            return builder;
        }
    }

    /// <summary>
    /// Kendisinden önce eklenen kaynakları yakalayıp placeholder çözen provider'ı oluşturan konfigürasyon kaynağı.
    /// </summary>
    internal sealed class EnvironmentPlaceholderConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// Builder'da bu kaynaktan önce duran kaynaklarla bir <see cref="EnvironmentPlaceholderConfigurationProvider"/> oluşturur.
        /// </summary>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var precedingSources = builder.Sources.TakeWhile(source => !ReferenceEquals(source, this)).ToList();
            return new EnvironmentPlaceholderConfigurationProvider(precedingSources, builder.Properties);
        }
    }

    /// <summary>
    /// Önceki kaynaklardaki değerleri tarayıp placeholder içerenleri ortam değişkeni değerleriyle yeniden yazar.
    /// Sadece placeholder içeren anahtarları üretir; diğer değerler önceki kaynaklardan okunmaya devam eder.
    /// </summary>
    internal sealed class EnvironmentPlaceholderConfigurationProvider : ConfigurationProvider
    {
        // En az bir alt çizgi zorunlu: "{everything}" (Ocelot) veya "{Timestamp}" (Serilog) gibi şablonlar placeholder sayılmaz.
        private static readonly Regex PlaceholderPattern = new(@"\{([A-Z][A-Z0-9]*(?:_[A-Z0-9]+)+)\}", RegexOptions.Compiled);

        private readonly IReadOnlyList<IConfigurationSource> _sources;
        private readonly IDictionary<string, object> _properties;

        /// <summary>
        /// Taranacak kaynakları ve builder özelliklerini (dosya sağlayıcı gibi) alır.
        /// </summary>
        public EnvironmentPlaceholderConfigurationProvider(IReadOnlyList<IConfigurationSource> sources, IDictionary<string, object> properties)
        {
            _sources = sources;
            _properties = properties;
        }

        /// <summary>
        /// Önceki kaynakları geçici bir konfigürasyonda birleştirir, placeholder içeren her değeri çözer.
        /// </summary>
        /// <exception cref="InvalidOperationException">Placeholder'da adı geçen bir ortam değişkeni tanımlı değilse.</exception>
        public override void Load()
        {
            var builder = new ConfigurationBuilder();
            foreach (var property in _properties)
                builder.Properties[property.Key] = property.Value;
            foreach (var source in _sources)
                builder.Add(source);

            var root = builder.Build();
            try
            {
                var missing = new SortedSet<string>(StringComparer.Ordinal);
                var resolved = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var (key, value) in root.AsEnumerable())
                {
                    if (string.IsNullOrEmpty(value) || !PlaceholderPattern.IsMatch(value))
                        continue;

                    resolved[key] = PlaceholderPattern.Replace(value, match =>
                    {
                        var variableValue = Environment.GetEnvironmentVariable(match.Groups[1].Value);
                        if (!string.IsNullOrEmpty(variableValue))
                            return variableValue;

                        missing.Add(match.Groups[1].Value);
                        return match.Value;
                    });
                }

                if (missing.Count > 0)
                {
                    throw new InvalidOperationException(
                        "Konfigürasyonda kullanılan ortam değişkenleri tanımlı değil: " + string.Join(", ", missing) +
                        ". Değerleri sunucuda veya geliştirici makinesinde ortam değişkeni olarak tanımlayın (bkz. README > Konfigürasyon ve secret'lar).");
                }

                Data = resolved;
            }
            finally
            {
                (root as IDisposable)?.Dispose();
            }
        }
    }
}
