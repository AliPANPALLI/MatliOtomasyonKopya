using NesneIzleme.Application.Interfaces;
using NesneIzleme.Domain.Interfaces;
using NesneIzleme.Domain.Models.ReportModels;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NesneIzleme.Application.Services
{
    public class DebimetreAppService : IDebimetreAppService
    {
        private readonly IDebimetreRepository _debimetreRepository;

        public DebimetreAppService(IDebimetreRepository debimetreRepository)
        {
            _debimetreRepository = debimetreRepository;
        }
        public async Task<List<SuKaynakRaporModel>> GetDebimetreRaporByLokasyonIds(List<int> lokasyonIds, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip, CancellationToken cancellationToken = default)
        {
            var result = new List<SuKaynakRaporModel>();

            if (lokasyonIds == null || lokasyonIds.Count == 0)
                return result;

            /*
             * Tarih seçicisinden bitiş 00:00 geliyorsa
             * seçilen son günü de rapora dahil ediyoruz.
             */
            var sorguBitisZamani = bitisZamani;

            if (bitisZamani.TimeOfDay == TimeSpan.Zero)
            {
                sorguBitisZamani = bitisZamani.Date.AddDays(1);
            }

            var debimetreler = await _debimetreRepository.GetAktifDebimetrelerByLokasyonIdsAsync(lokasyonIds, cancellationToken);

            if (debimetreler.Count == 0)
                return result;

            var lokasyonlar = await _debimetreRepository.GetLokasyonlarFromLokasyonIds(lokasyonIds);

            var raporVerileri = await _debimetreRepository.GetDebimetreRaporVerileriAsync(lokasyonIds, baslangicZamani, sorguBitisZamani, raporlamaTip, cancellationToken);

            var veriGruplari = raporVerileri
                .GroupBy(x => x.DebimetreId)
                .ToDictionary(
                    x => x.Key,
                    x => x
                        .OrderBy(y => y.PeriyotBaslangic)
                        .ToList());

            foreach (var debimetre in debimetreler)
            {
                var lokasyonAdi = lokasyonlar
                    .FirstOrDefault(
                        x => x.Id == debimetre.LokasyonId)
                    ?.Name ?? string.Empty;

                var model = new SuKaynakRaporModel
                {
                    LokasyonName = lokasyonAdi,
                    Bolum = GetDebimetreBolumAdi(debimetre.Ad),
                    Kategori = GetDebimetreKategoriAdi(
                        debimetre.KategoriId),
                    Nokta = debimetre.Ad,
                    ZamansalVeri =
                        new Dictionary<string, decimal>()
                };

                if (veriGruplari.TryGetValue(
                        debimetre.Id,
                        out var veriler))
                {
                    foreach (var veri in veriler)
                    {
                        if (!veri.Toplam.HasValue)
                            continue;

                        var periyotAdi =
                            GetDebimetrePeriyotAdi(
                                veri.PeriyotBaslangic,
                                raporlamaTip);

                        model.ZamansalVeri[periyotAdi] =
                            decimal.Round(
                                veri.Toplam.Value,
                                2,
                                MidpointRounding.AwayFromZero);
                    }
                }

                result.Add(model);
            }

            return result;
        }

        private static string GetDebimetreBolumAdi(string debimetreAdi)
        {
            if (Regex.IsMatch(debimetreAdi, @"(?<![A-Z0-9])KH1(?![A-Z0-9])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return "Kesimhane 1";

            if (Regex.IsMatch(debimetreAdi, @"(?<![A-Z0-9])KH2(?![A-Z0-9])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return "Kesimhane-2";

            return string.Empty;
        }

        private static string GetDebimetreKategoriAdi(byte? kategoriId)
        {
            return kategoriId switch
            {
                1 => "Su Üretimi (m³)",
                2 => "Su Tüketimi (m³)",
                3 => "Gönderilen Su (m³)",
                _ => "Su Tüketimi (m³)"
            };
        }

        private static string GetDebimetrePeriyotAdi(DateTime periyotBaslangic, string raporlamaTip)
        {
            var culture = new CultureInfo("tr-TR");

            return raporlamaTip switch
            {
                "5 Dakikalık" or "5 Dakika" =>
                    $"{periyotBaslangic:dd MM yyyy} / " +
                    $"{periyotBaslangic:HH:mm}-" +
                    $"{periyotBaslangic.AddMinutes(5):HH:mm}",

                "Saatlik" =>
                    $"{periyotBaslangic:dd MM yyyy} / " +
                    $"{periyotBaslangic:HH}:00-" +
                    $"{periyotBaslangic.AddHours(1):HH}:00",

                "Günlük" =>
                    periyotBaslangic.ToString(
                        "dd MM yyyy dddd",
                        culture),

                "Haftalık" =>
                    $"{periyotBaslangic.Year} / Hafta " +
                    CultureInfo.InvariantCulture.Calendar
                        .GetWeekOfYear(
                            periyotBaslangic,
                            CalendarWeekRule.FirstDay,
                            DayOfWeek.Monday),

                "Aylık" =>
                    periyotBaslangic.ToString(
                        "MMMM yyyy",
                        culture),

                "Yıllık" =>
                    periyotBaslangic.ToString(
                        "yyyy",
                        culture),

                _ =>
                    periyotBaslangic.ToString(
                        "dd MM yyyy HH:mm",
                        culture)
            };
        }
    }
}
