using MapsterMapper;
using SharedApp.Domain.Models;
using WaterLineScada.Application.Helpers;
using WaterLineScada.Application.Interfaces;
using WaterLineScada.Application.Services.Core;
using WaterLineScada.Application.ViewModels;
using WaterLineScada.Domain.DbViewModels;
using WaterLineScada.Domain.Interfaces;
using WaterLineScada.Domain.Models;
using WaterLineScada.Domain.Models.ReportModels;
using System.Globalization;
using System.Net;
using System.Text.Json;
using static MatliOtomasyon.Shared.Infrastructure.Configuration.ApiUrls;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

namespace WaterLineScada.Application.Services
{
    public class SuKaynakAppService : AppService<SuKaynak, SuKaynakViewModel>, ISuKaynakAppService
    {
        private readonly ISuKaynakRepository _suKaynakRepository;
        private readonly ISayacRepository _sayacRepository;
        private readonly IPlcAyarlarRepository _plcAyarlarRepository;
        private const int KesimhaneLokasyonId = 10;
        private const int DogalgazliKazanLokasyonId = 66;
        public SuKaynakAppService(IMapper mapper, ISuKaynakRepository suKaynakRepository, ISayacRepository sayacRepository, IPlcAyarlarRepository plcAyarlarRepository) : base(mapper, suKaynakRepository)
        {
            _sayacRepository = sayacRepository;
            _suKaynakRepository = suKaynakRepository;
            _plcAyarlarRepository = plcAyarlarRepository;
        }

        public async Task<List<SuKaynak>> GetAllSuKaynakForYetki()
        {
            var listSuKaynak = new List<SuKaynak>();

            var suKaynaklar = await _suKaynakRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true);
            listSuKaynak.AddRange(suKaynaklar);

            return listSuKaynak;
        }

        public async Task<List<Cizim>> GetAllCizimForYetki()
        {
            return await _suKaynakRepository.GetAllCizimForYetki();
        }

        public async Task<List<vwCizimLokasyonEsleme>> GetAllSuKaynakCizimForYetki()
        {
            var suKaynaklar = await _suKaynakRepository.GetAllSuKaynakCizimForYetki();
            return suKaynaklar.DistinctBy(x => x.CizimId).ToList();
        }

        public async Task<List<int>> GetLokasyonIdsBySuKaynakIds(List<int> suKaynakIds)
        {
            var lokasyonList = (await _suKaynakRepository.GetAllAsyncByPredicateWithAsNoTracking(x => suKaynakIds.Contains(x.Id) && x.Silindi != true)).Select(x => x.LokasyonId).ToList();
            return lokasyonList;
        }

        public async Task<List<AlarmViewModel>> GetAlarmLogForSuIzleme(int lokasyonId)
        {
            List<AlarmViewModel> result = new List<AlarmViewModel>();
            var alarmlar = await _suKaynakRepository.GetAlarmLogForSuIzleme(lokasyonId);

            result.AddRange(_mapper.Map<List<AlarmViewModel>>(alarmlar));
            return result;
        }
        public async Task<List<AlarmViewModel>> GetAktifAlarmLogForSuIzleme(int cizimId)
        {
            List<AlarmViewModel> result = new List<AlarmViewModel>();
            var alarmlar = await _suKaynakRepository.GetAktifAlarmLogForSuIzleme(cizimId);

            result.AddRange(_mapper.Map<List<AlarmViewModel>>(alarmlar));
            return result;
        }
        public async Task<List<AlarmViewModel>> GetPasifAlarmLogForSuIzleme(int cizimId)
        {
            List<AlarmViewModel> result = new List<AlarmViewModel>();
            var alarmlar = await _suKaynakRepository.GetPasifAlarmLogForSuIzleme(cizimId);

            result.AddRange(_mapper.Map<List<AlarmViewModel>>(alarmlar));
            return result;
        }
        public async Task<List<AlarmViewModel>> GetAktifAlarmLogForSuIzlemeByLokasyonId(int lokasyonId)
        {
            List<AlarmViewModel> result = new List<AlarmViewModel>();
            var alarmlar = await _suKaynakRepository.GetAktifAlarmLogForSuIzlemeByLokasyonId(lokasyonId);

            result.AddRange(_mapper.Map<List<AlarmViewModel>>(alarmlar));
            return result;
        }
        public List<TimePeriodData> GetTankData(List<SuKaynakLogYeni> dailyDataList, int suKaynakId, DateTime startDate, DateTime endDate, string period)
        {
            List<TimePeriodData> totalData = new List<TimePeriodData>();
            DateTime current = startDate;
            decimal previousPeriodValue = 0; // Önceki dönemin son değeri

            while (current <= endDate)
            {
                IEnumerable<SuKaynakLogYeni> filteredData = null;
                decimal total = 0;
                string periodName = "";
                switch (period)
                {
                    case "Günlük":
                        var dayData = dailyDataList.Where(data => data.KayitZamani.Date == current.Date && data.SuKaynakId == suKaynakId && data.DebiSayac.GetValueOrDefault(0) > 0)
                                   .OrderBy(data => data.KayitZamani) // Günün tüm verilerini sıralıyoruz
                                   .ToList();

                        if (dayData.Count > 0)
                        {
                            var firstDayRecord = dayData.FirstOrDefault(); // Günün ilk kaydını al
                            var lastDayRecord = dayData.LastOrDefault();  // Günün son kaydını al

                            if (firstDayRecord != null && lastDayRecord != null)
                            {
                                total = lastDayRecord.DebiSayac.GetValueOrDefault(0) - firstDayRecord.DebiSayac.GetValueOrDefault(0);
                            }
                        }
                        periodName = current.ToString("dd MM yyyy dddd", new CultureInfo("tr-TR"));
                        current = current.AddDays(1);
                        break;
                }
                // Sonucu ekle
                totalData.Add(new TimePeriodData { Periyot = current, Toplam = total, PeriyotName = periodName });
            }
            return totalData;
        }
        public TimeSpan GetCalismaSuresi(List<EtiketLogYeni> veriler)
        {
            TimeSpan toplamCalismaSuresi = TimeSpan.Zero;
            DateTime? baslangicZamani = null;

            // Verileri zaman sırasına göre sıralıyoruz
            var sıralıVeriler = veriler.OrderBy(v => v.KayitZamani).ToList();

            for (int i = 0; i < sıralıVeriler.Count; i++)
            {
                var zaman = sıralıVeriler[i].KayitZamani;
                var deger = sıralıVeriler[i].Deger;

                if (deger == 1) // Çalışıyorsa
                {
                    if (!baslangicZamani.HasValue)
                    {
                        // Başlangıç zamanını kaydet
                        baslangicZamani = zaman;
                    }
                }
                else if (deger != 1 && baslangicZamani.HasValue) // Çalışmıyorsa ve çalışma başlamışsa
                {
                    // Çalışma bitti, süreyi hesapla
                    toplamCalismaSuresi += (zaman.Value - baslangicZamani.Value);
                    baslangicZamani = null; // Başlangıç zamanını sıfırla
                }
            }

            // Eğer son veride hala çalışma devam ediyorsa (yani deger == 1 ise)
            if (baslangicZamani.HasValue)
            {
                // Çalışma süresi sonlandırılmadığı için toplam süreyi en son kaydedilen zamana kadar ekle
                toplamCalismaSuresi += (sıralıVeriler.Last().KayitZamani.Value - baslangicZamani.Value);
            }

            return toplamCalismaSuresi;
        }
        public List<TimePeriodData> GetTotalCalismaSureEski(List<EtiketLogYeni> dailyDataList, SuKaynak suKaynak, DateTime startDate, DateTime endDate, string period)
        {
            try
            {
                List<TimePeriodData> totalData = new List<TimePeriodData>();
                DateTime current = startDate;
                DateTime bitisZamani = endDate;

                while (current <= bitisZamani)
                {
                    IEnumerable<SuKaynakLogYeni> filteredData = null;
                    decimal total = 0;
                    string periodName = "";
                    if (period == "Saatlik") bitisZamani = endDate.AddMinutes(-1);
                    switch (period)
                    {
                        case "Saatlik":
                            var hourlyData = dailyDataList.Where(data => data.KayitZamani.Value >= current && data.KayitZamani.Value <= current.AddHours(1) && data.SuKaynakId == suKaynak.Id && data.EtiketId == suKaynak.CalismaSureEtiketId)
                                      .OrderBy(data => data.KayitZamani)
                                      .ToList();
                            if (hourlyData.Count > 0)
                                total = Convert.ToDecimal(GetCalismaSuresi(hourlyData).TotalHours);
                            else
                            {

                            }

                            periodName = current.ToString("dd MM yyyy", new CultureInfo("tr-TR")) + " / " + current.Hour.ToString("D2") + ":00" + /*hourlyData.First()?.KayitZamani.Value.Minute.ToString("D2") +*/ "-" + current.AddHours(1).Hour.ToString("D2") + ":00" /*+ hourlyData.Last()?.KayitZamani.Value.Minute.ToString("D2")*/;
                            current = current.AddHours(1);
                            break;
                        case "Günlük":
                            var dayData = dailyDataList.Where(data => data.KayitZamani.Value.Date == current.Date && data.SuKaynakId == suKaynak.Id && data.EtiketId == suKaynak.CalismaSureEtiketId)
                                      .OrderBy(data => data.KayitZamani)
                                      .ToList();
                            if (dayData.Count > 0) total = Convert.ToDecimal(GetCalismaSuresi(dayData).TotalHours);

                            periodName = current.ToString("dd MM yyyy dddd", new CultureInfo("tr-TR"));
                            current = current.AddDays(1);
                            break;
                        case "Haftalık":
                            DateTime weekStart = current.AddDays(-((int)current.DayOfWeek - (int)DayOfWeek.Monday));
                            DateTime weekEnd = weekStart.AddDays(6);

                            var weeklyData = dailyDataList.Where(data => data.KayitZamani.Value.Date >= weekStart && data.KayitZamani.Value.Date <= weekEnd && data.SuKaynakId == suKaynak.Id && data.EtiketId == suKaynak.CalismaSureEtiketId)
                                             .OrderBy(data => data.KayitZamani)
                                             .ToList();
                            if (weeklyData.Count > 0) total = Convert.ToDecimal(GetCalismaSuresi(weeklyData).TotalHours);

                            periodName = $"{current.ToString("yyyy", new CultureInfo("tr-TR"))} / Hafta {CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(current, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}";
                            current = weekStart.AddDays(7);
                            break;
                        case "Aylık":
                            DateTime monthStart = new DateTime(current.Year, current.Month, 1);
                            DateTime monthEnd = monthStart.AddMonths(1);
                            var monthData = dailyDataList.Where(data => data.KayitZamani.Value.Date >= monthStart && data.KayitZamani.Value.Date < monthEnd && data.SuKaynakId == suKaynak.Id && data.EtiketId == suKaynak.CalismaSureEtiketId)
                                        .OrderBy(data => data.KayitZamani)
                                        .ToList();
                            if (monthData.Count > 0) total = Convert.ToDecimal(GetCalismaSuresi(monthData).TotalHours);

                            periodName = current.ToString("MMMM yyyy", new CultureInfo("tr-TR"));
                            current = monthStart.AddMonths(1);
                            break;
                        case "Yıllık":
                            DateTime yearStart = new DateTime(current.Year, 1, 1);
                            DateTime yearEnd = new DateTime(current.Year, 12, 31);

                            var yearData = dailyDataList.Where(data => data.KayitZamani.Value.Date >= yearStart && data.KayitZamani.Value.Date <= yearEnd && data.SuKaynakId == suKaynak.Id && data.EtiketId == suKaynak.CalismaSureEtiketId)
                                                        .OrderBy(data => data.KayitZamani)
                                                        .ToList();
                            if (yearData.Count > 0) total = Convert.ToDecimal(GetCalismaSuresi(yearData).TotalHours);

                            periodName = current.ToString("yyyy", new CultureInfo("tr-TR"));
                            current = yearStart.AddYears(1);
                            break;
                    }
                    totalData.Add(new TimePeriodData { Periyot = current, Toplam = total, PeriyotName = periodName });
                }
                return totalData;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task<List<TimePeriodData>> GetTankSuDataFromView(string periyot, int suKaynakId, string tankName, DateTime startDate, DateTime endDate)
        {
            return await _suKaynakRepository.GetTankSuDataFromView(periyot, suKaynakId, tankName, startDate, endDate);
        }
        public async Task<List<TimePeriodData>> GetTotalData(int sayacId, DateTime startDate, DateTime endDate, string period)
        {
            return await _suKaynakRepository.GetToplamSuDataFromView(period, sayacId, startDate, endDate);
        }

        //public async Task<List<TimePeriodData>> GetTotalCalismaSure(int suKaynakId, DateTime startDate, DateTime endDate, string period)
        //{
        //    return await _suKaynakRepository.GetToplamCalismaSuresiDataFromView(period, suKaynakId, startDate, endDate);
        //}
        public List<TimePeriodData> GetTotalDataEski(List<SuKaynakLogYeni> dailyDataList, int sayacId, DateTime startDate, DateTime endDate, string period)
        {
            List<TimePeriodData> totalData = new List<TimePeriodData>();
            DateTime current = startDate;
            decimal previousPeriodValue = 0; // Önceki dönemin son değeri

            while (current <= endDate)
            {
                IEnumerable<SuKaynakLogYeni> filteredData = null;
                decimal total = 0;
                string periodName = "";
                switch (period)
                {
                    case "Günlük":
                        // Günlük veriler için filtreleme
                        //filteredData = dailyDataList.Where(data => data.KayitZamani.Date.Date == current.Date).OrderByDescending(x => x.Id);
                        //total = filteredData.OrderByDescending(X => X.Id).FirstOrDefault().DebiSayac.GetValueOrDefault(0);
                        var dayData = dailyDataList.Where(data => data.KayitZamani.Date == current.Date && data.SayacId == sayacId && data.DebiSayac.GetValueOrDefault(0) > 0)
                                  .OrderBy(data => data.KayitZamani) // Günün tüm verilerini sıralıyoruz
                                  .ToList();

                        if (dayData.Count > 0)
                        {
                            var firstDayRecord = dayData.FirstOrDefault(); // Günün ilk kaydını al
                            var lastDayRecord = dayData.LastOrDefault();  // Günün son kaydını al

                            if (firstDayRecord != null && lastDayRecord != null)
                            {
                                total = lastDayRecord.DebiSayac.GetValueOrDefault(0) - firstDayRecord.DebiSayac.GetValueOrDefault(0);
                            }
                        }
                        periodName = current.ToString("dd MM yyyy dddd", new CultureInfo("tr-TR"));
                        current = current.AddDays(1);
                        break;
                    case "Haftalık":
                        //while (current.DayOfWeek != DayOfWeek.Monday)
                        //{
                        //    current = current.AddDays(-1);
                        //} // Haftalık veriler için filtreleme (Pazartesi başlangıç)
                        DateTime weekStart = current.AddDays(-((int)current.DayOfWeek - (int)DayOfWeek.Monday));
                        DateTime weekEnd = weekStart.AddDays(6);
                        // Haftalık verileri filtrele
                        var weeklyData = dailyDataList.Where(data => data.KayitZamani.Date >= weekStart && data.KayitZamani.Date <= weekEnd && data.SayacId == sayacId && data.DebiSayac.GetValueOrDefault(0) > 0)
                                         .OrderBy(data => data.KayitZamani) // Ayın tüm verilerini sıralıyoruz
                                         .ToList();
                        //.GroupBy(data => data.KayitZamani.Date) // Günlere göre grupla
                        //.Select(g => g.OrderByDescending(data => data.KayitZamani) // Günün en son kaydını al
                        //             .FirstOrDefault())
                        //.ToList();
                        if (weeklyData.Count > 0)
                        {
                            // Haftanın ilk günü ve son günü için debisayac değerlerini al
                            var firstDayRecord = weeklyData.OrderBy(data => data.KayitZamani).FirstOrDefault(); // Haftanın ilk günü
                            var lastDayRecord = weeklyData.OrderByDescending(data => data.KayitZamani).FirstOrDefault(); // Haftanın son günü

                            // Eğer her iki kaydın debisayac değeri varsa farkını hesapla
                            if (firstDayRecord != null && lastDayRecord != null)
                            {
                                total = lastDayRecord.DebiSayac.GetValueOrDefault(0) - firstDayRecord.DebiSayac.GetValueOrDefault(0);
                                // Önceki haftanın son değeriyle farkı hesapla
                                //if (previousPeriodValue != 0)
                                //{
                                //    total += lastDayRecord.DebiSayac.GetValueOrDefault(0) - previousPeriodValue;
                                //}

                                //previousPeriodValue = lastDayRecord.DebiSayac.GetValueOrDefault(0); // Şu anki haftanın son değeri bir sonraki hafta için saklanacak
                            }
                        }
                        filteredData = dailyDataList.Where(data => data.KayitZamani.Date >= weekStart && data.KayitZamani.Date <= weekEnd);
                        periodName = $"{current.ToString("yyyy", new CultureInfo("tr-TR"))} / Hafta {CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(current, CalendarWeekRule.FirstDay, DayOfWeek.Monday)}";
                        current = weekStart.AddDays(7);
                        break;
                    case "Aylık":
                        // Aylık veriler için filtreleme
                        //filteredData = dailyDataList.Where(data => data.KayitZamani.Date.Year == current.Year && data.KayitZamani.Date.Month == current.Month);
                        DateTime monthStart = new DateTime(current.Year, current.Month, 1);
                        DateTime monthEnd = monthStart.AddMonths(1);
                        var monthData = dailyDataList.Where(data => data.KayitZamani.Date >= monthStart && data.KayitZamani.Date < monthEnd && data.SayacId == sayacId && data.DebiSayac.GetValueOrDefault(0) > 0)
                                    .OrderBy(data => data.KayitZamani) // Ayın tüm verilerini sıralıyoruz
                                    .ToList();

                        if (monthData.Count > 0)
                        {
                            var firstMonthRecord = monthData.FirstOrDefault(); // Ayın ilk kaydını al
                            var lastMonthRecord = monthData.LastOrDefault();  // Ayın son kaydını al

                            if (firstMonthRecord != null && lastMonthRecord != null)
                            {
                                total = lastMonthRecord.DebiSayac.GetValueOrDefault(0) - firstMonthRecord.DebiSayac.GetValueOrDefault(0);
                                // Önceki ayın son değeriyle farkı hesapla
                                //if (previousPeriodValue != 0)
                                //{
                                //    total += lastMonthRecord.DebiSayac.GetValueOrDefault(0) - previousPeriodValue;
                                //}

                                //previousPeriodValue = lastMonthRecord.DebiSayac.GetValueOrDefault(0); // Şu anki ayın son değeri bir sonraki ay için saklanacak
                            }
                        }
                        periodName = current.ToString("MMMM yyyy", new CultureInfo("tr-TR")); // "Ocak 2024"
                        current = monthStart.AddMonths(1);
                        break;
                    case "Yıllık":
                        // Yıllık veriler için filtreleme
                        //filteredData = dailyDataList.Where(data => data.KayitZamani.Date.Year == current.Year);
                        DateTime yearStart = new DateTime(current.Year, 1, 1);
                        DateTime yearEnd = new DateTime(current.Year, 12, 31);

                        var yearData = dailyDataList.Where(data => data.KayitZamani.Date >= yearStart && data.KayitZamani.Date <= yearEnd && data.SayacId == sayacId && data.DebiSayac.GetValueOrDefault(0) > 0)
                                                    .OrderBy(data => data.KayitZamani) // Yılın tüm verilerini sıralıyoruz
                                                    .ToList();

                        if (yearData.Count > 0)
                        {
                            var firstYearRecord = yearData.FirstOrDefault(); // Yılın ilk kaydını al
                            var lastYearRecord = yearData.LastOrDefault();  // Yılın son kaydını al

                            if (firstYearRecord != null && lastYearRecord != null)
                            {
                                total = lastYearRecord.DebiSayac.GetValueOrDefault(0) - firstYearRecord.DebiSayac.GetValueOrDefault(0);
                                // Önceki yılın son değeriyle farkı hesapla
                                //if (previousPeriodValue != 0)
                                //{
                                //    total += lastYearRecord.DebiSayac.GetValueOrDefault(0) - previousPeriodValue;
                                //}
                                //previousPeriodValue = lastYearRecord.DebiSayac.GetValueOrDefault(0); // Şu anki ayın son değeri bir sonraki ay için saklanacak
                            }
                        }
                        periodName = current.ToString("yyyy", new CultureInfo("tr-TR"));
                        current = yearStart.AddYears(1);
                        break;
                }

                // Filtrelenen verilerin toplamını hesapla
                //decimal total = filteredData.Sum(data => data.DebiSayac.GetValueOrDefault(0));

                // Sonucu ekle
                totalData.Add(new TimePeriodData { Periyot = current, Toplam = total, PeriyotName = periodName });

                // Zaman dilimine göre sonraki döneme geç
                //switch (period)
                //{
                //    case "Günlük":
                //        current = current.AddDays(1);
                //        break;
                //    case "Haftalık":
                //        current = current.AddDays(7);
                //        break;
                //    case "Aylık":
                //        current = current.AddMonths(1);
                //        break;
                //    case "Yıllık":
                //        current = current.AddYears(1);
                //        break;
                //}
            }

            return totalData;
        }
        public async Task<List<Tuple<string, int>>> GetLokasyonNamesForSuHatRaporByCizimId(int cizimId)
        {
            List<Tuple<string, int>> result = new List<Tuple<string, int>>();
            var cizimLokasyonlar = await _suKaynakRepository.GetVwCizimLokasyonEslemeFromCizimId(cizimId);

            return result;
        }
        public async Task<List<SuKaynakRaporModel>> GetSuHatRaporByCizimId(int cizimId, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip)
        {
            try
            {

                List<string> columns = new List<string>();
                if (raporlamaTip == "Günlük")
                {
                    columns = GetDayColumns(baslangicZamani, bitisZamani);
                }
                else if (raporlamaTip == "Haftalık")
                {
                    columns = GetWeekColumns(baslangicZamani, bitisZamani);
                }
                else if (raporlamaTip == "Aylık")
                {
                    columns = GetMonthColumns(baslangicZamani, bitisZamani);
                }
                else if (raporlamaTip == "Yıllık")
                {
                    columns = GetYearColumns(baslangicZamani, bitisZamani);
                }

                List<SuKaynakRaporModel> result = new List<SuKaynakRaporModel>();

                var lokasyonlarFromLokasyonId = await _suKaynakRepository.GetVwCizimLokasyonEslemeFromCizimId(cizimId);
                var suKaynaklar = await _suKaynakRepository.GetAllAsyncByPredicateWithAsNoTracking(x => (lokasyonlarFromLokasyonId.Select(y => y.LokasyonId).ToList().Contains(x.LokasyonId) || lokasyonlarFromLokasyonId.Select(y => y.LokasyonId).ToList().Contains(x.LokasyonId2.GetValueOrDefault(0))) && x.Silindi != true);
                var sayaclar = await _sayacRepository.GetAllAsyncByPredicateWithAsNoTracking(x => (suKaynaklar.Select(y => y.Id).ToList().Contains(x.FromSuKaynak.GetValueOrDefault(0)) || suKaynaklar.Select(y => y.Id).ToList().Contains(x.ToSuKaynak.GetValueOrDefault(0))) && x.Silindi != true);
                if (sayaclar.Count() == 0 || suKaynaklar.Count() == 0)
                {
                    return result;
                }
                var etiketLogVeriler = await _sayacRepository.GetEtiketLogVerilerFromSuKaynakId(suKaynaklar.Select(y => y.Id).ToList(), baslangicZamani, bitisZamani);
                //var suKaynakLogVeriler = await _sayacRepository.GetVwSuKaynakLog_SaatlikFromSayacId(sayaclar.Select(y => y.Id).ToList(), baslangicZamani, bitisZamani);
                //var lokasyonlar = await GetLokasyonlarWithApi();
                //if (lokasyonlar == null)
                //{
                //    throw new Exception("Lokasyonlar bulunamadı");
                //}
                var lokasyonIdCounter = new { Id = sayaclar[0].LokasyonId, Name = lokasyonlarFromLokasyonId.Where(x => x.LokasyonId == sayaclar[0].LokasyonId).First().LokasyonName };

                foreach (var sayac in sayaclar.OrderBy(x => x.LokasyonId).ThenBy(x => x.RaporSira))
                {
                    if (lokasyonIdCounter.Id != sayac.LokasyonId)
                    {
                        lokasyonIdCounter = new { Id = sayac.LokasyonId, Name = lokasyonlarFromLokasyonId.Where(x => x.LokasyonId == sayac.LokasyonId).First().LokasyonName };
                    }

                    if (sayac.SayacTip == SayacTip.None) continue;
                    //var data = AggregateByPeriod(suKaynakLogVeriler.Where(x => x.SayacId == sayac.Id).ToList(), raporlamaTip);
                    //var data = GetTotalData(suKaynakLogVeriler, sayac.Id, baslangicZamani, bitisZamani, raporlamaTip);
                    var data = await GetTotalData(sayac.Id, baslangicZamani, bitisZamani, raporlamaTip);

                    var model = new SuKaynakRaporModel();
                    model.LokasyonName = lokasyonIdCounter.Name;
                    if (sayac.SayacTip == SayacTip.Uretim)
                    {
                        model.Kategori = "Su Üretimi (m³)";
                    }
                    else if (sayac.SayacTip == SayacTip.Tuketim)
                    {
                        model.Kategori = "Su Tüketimi (m³)";
                    }
                    else if (sayac.SayacTip == SayacTip.Gonderilen)
                    {
                        model.Kategori = "Gönderilen Su (m³)";
                    }
                    model.Nokta = sayac.Name;
                    model.ZamansalVeri = new Dictionary<string, decimal>();
                    for (int i = 0; i < data.Count; i++)
                    {
                        model.ZamansalVeri[data[i].PeriyotName] = data[i].Toplam;
                    }

                    result.Add(model);
                }

                lokasyonIdCounter = new { Id = (int?)suKaynaklar[0].LokasyonId, Name = lokasyonlarFromLokasyonId.Where(x => x.LokasyonId == suKaynaklar[0].LokasyonId).First().LokasyonName };

                foreach (var suKaynak in suKaynaklar.OrderBy(x => x.RaporSira))
                {
                    if (lokasyonIdCounter.Id != suKaynak.LokasyonId)
                    {
                        lokasyonIdCounter = new { Id = (int?)suKaynak.LokasyonId, Name = lokasyonlarFromLokasyonId.Where(x => x.LokasyonId == suKaynak.LokasyonId).First().LokasyonName };
                    }

                    //var data = await GetTotalCalismaSure(suKaynak.Id, baslangicZamani, bitisZamani, raporlamaTip);
                    var model = new SuKaynakRaporModel();

                    if (suKaynak.SuKaynakTip == SuKaynakTip.Tank)
                    {
                        var data = await GetTankSuDataFromView(raporlamaTip, suKaynak.Id, suKaynak.Name, baslangicZamani, bitisZamani);
                        if (data.Count() == 0)
                            continue;
                        // 3 model oluşturuyoruz: Min, Ort., Max
                        var minModel = new SuKaynakRaporModel
                        {
                            Nokta = suKaynak.Name + "-Min",
                            LokasyonName = lokasyonIdCounter.Name,
                            Kategori = "Tank Su Hacim (m³)",
                            ZamansalVeri = new Dictionary<string, decimal>()
                        };

                        var ortModel = new SuKaynakRaporModel
                        {
                            Nokta = suKaynak.Name + "-Ort.",
                            LokasyonName = lokasyonIdCounter.Name,
                            Kategori = "Tank Su Hacim (m³)",
                            ZamansalVeri = new Dictionary<string, decimal>()
                        };

                        var maxModel = new SuKaynakRaporModel
                        {
                            Nokta = suKaynak.Name + "-Max",
                            LokasyonName = lokasyonIdCounter.Name,
                            Kategori = "Tank Su Hacim (m³)",
                            ZamansalVeri = new Dictionary<string, decimal>()
                        };

                        // Verileri ilgili modellere dağıtıyoruz
                        for (int i = 0; i < data.Count; i++)
                        {
                            var periyot = data[i].PeriyotName;
                            var toplam = decimal.Round(data[i].Toplam, 2, MidpointRounding.AwayFromZero);

                            if (i % 3 == 0)
                            {
                                minModel.ZamansalVeri[periyot] = toplam;
                            }
                            else if (i % 3 == 1)
                            {
                                ortModel.ZamansalVeri[periyot] = toplam;
                            }
                            else if (i % 3 == 2)
                            {
                                maxModel.ZamansalVeri[periyot] = toplam;
                            }
                        }
                        result.Add(minModel);
                        result.Add(ortModel);
                        result.Add(maxModel);
                    }
                    else
                    {
                        if (suKaynak.CalismaSureEtiketId.GetValueOrDefault(0) == 0) continue;
                        model.LokasyonName = lokasyonIdCounter.Name;
                        model.Nokta = suKaynak.Name;
                        model.ZamansalVeri = new Dictionary<string, decimal>();
                        model.Kategori = "Çalışma Süresi (saat)";
                        var data = GetTotalCalismaSureEski(etiketLogVeriler, suKaynak, baslangicZamani, bitisZamani, raporlamaTip);
                        for (int i = 0; i < data.Count; i++)
                        {
                            model.ZamansalVeri[data[i].PeriyotName] = decimal.Round(data[i].Toplam, 2, MidpointRounding.AwayFromZero);
                        }
                        result.Add(model);
                    }
                }

                return result;

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<List<SuKaynakRaporModel>> GetSuHatRaporByLokasyonIds(List<int> lokasyonIds, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip)
        {
            try
            {
                if (lokasyonIds == null || !lokasyonIds.Any())
                {
                    return new List<SuKaynakRaporModel>();
                }

                /*
                 * Kullanıcının gerçekten seçtiği lokasyonlar.
                 * Örneğin: [10]
                 */
                var secilenLokasyonIds = lokasyonIds
                    .Distinct()
                    .ToList();

                /*
                 * Veritabanında sorgulanacak lokasyonlar.
                 * Kesimhane seçildiyse: [10, 66]
                 */
                var sorgulanacakLokasyonIds = GetSorgulanacakLokasyonIds(secilenLokasyonIds);

                List<string> columns = new List<string>();
                if (raporlamaTip == "Günlük")
                {
                    columns = GetDayColumns(baslangicZamani, bitisZamani);
                }
                else if (raporlamaTip == "Haftalık")
                {
                    columns = GetWeekColumns(baslangicZamani, bitisZamani);
                }
                else if (raporlamaTip == "Aylık")
                {
                    columns = GetMonthColumns(baslangicZamani, bitisZamani);
                }
                else if (raporlamaTip == "Yıllık")
                {
                    columns = GetYearColumns(baslangicZamani, bitisZamani);
                }

                List<SuKaynakRaporModel> result = new List<SuKaynakRaporModel>();
                /*
                 * Lokasyon isimlerinde yalnızca kullanıcının seçtiği ana
                 * lokasyonları alıyoruz.
                 *
                 * 66 numaralı lokasyonun adı raporda kullanılmayacak.
                 * Onun yerine 10 numaralı Kesimhane adı kullanılacak.
                 */
                var lokasyons = await _suKaynakRepository.GetLokasyonlarFromLokasyonIds(secilenLokasyonIds);
                var lokasyonAdlari = lokasyons.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First().Name);
                //var lokasyonlarFromLokasyonId = await _suKaynakRepository.GetVwCizimLokasyonEslemeFromCizimId(cizimId);
                //var lokasyons = await _suKaynakRepository.GetLokasyonlarFromLokasyonIds(lokasyonIds);
                var suKaynaklar = await _suKaynakRepository.GetAllAsyncByPredicateWithAsNoTracking(x => (sorgulanacakLokasyonIds.Contains(x.LokasyonId) /*|| lokasyonIds.Contains(x.LokasyonId2.GetValueOrDefault(0))*/) && x.Silindi != true);
                var sayaclar = await _sayacRepository.GetAllAsyncByPredicateWithAsNoTracking(x => (suKaynaklar.Select(y => y.Id).ToList().Contains(x.FromSuKaynak.GetValueOrDefault(0)) /*|| suKaynaklar.Select(y => y.Id).ToList().Contains(x.ToSuKaynak.GetValueOrDefault(0))*/) && x.Silindi != true);
                if (sayaclar.Count() == 0 && suKaynaklar.Count() == 0)
                {
                    return result;
                }
                var etiketLogVeriler = await _sayacRepository.GetEtiketLogVerilerFromSuKaynakId(suKaynaklar.Select(y => y.Id).ToList(), baslangicZamani, bitisZamani);
                //var suKaynakLogVeriler = await _sayacRepository.GetVwSuKaynakLog_SaatlikFromSayacId(sayaclar.Select(y => y.Id).ToList(), baslangicZamani, bitisZamani);
                //var lokasyonlar = await GetLokasyonlarWithApi();
                //if (lokasyonlar == null)
                //{
                //    throw new Exception("Lokasyonlar bulunamadı");
                //}
                if (sayaclar.Any())
                {
                    foreach (var sayac in sayaclar
                        .OrderBy(x => GetRaporLokasyonId(x.LokasyonId))
                        .ThenBy(x => x.RaporSira))
                    {
                        if (sayac.SayacTip == SayacTip.None)
                        {
                            continue;
                        }

                        // 66 ise 10 döner.
                        var raporLokasyonId =
                            GetRaporLokasyonId(sayac.LokasyonId);

                        // Lokasyon adı 10 üzerinden bulunur.
                        if (!lokasyonAdlari.TryGetValue(
                                raporLokasyonId,
                                out var raporLokasyonName))
                        {
                            continue;
                        }

                        var data = await GetTotalData(
                            sayac.Id,
                            baslangicZamani,
                            bitisZamani,
                            raporlamaTip);

                        var model = new SuKaynakRaporModel
                        {
                            LokasyonName = raporLokasyonName,
                            Nokta = sayac.Name,
                            ZamansalVeri = new Dictionary<string, decimal>()
                        };

                        if (sayac.SayacTip == SayacTip.Uretim)
                        {
                            model.Kategori = "Su Üretimi (m³)";
                        }
                        else if (sayac.SayacTip == SayacTip.Tuketim)
                        {
                            model.Kategori = "Su Tüketimi (m³)";
                        }
                        else if (sayac.SayacTip == SayacTip.Gonderilen)
                        {
                            model.Kategori = "Gönderilen Su (m³)";
                        }

                        foreach (var item in data)
                        {
                            model.ZamansalVeri[item.PeriyotName] =
                                decimal.Round(
                                    item.Toplam,
                                    2,
                                    MidpointRounding.AwayFromZero);
                        }

                        result.Add(model);
                    }
                }
                if (suKaynaklar.Any())
                {
                    foreach (var suKaynak in suKaynaklar
                        .OrderBy(x => GetRaporLokasyonId(x.LokasyonId))
                        .ThenBy(x => x.RaporSira))
                    {
                        // 66 numaralı Doğalgazlı Kazan için 10 döner.
                        var raporLokasyonId =
                            GetRaporLokasyonId(suKaynak.LokasyonId);

                        // Lokasyon adı Kesimhane üzerinden alınır.
                        if (!lokasyonAdlari.TryGetValue(
                                raporLokasyonId,
                                out var raporLokasyonName))
                        {
                            continue;
                        }

                        if (suKaynak.SuKaynakTip == SuKaynakTip.Tank)
                        {
                            var data = await GetTankSuDataFromView(
                                raporlamaTip,
                                suKaynak.Id,
                                suKaynak.Name,
                                baslangicZamani,
                                bitisZamani);

                            if (data == null || !data.Any())
                            {
                                continue;
                            }

                            var minModel = new SuKaynakRaporModel
                            {
                                Nokta = suKaynak.Name + "-Min",
                                LokasyonName = raporLokasyonName,
                                Kategori = "Tank Su Hacim (m³)",
                                ZamansalVeri = new Dictionary<string, decimal>()
                            };

                            var ortModel = new SuKaynakRaporModel
                            {
                                Nokta = suKaynak.Name + "-Ort.",
                                LokasyonName = raporLokasyonName,
                                Kategori = "Tank Su Hacim (m³)",
                                ZamansalVeri = new Dictionary<string, decimal>()
                            };

                            var maxModel = new SuKaynakRaporModel
                            {
                                Nokta = suKaynak.Name + "-Max",
                                LokasyonName = raporLokasyonName,
                                Kategori = "Tank Su Hacim (m³)",
                                ZamansalVeri = new Dictionary<string, decimal>()
                            };

                            for (var i = 0; i < data.Count; i++)
                            {
                                var periyot = data[i].PeriyotName;

                                var toplam = decimal.Round(
                                    data[i].Toplam,
                                    2,
                                    MidpointRounding.AwayFromZero);

                                if (i % 3 == 0)
                                {
                                    minModel.ZamansalVeri[periyot] = toplam;
                                }
                                else if (i % 3 == 1)
                                {
                                    ortModel.ZamansalVeri[periyot] = toplam;
                                }
                                else
                                {
                                    maxModel.ZamansalVeri[periyot] = toplam;
                                }
                            }

                            result.Add(minModel);
                            result.Add(ortModel);
                            result.Add(maxModel);
                        }
                        else
                        {
                            if (suKaynak.CalismaSureEtiketId
                                    .GetValueOrDefault(0) == 0)
                            {
                                continue;
                            }

                            var model = new SuKaynakRaporModel
                            {
                                LokasyonName = raporLokasyonName,
                                Nokta = suKaynak.Name,
                                Kategori = "Çalışma Süresi (saat)",
                                ZamansalVeri = new Dictionary<string, decimal>()
                            };

                            var data = GetTotalCalismaSureEski(
                                etiketLogVeriler,
                                suKaynak,
                                baslangicZamani,
                                bitisZamani,
                                raporlamaTip);

                            foreach (var item in data)
                            {
                                model.ZamansalVeri[item.PeriyotName] =
                                    decimal.Round(
                                        item.Toplam,
                                        2,
                                        MidpointRounding.AwayFromZero);
                            }

                            result.Add(model);
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<List<SuKaynakRaporModel>> GetSuHatRaporByCizimId2(int cizimId, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip, CancellationToken cancellationToken = default)
        {
            var result = new List<SuKaynakRaporModel>();

            /*
             * 1. Çizime bağlı lokasyonları getir.
             */
            var lokasyonlarFromLokasyonId =
                await _suKaynakRepository
                    .GetVwCizimLokasyonEslemeFromCizimId(
                        cizimId);

            if (lokasyonlarFromLokasyonId == null ||
                lokasyonlarFromLokasyonId.Count == 0)
            {
                return result;
            }

            var lokasyonIds = lokasyonlarFromLokasyonId
                .Select(x => Convert.ToInt32(x.LokasyonId))
                .Distinct()
                .ToList();

            var lokasyonAdlari =
                lokasyonlarFromLokasyonId
                    .GroupBy(x =>
                        Convert.ToInt32(x.LokasyonId))
                    .ToDictionary(
                        group => group.Key,
                        group => group.First().LokasyonName);

            /*
             * 2. Lokasyonlara bağlı su kaynaklarını getir.
             */
            var suKaynaklar =
                await _suKaynakRepository
                    .GetAllAsyncByPredicateWithAsNoTracking(x =>
                        (
                            lokasyonIds.Contains(x.LokasyonId) ||

                            lokasyonIds.Contains(
                                x.LokasyonId2.GetValueOrDefault())
                        ) &&
                        x.Silindi != true);

            if (suKaynaklar == null ||
                suKaynaklar.Count == 0)
            {
                return result;
            }

            var suKaynakIds = suKaynaklar
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            /*
             * 3. Su kaynaklarına bağlı sayaçları getir.
             */
            var sayaclar =
                await _sayacRepository
                    .GetAllAsyncByPredicateWithAsNoTracking(x =>
                        (
                            suKaynakIds.Contains(
                                x.FromSuKaynak.GetValueOrDefault()) ||

                            suKaynakIds.Contains(
                                x.ToSuKaynak.GetValueOrDefault())
                        ) &&
                        x.Silindi != true);

            var aktifSayaclar = sayaclar
                .Where(x =>
                    x.SayacTip != SayacTip.None)
                .OrderBy(x => x.LokasyonId)
                .ThenBy(x => x.RaporSira)
                .ToList();

            var sayacIds = aktifSayaclar
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            /*
             * 4. Tankları ve çalışma süresi olan kaynakları ayır.
             */
            var tanklar = suKaynaklar
                .Where(x =>
                    x.SuKaynakTip == SuKaynakTip.Tank)
                .ToList();

            var tankIds = tanklar
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            var calismaSureKaynaklari = suKaynaklar
                .Where(x =>
                    x.SuKaynakTip != SuKaynakTip.Tank &&
                    x.CalismaSureEtiketId.HasValue &&
                    x.CalismaSureEtiketId.Value > 0)
                .ToList();

            var calismaSureSuKaynakIds =
                calismaSureKaynaklari
                    .Select(x => x.Id)
                    .Distinct()
                    .ToList();

            var calismaSureEtiketIds =
                calismaSureKaynaklari
                    .Select(x =>
                        x.CalismaSureEtiketId!.Value)
                    .Distinct()
                    .ToList();

            /*
             * 5. Bütün sayaç verilerini tek sorguda getir.
             */
            var tumSayacVerileri =
                sayacIds.Count == 0
                    ? new List<SayacPeriyotData2>()
                    : await _sayacRepository
                        .GetToplamSuDataFromViewToplu2(
                            raporlamaTip,
                            sayacIds,
                            baslangicZamani,
                            bitisZamani,
                            cancellationToken);

            /*
             * 6. Bütün tank verilerini tek sorguda getir.
             */
            var tumTankVerileri =
                tankIds.Count == 0
                    ? new List<TankPeriyotData2>()
                    : await _sayacRepository
                        .GetTankSuDataFromViewToplu2(
                            raporlamaTip,
                            tankIds,
                            baslangicZamani,
                            bitisZamani,
                            cancellationToken);

            /*
             * 7. Bütün çalışma süresi etiketlerini tek sorguda getir.
             */
            var tumEtiketLoglari =
                calismaSureSuKaynakIds.Count == 0
                    ? new List<EtiketLogYeni>()
                    : await _sayacRepository
                        .GetEtiketLogVeriler2(
                            calismaSureSuKaynakIds,
                            calismaSureEtiketIds,
                            baslangicZamani,
                            bitisZamani,
                            cancellationToken);

            /*
             * 8. Verileri bellekte hızlı erişim için grupla.
             */
            var sayacLookup =
                tumSayacVerileri.ToLookup(
                    x => x.SayacId);

            var tankLookup =
                tumTankVerileri.ToLookup(
                    x => x.SuKaynakId);

            var etiketLogLookup =
                tumEtiketLoglari
                    .Where(x =>
                        x.SuKaynakId.HasValue &&
                        x.EtiketId.HasValue &&
                        x.KayitZamani.HasValue)
                    .ToLookup(x => (
                        SuKaynakId: x.SuKaynakId!.Value,
                        EtiketId: x.EtiketId!.Value
                    ));

            /*
             * 9. Sayaç rapor modellerini oluştur.
             */
            foreach (var sayac in aktifSayaclar)
            {
                var lokasyonId =
                    Convert.ToInt32(sayac.LokasyonId);

                lokasyonAdlari.TryGetValue(
                    lokasyonId,
                    out var lokasyonName);

                var model = new SuKaynakRaporModel
                {
                    LokasyonName = lokasyonName ?? string.Empty,
                    Nokta = sayac.Name,
                    ZamansalVeri =
                        new Dictionary<string, decimal>()
                };

                model.Kategori = sayac.SayacTip switch
                {
                    SayacTip.Uretim =>
                        "Su Üretimi (m³)",

                    SayacTip.Tuketim =>
                        "Su Tüketimi (m³)",

                    SayacTip.Gonderilen =>
                        "Gönderilen Su (m³)",

                    _ => string.Empty
                };

                var sayacVerileri =
                    sayacLookup[sayac.Id]
                        .OrderBy(x => x.Periyot)
                        .ToList();

                foreach (var veri in sayacVerileri)
                {
                    var periyotName =
                        SuHatRaporTarihHelper2.GetPeriodName2(
                            veri.Periyot,
                            raporlamaTip);

                    model.ZamansalVeri[periyotName] =
                        decimal.Round(
                            veri.Toplam,
                            2,
                            MidpointRounding.AwayFromZero);
                }

                result.Add(model);
            }

            /*
             * 10. Su kaynakları rapor modellerini oluştur.
             */
            foreach (var suKaynak in suKaynaklar
                         .OrderBy(x => x.LokasyonId)
                         .ThenBy(x => x.RaporSira))
            {
                var lokasyonId =
                    Convert.ToInt32(suKaynak.LokasyonId);

                lokasyonAdlari.TryGetValue(
                    lokasyonId,
                    out var lokasyonName);

                /*
                 * Tank ise Min / Ortalama / Max modellerini oluştur.
                 */
                if (suKaynak.SuKaynakTip == SuKaynakTip.Tank)
                {
                    var tankVerileri =
                        tankLookup[suKaynak.Id]
                            .OrderBy(x => x.Periyot)
                            .ToList();

                    if (tankVerileri.Count == 0)
                        continue;

                    var minModel = new SuKaynakRaporModel
                    {
                        Nokta = suKaynak.Name + "-Min",
                        LokasyonName =
                            lokasyonName ?? string.Empty,
                        Kategori = "Tank Su Hacim (m³)",
                        ZamansalVeri =
                            new Dictionary<string, decimal>()
                    };

                    var ortModel = new SuKaynakRaporModel
                    {
                        Nokta = suKaynak.Name + "-Ort.",
                        LokasyonName =
                            lokasyonName ?? string.Empty,
                        Kategori = "Tank Su Hacim (m³)",
                        ZamansalVeri =
                            new Dictionary<string, decimal>()
                    };

                    var maxModel = new SuKaynakRaporModel
                    {
                        Nokta = suKaynak.Name + "-Max",
                        LokasyonName =
                            lokasyonName ?? string.Empty,
                        Kategori = "Tank Su Hacim (m³)",
                        ZamansalVeri =
                            new Dictionary<string, decimal>()
                    };

                    foreach (var veri in tankVerileri)
                    {
                        var periyotName =
                            SuHatRaporTarihHelper2.GetPeriodName2(
                                veri.Periyot,
                                raporlamaTip);

                        minModel.ZamansalVeri[periyotName] =
                            decimal.Round(
                                veri.MinHacim,
                                2,
                                MidpointRounding.AwayFromZero);

                        ortModel.ZamansalVeri[periyotName] =
                            decimal.Round(
                                veri.OrtalamaHacim,
                                2,
                                MidpointRounding.AwayFromZero);

                        maxModel.ZamansalVeri[periyotName] =
                            decimal.Round(
                                veri.MaxHacim,
                                2,
                                MidpointRounding.AwayFromZero);
                    }

                    result.Add(minModel);
                    result.Add(ortModel);
                    result.Add(maxModel);

                    continue;
                }

                /*
                 * Tank değilse çalışma süresini hesapla.
                 */
                if (!suKaynak.CalismaSureEtiketId.HasValue ||
                    suKaynak.CalismaSureEtiketId.Value == 0)
                {
                    continue;
                }

                var lookupKey = (
                    SuKaynakId: suKaynak.Id,
                    EtiketId:
                        suKaynak.CalismaSureEtiketId.Value
                );

                var ilgiliEtiketLoglari =
                    etiketLogLookup[lookupKey]
                        .OrderBy(x => x.KayitZamani)
                        .ToList();

                var calismaSureData =
                    GetTotalCalismaSure2(
                        ilgiliEtiketLoglari,
                        baslangicZamani,
                        bitisZamani,
                        raporlamaTip);

                var calismaModel = new SuKaynakRaporModel
                {
                    LokasyonName =
                        lokasyonName ?? string.Empty,

                    Nokta = suKaynak.Name,

                    Kategori =
                        "Çalışma Süresi (saat)",

                    ZamansalVeri =
                        new Dictionary<string, decimal>()
                };

                foreach (var veri in calismaSureData)
                {
                    calismaModel.ZamansalVeri[
                        veri.PeriyotName
                    ] = decimal.Round(
                        veri.Toplam,
                        2,
                        MidpointRounding.AwayFromZero);
                }

                result.Add(calismaModel);
            }

            return result;
        }
        public List<TimePeriodData> GetTotalCalismaSure2(List<EtiketLogYeni> etiketLoglari, DateTime startDate, DateTime endDate, string period)
        {
            var range =
                SuHatRaporTarihHelper2.GetReportRange2(
                    startDate,
                    endDate,
                    period);

            var uygunKayitlar = etiketLoglari
                .Where(x =>
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value >= range.Start &&
                    x.KayitZamani.Value < range.EndExclusive)
                .OrderBy(x => x.KayitZamani)
                .ToList();

            var periyotKayitlari = uygunKayitlar
                .GroupBy(x =>
                    SuHatRaporTarihHelper2.GetPeriodStart2(
                        x.KayitZamani!.Value,
                        period))
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderBy(x => x.KayitZamani)
                        .ToList());

            var result = new List<TimePeriodData>();

            var current =
                SuHatRaporTarihHelper2.GetPeriodStart2(
                    range.Start,
                    period);

            while (current < range.EndExclusive)
            {
                decimal toplam = 0;

                if (periyotKayitlari.TryGetValue(
                        current,
                        out var periodData) &&
                    periodData.Count > 0)
                {
                    toplam = Convert.ToDecimal(
                        GetCalismaSuresi(periodData).TotalHours);
                }

                result.Add(new TimePeriodData
                {
                    Periyot = current,

                    Toplam = decimal.Round(
                        toplam,
                        2,
                        MidpointRounding.AwayFromZero),

                    PeriyotName =
                        SuHatRaporTarihHelper2.GetPeriodName2(
                            current,
                            period)
                });

                current =
                    SuHatRaporTarihHelper2.GetNextPeriod2(
                        current,
                        period);
            }

            return result;
        }
        public async Task<List<SuKaynakRaporModel>> GetSuHatRaporByCizimIdYeni(int cizimId, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip)
        {
            try
            {
                List<string> columns = new List<string>();
                if (raporlamaTip == "Günlük")
                {
                    columns = GetDayColumns(baslangicZamani, bitisZamani);
                }
                else if (raporlamaTip == "Haftalık")
                {
                    columns = GetWeekColumns(baslangicZamani, bitisZamani);
                }
                else if (raporlamaTip == "Aylık")
                {
                    columns = GetMonthColumns(baslangicZamani, bitisZamani);
                }
                else if (raporlamaTip == "Yıllık")
                {
                    columns = GetYearColumns(baslangicZamani, bitisZamani);
                }

                List<SuKaynakRaporModel> result = new List<SuKaynakRaporModel>();

                var lokasyonlarFromLokasyonId = await _suKaynakRepository.GetVwCizimLokasyonEslemeFromCizimId(cizimId);
                var suKaynaklar = await _suKaynakRepository.GetAllAsyncByPredicateWithAsNoTracking(x => (lokasyonlarFromLokasyonId.Select(y => y.LokasyonId).ToList().Contains(x.LokasyonId) || lokasyonlarFromLokasyonId.Select(y => y.LokasyonId).ToList().Contains(x.LokasyonId2.GetValueOrDefault(0))) && x.Silindi != true);
                var sayaclar = await _sayacRepository.GetAllAsyncByPredicateWithAsNoTracking(x => (suKaynaklar.Select(y => y.Id).ToList().Contains(x.FromSuKaynak.GetValueOrDefault(0)) || suKaynaklar.Select(y => y.Id).ToList().Contains(x.ToSuKaynak.GetValueOrDefault(0))) && x.Silindi != true);
                if (sayaclar.Count() == 0 || suKaynaklar.Count() == 0)
                {
                    return result;
                }
                var etiketLogVeriler = await _sayacRepository.GetEtiketLogVerilerFromSuKaynakId(suKaynaklar.Select(y => y.Id).ToList(), baslangicZamani, bitisZamani);
                //var suKaynakLogVeriler = await _sayacRepository.GetVwSuKaynakLog_SaatlikFromSayacId(sayaclar.Select(y => y.Id).ToList(), baslangicZamani, bitisZamani);
                //var lokasyonlar = await GetLokasyonlarWithApi();
                //if (lokasyonlar == null)
                //{
                //    throw new Exception("Lokasyonlar bulunamadı");
                //}
                var lokasyonIdCounter = new { Id = sayaclar[0].LokasyonId, Name = lokasyonlarFromLokasyonId.First().LokasyonName };

                foreach (var sayac in sayaclar.OrderBy(x => x.LokasyonId).ThenBy(x => x.RaporSira))
                {
                    if (lokasyonIdCounter.Id != sayac.LokasyonId)
                    {
                        lokasyonIdCounter = new { Id = sayac.LokasyonId, Name = lokasyonlarFromLokasyonId.First().LokasyonName };
                    }

                    if (sayac.SayacTip == SayacTip.None) continue;
                    //var data = AggregateByPeriod(suKaynakLogVeriler.Where(x => x.SayacId == sayac.Id).ToList(), raporlamaTip);
                    //var data = GetTotalData(suKaynakLogVeriler, sayac.Id, baslangicZamani, bitisZamani, raporlamaTip);
                    var data = await GetTotalData(sayac.Id, baslangicZamani, bitisZamani, raporlamaTip);

                    var model = new SuKaynakRaporModel();
                    model.LokasyonName = lokasyonIdCounter.Name;
                    if (sayac.SayacTip == SayacTip.Uretim)
                    {
                        model.Kategori = "Su Üretimi (m³)";
                    }
                    else if (sayac.SayacTip == SayacTip.Tuketim)
                    {
                        model.Kategori = "Su Tüketimi (m³)";
                    }
                    else if (sayac.SayacTip == SayacTip.Gonderilen)
                    {
                        model.Kategori = "Gönderilen Su (m³)";
                    }
                    model.Nokta = sayac.Name;
                    model.ZamansalVeri = new Dictionary<string, decimal>();
                    for (int i = 0; i < data.Count; i++)
                    {
                        model.ZamansalVeri[data[i].PeriyotName] = data[i].Toplam;
                    }

                    result.Add(model);
                }

                lokasyonIdCounter = new { Id = (int?)suKaynaklar[0].LokasyonId, Name = lokasyonlarFromLokasyonId.First().LokasyonName };

                foreach (var suKaynak in suKaynaklar.OrderBy(x => x.RaporSira))
                {
                    if (lokasyonIdCounter.Id != suKaynak.LokasyonId)
                    {
                        lokasyonIdCounter = new { Id = (int?)suKaynak.LokasyonId, Name = lokasyonlarFromLokasyonId.First().LokasyonName };
                    }

                    //var data = await GetTotalCalismaSure(suKaynak.Id, baslangicZamani, bitisZamani, raporlamaTip);
                    var model = new SuKaynakRaporModel();

                    if (suKaynak.SuKaynakTip == SuKaynakTip.Tank)
                    {
                        var data = await GetTankSuDataFromView(raporlamaTip, suKaynak.Id, suKaynak.Name, baslangicZamani, bitisZamani);
                        if (data.Count() == 0)
                            continue;
                        // 3 model oluşturuyoruz: Min, Ort., Max
                        var minModel = new SuKaynakRaporModel
                        {
                            Nokta = suKaynak.Name + "-Min",
                            LokasyonName = lokasyonIdCounter.Name,
                            Kategori = "Tank Su Hacim (m³)",
                            ZamansalVeri = new Dictionary<string, decimal>()
                        };

                        var ortModel = new SuKaynakRaporModel
                        {
                            Nokta = suKaynak.Name + "-Ort.",
                            LokasyonName = lokasyonIdCounter.Name,
                            Kategori = "Tank Su Hacim (m³)",
                            ZamansalVeri = new Dictionary<string, decimal>()
                        };

                        var maxModel = new SuKaynakRaporModel
                        {
                            Nokta = suKaynak.Name + "-Max",
                            LokasyonName = lokasyonIdCounter.Name,
                            Kategori = "Tank Su Hacim (m³)",
                            ZamansalVeri = new Dictionary<string, decimal>()
                        };

                        // Verileri ilgili modellere dağıtıyoruz
                        for (int i = 0; i < data.Count; i++)
                        {
                            var periyot = data[i].PeriyotName;
                            var toplam = decimal.Round(data[i].Toplam, 2, MidpointRounding.AwayFromZero);

                            if (i % 3 == 0)
                            {
                                minModel.ZamansalVeri[periyot] = toplam;
                            }
                            else if (i % 3 == 1)
                            {
                                ortModel.ZamansalVeri[periyot] = toplam;
                            }
                            else if (i % 3 == 2)
                            {
                                maxModel.ZamansalVeri[periyot] = toplam;
                            }
                        }
                        result.Add(minModel);
                        result.Add(ortModel);
                        result.Add(maxModel);
                    }
                    else
                    {
                        if (suKaynak.CalismaSureEtiketId.GetValueOrDefault(0) == 0) continue;
                        model.LokasyonName = lokasyonIdCounter.Name;
                        model.Nokta = suKaynak.Name;
                        model.ZamansalVeri = new Dictionary<string, decimal>();
                        model.Kategori = "Çalışma Süresi (saat)";
                        var data = GetTotalCalismaSureEski(etiketLogVeriler, suKaynak, baslangicZamani, bitisZamani, raporlamaTip);
                        for (int i = 0; i < data.Count; i++)
                        {
                            model.ZamansalVeri[data[i].PeriyotName] = decimal.Round(data[i].Toplam, 2, MidpointRounding.AwayFromZero);
                        }
                        result.Add(model);
                    }
                }

                return result;

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<List<SuKaynakRaporModel>> GetSuHatRaporByLokasyonId(int lokasyonId, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip)
        {
            List<string> columns = new List<string>();
            if (raporlamaTip == "Günlük")
            {
                columns = GetDayColumns(baslangicZamani, bitisZamani);
            }
            else if (raporlamaTip == "Haftalık")
            {
                columns = GetWeekColumns(baslangicZamani, bitisZamani);
            }
            else if (raporlamaTip == "Aylık")
            {
                columns = GetMonthColumns(baslangicZamani, bitisZamani);
            }
            else if (raporlamaTip == "Yıllık")
            {
                columns = GetYearColumns(baslangicZamani, bitisZamani);
            }

            List<SuKaynakRaporModel> result = new List<SuKaynakRaporModel>();

            var lokasyonlarFromLokasyonId = await _suKaynakRepository.GetCizimLokasyonEslemeFromLokasyonId(lokasyonId);
            var suKaynaklar = await _suKaynakRepository.GetAllAsyncByPredicateWithAsNoTracking(x => (lokasyonlarFromLokasyonId.Select(y => y.LokasyonId).ToList().Contains(x.LokasyonId) || lokasyonlarFromLokasyonId.Select(y => y.LokasyonId).ToList().Contains(x.LokasyonId2.GetValueOrDefault(0))) && x.Silindi != true);
            var sayaclar = await _sayacRepository.GetAllAsyncByPredicateWithAsNoTracking(x => (suKaynaklar.Select(y => y.Id).ToList().Contains(x.FromSuKaynak.GetValueOrDefault(0)) || suKaynaklar.Select(y => y.Id).ToList().Contains(x.ToSuKaynak.GetValueOrDefault(0))) && x.Silindi != true);
            if (sayaclar.Count() == 0 || suKaynaklar.Count() == 0)
            {
                return result;
            }
            var etiketLogVeriler = await _sayacRepository.GetEtiketLogVerilerFromSuKaynakId(suKaynaklar.Select(y => y.Id).ToList(), baslangicZamani, bitisZamani);
            var suKaynakLogVeriler = await _sayacRepository.GetSuKaynakLogVerilerFromSayacId(sayaclar.Select(y => y.Id).ToList(), baslangicZamani, bitisZamani);
            var lokasyonlar = await GetLokasyonlarWithApi();
            if (lokasyonlar == null)
            {
                throw new Exception("Lokasyonlar bulunamadı");
            }
            var lokasyonIdCounter = new { Id = sayaclar[0].LokasyonId, Name = lokasyonlar.Where(x => x.Id == sayaclar[0].LokasyonId).First().Name };

            foreach (var sayac in sayaclar.OrderBy(x => x.LokasyonId))
            {
                if (lokasyonIdCounter.Id != sayac.LokasyonId)
                {
                    lokasyonIdCounter = new { Id = sayac.LokasyonId, Name = lokasyonlar.Where(x => x.Id == sayac.LokasyonId).First().Name };
                }

                if (sayac.SayacTip == SayacTip.None) continue;
                var data = GetTotalDataEski(suKaynakLogVeriler, sayac.Id, baslangicZamani, bitisZamani, raporlamaTip);

                var model = new SuKaynakRaporModel();
                model.LokasyonName = lokasyonIdCounter.Name;
                if (sayac.SayacTip == SayacTip.Uretim)
                {
                    model.Kategori = "Su Üretimi (m³)";
                }
                else if (sayac.SayacTip == SayacTip.Tuketim)
                {
                    model.Kategori = "Gönderilen Su (m³)";
                }
                model.Nokta = sayac.Name;
                model.ZamansalVeri = new Dictionary<string, decimal>();
                for (int i = 0; i < data.Count; i++)
                {
                    model.ZamansalVeri[data[i].PeriyotName] = data[i].Toplam;
                }

                result.Add(model);
            }

            lokasyonIdCounter = new { Id = (int?)suKaynaklar[0].LokasyonId, Name = lokasyonlar.Where(x => x.Id == suKaynaklar[0].LokasyonId).First().Name };

            foreach (var suKaynak in suKaynaklar)
            {
                if (lokasyonIdCounter.Id != suKaynak.LokasyonId)
                {
                    lokasyonIdCounter = new { Id = (int?)suKaynak.LokasyonId, Name = lokasyonlar.Where(x => x.Id == suKaynak.LokasyonId).First().Name };
                }

                if (suKaynak.CalismaSureEtiketId.GetValueOrDefault(0) == 0) continue;

                var data = GetTotalCalismaSureEski(etiketLogVeriler, suKaynak, baslangicZamani, bitisZamani, raporlamaTip);
                var model = new SuKaynakRaporModel();
                model.LokasyonName = lokasyonIdCounter.Name;
                model.Kategori = "Çalışma Süresi (saat)";
                model.Nokta = suKaynak.Name;
                model.ZamansalVeri = new Dictionary<string, decimal>();
                for (int i = 0; i < data.Count; i++)
                {
                    model.ZamansalVeri[data[i].PeriyotName] = decimal.Round(data[i].Toplam, 2, MidpointRounding.AwayFromZero);
                }
                result.Add(model);
            }
            return result;
        }

        public List<string> GetYearColumns(DateTime startDate, DateTime endDate)
        {
            List<string> years = new();
            DateTime current = new DateTime(startDate.Year, 1, 1);

            while (current <= endDate)
            {
                string yearName = current.ToString("yyyy", new CultureInfo("tr-TR"));
                years.Add(yearName);
                current = current.AddYears(1);
            }

            return years;
        }

        public List<string> GetWeekColumns(DateTime startDate, DateTime endDate)
        {
            List<string> weeks = new();
            DateTime current = startDate;

            while (current.DayOfWeek != DayOfWeek.Monday)
            {
                current = current.AddDays(-1);
            }

            while (current <= endDate)
            {
                string weekName = $"Hafta {CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(current, CalendarWeekRule.FirstDay, DayOfWeek.Monday)} {current.ToString("yyyy", new CultureInfo("tr-TR"))}";
                weeks.Add(weekName);
                current = current.AddDays(7);
            }

            return weeks;
        }

        public List<string> GetDayColumns(DateTime startDate, DateTime endDate)
        {
            List<string> months = new();
            DateTime current = new(startDate.Year, startDate.Month, 1);

            while (current <= endDate)
            {
                string dayName = current.ToString("dd MM yyyy dddd", new CultureInfo("tr-TR"));
                months.Add(dayName);
                current = current.AddDays(1);
            }

            return months;
        }

        public List<string> GetMonthColumns(DateTime startDate, DateTime endDate)
        {
            List<string> months = new();
            DateTime current = new(startDate.Year, startDate.Month, 1);

            while (current <= endDate)
            {
                string monthName = current.ToString("MMMM yyyy", new CultureInfo("tr-TR")); // "Ocak 2024"
                months.Add(monthName);
                //months.Add($"{monthName} Toplam");
                current = current.AddMonths(1);
            }

            return months;
        }

        public async Task<List<Lokasyon>?> GetLokasyonlarWithApi()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var requestUri = new Uri($"{ApiUrls.SharedAppBaseUrl}/Lokasyon/GetAllList");

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = requestUri
                    };

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var lokasyon = JsonSerializer.Deserialize<List<Lokasyon>?>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return lokasyon;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<SuHatPlcAyarlarModel>> GetSuHatPlcAyarlar()
        {
            List<SuHatPlcAyarlarModel> result = new List<SuHatPlcAyarlarModel>();

            var suKaynakPlcAyarlar = await _plcAyarlarRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true);
            foreach (var plcAyar in suKaynakPlcAyarlar)
            {
                var model = new SuHatPlcAyarlarModel();
                model.SuKaynakId = plcAyar.Id;
                model.Name = plcAyar.Name;
                model.PlcIpAdres = plcAyar.PlcIpAdres;
                model.PlcAutoConnect = plcAyar.PlcAutoConnect;
                model.Aktif = plcAyar.Aktif;
                model.ServisApiUrl = plcAyar.ServisApiUrl;
                model.PlcBagli = await GetPlcConnectedAndReadingByKumesIdWithApi(plcAyar.Id, plcAyar.ServisApiUrl);
                result.Add(model);
            }
            return result;
        }

        public async Task<SuHatPlcAyarlarModel> GetSuHatPlcAyarlarBySuLokasyonId(int suLokasyonId)
        {
            var suKaynakPlcAyar = await _plcAyarlarRepository.GetAsyncByPredicate(x => x.Id == suLokasyonId && x.Silindi != true);

            var model = new SuHatPlcAyarlarModel();
            model.SuKaynakId = suKaynakPlcAyar.Id;
            model.Name = suKaynakPlcAyar.Name;
            model.PlcIpAdres = suKaynakPlcAyar.PlcIpAdres;
            model.PlcAutoConnect = suKaynakPlcAyar.PlcAutoConnect;
            model.Aktif = suKaynakPlcAyar.Aktif;
            model.ServisApiUrl = suKaynakPlcAyar.ServisApiUrl;
            model.PlcBagli = await GetPlcConnectedAndReadingByKumesIdWithApi(suKaynakPlcAyar.Id, suKaynakPlcAyar.ServisApiUrl);

            return model;
        }

        public async Task SuKaynakHaberlesmeBilgileriDegistir(int suLokasyonId, string plcIpAdres, bool plcAutoConnect, bool aktif)
        {
            var plcAyar = await _plcAyarlarRepository.GetByIdAsync(suLokasyonId);
            if (plcAyar == null)
            {
                throw new Exception("Su Kaynak Plc Ayar bulunamadı");
            }
            if (plcAyar.PlcIpAdres != plcIpAdres)
            {
                plcAyar.PlcIpAdresFlag = true;
                plcAyar.PlcNewIpAdres = plcIpAdres;
                await _plcAyarlarRepository.SaveChangesAsync();
                var result = await PlcCheckFlagByKumesIdWithApi(suLokasyonId, plcAyar.ServisApiUrl);
            }
            else if (plcAyar.PlcAutoConnect != plcAutoConnect)
            {
                plcAyar.PlcAutoConnectFlag = true;
                plcAyar.PlcNewAutoConnect = plcAutoConnect;
                await _plcAyarlarRepository.SaveChangesAsync();
                var result = await PlcCheckFlagByKumesIdWithApi(suLokasyonId, plcAyar.ServisApiUrl);
            }
            else if (plcAyar.Aktif != aktif)
            {
                plcAyar.PlcAktifFlag = true;
                plcAyar.PlcNewAktif = aktif;
                await _plcAyarlarRepository.SaveChangesAsync();
                var result = await PlcCheckFlagByKumesIdWithApi(suLokasyonId, plcAyar.ServisApiUrl);
            }
        }

        public async Task<bool> GetPlcConnectedAndReadingByKumesIdWithApi(int suLokasyonId, string serviceApiUrl)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(serviceApiUrl + SuIzlemeWorkerUrls.GetPlcConnectedAndReading)
                    };

                    var uriBuilder = new UriBuilder(request.RequestUri);
                    var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                    query["suLokasyonId"] = suLokasyonId.ToString();
                    uriBuilder.Query = query.ToString();
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.StatusCode == HttpStatusCode.NoContent)//todo bu blok silinecek galiba
                    {
                        return false;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        // Sonuç boş veya hatalı ise false döndür
                        if (string.IsNullOrEmpty(result))
                        {
                            return false;
                        }

                        var isPlcConnected = JsonSerializer.Deserialize<bool>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return isPlcConnected;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout durumu
                return false;
            }
            catch (HttpRequestException)
            {
                // HTTP isteği ile ilgili herhangi bir hata
                return false;
            }
            catch (Exception)
            {
                // Diğer genel hatalar
                return false;
            }
        }

        public async Task<bool> PlcCheckFlagByKumesIdWithApi(int suLokasyonId, string serviceApiUrl)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Put,
                        RequestUri = new Uri(serviceApiUrl + SuIzlemeWorkerUrls.CheckFlag)
                    };

                    var uriBuilder = new UriBuilder(request.RequestUri);
                    var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                    query["suLokasyonId"] = suLokasyonId.ToString();
                    uriBuilder.Query = query.ToString();
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.StatusCode == HttpStatusCode.NoContent)//todo bu blok silinecek galiba
                    {
                        return false;
                    }

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        // Sonuç boş veya hatalı ise false döndür
                        if (string.IsNullOrEmpty(result))
                        {
                            return false;
                        }
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout durumu
                return false;
            }
            catch (HttpRequestException)
            {
                // HTTP isteği ile ilgili herhangi bir hata
                return false;
            }
            catch (Exception)
            {
                // Diğer genel hatalar
                return false;
            }
        }

        public async Task<List<SuHatRaporLokasyon>> GetRaporLokasyonlarByCizimId(int cizimId)
        {
            var lokasyonlar = await _suKaynakRepository.GetVwCizimLokasyonEslemeFromCizimId(cizimId);

            if (lokasyonlar == null || !lokasyonlar.Any())
            {
                throw new Exception("Lokasyonlar bulunamadı");
            }


            return lokasyonlar
                .Where(x => x.LokasyonId != DogalgazliKazanLokasyonId)
                .GroupBy(x => x.LokasyonId)
                .Select(x => new SuHatRaporLokasyon
                {
                    Id = x.Key,
                    Name = x.First().LokasyonName
                })
                .ToList();
        }

        /// <summary>
        /// Kullanıcının seçtiği lokasyonlara göre veritabanında
        /// sorgulanması gereken gerçek lokasyonları oluşturur.
        /// Kesimhane seçildiyse Doğalgazlı Kazan da sorguya eklenir.
        /// </summary>
        private static List<int> GetSorgulanacakLokasyonIds(
            IEnumerable<int> secilenLokasyonIds)
        {
            var sorgulanacakLokasyonIds = secilenLokasyonIds
                .Distinct()
                .ToList();

            if (sorgulanacakLokasyonIds.Contains(KesimhaneLokasyonId) &&
                !sorgulanacakLokasyonIds.Contains(DogalgazliKazanLokasyonId))
            {
                sorgulanacakLokasyonIds.Add(DogalgazliKazanLokasyonId);
            }

            return sorgulanacakLokasyonIds;
        }

        /// <summary>
        /// Verinin raporda hangi ana lokasyon altında gösterileceğini belirler.
        /// Doğalgazlı Kazan verileri Kesimhane altında gösterilir.
        /// </summary>
        private static int GetRaporLokasyonId(int? lokasyonId)
        {
            if (!lokasyonId.HasValue)
            {
                return int.MaxValue;
            }

            return lokasyonId.Value == DogalgazliKazanLokasyonId
                ? KesimhaneLokasyonId
                : lokasyonId.Value;
        }
    }
}