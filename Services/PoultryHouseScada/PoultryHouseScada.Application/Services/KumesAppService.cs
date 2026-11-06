using PoultryHouseScada.Application.Interfaces;
using PoultryHouseScada.Application.ViewModels;
using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Domain.Models.ReportModels;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using PanelIdentification.Application.Services.Core;
using PanelIdentification.Domain.Models.ReportModels;
using SharedApp.Domain.Models;
using System.Globalization;
using System.Net;
using System.Text.Json;
using static MatliOtomasyon.Shared.Infrastructure.Configuration.ApiUrls;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;
using MatliOtomasyon.Shared.Infrastructure.Configuration;

namespace PoultryHouseScada.Application.Services
{
    public class KumesAppService : AppService<Kumes, KumesViewModel>, IKumesAppService
    {
        private readonly IKumesRepository _kumesRepository;
        private readonly IDonemRepository _donemRepository;
        private readonly IKumesLogTempRepository _kumesLogTempRepository;
        private readonly IKumesLogRepository _kumesLogRepository;
        private readonly IKumesHayvanLogRepository _kumesHayvanLogRepository;
        //private readonly IKumesHayvanRepository _kumesHayvanRepository;
        private readonly IAlarmLogRepository _alarmLogRepository;
        private readonly IEtiketLogTempRepository _etiketLogTempRepository;
        private readonly IKumesRaporRehberRepository _kumesRaporRehberRepository;
        private readonly IKumesHaftalikCatiKontrolRepository _kumesHaftalikCatiKontrolRepository;
        private readonly IServiceProvider _serviceProvider;

        public KumesAppService(IMapper mapper, IKumesRepository kumesRepository, IDonemRepository donemRepository, IKumesLogTempRepository kumesLogTempRepository, IKumesLogRepository kumesLogRepository,
            /*IKumesHayvanRepository kumesHayvanRepository,*/ IKumesHayvanLogRepository kumesHayvanLogRepository, IAlarmLogRepository alarmLogRepository, IEtiketLogTempRepository etiketLogTempRepository,
            IKumesRaporRehberRepository kumesRaporRehberRepository, IKumesHaftalikCatiKontrolRepository kumesHaftalikCatiKontrolRepository, IServiceProvider serviceProvider) : base(mapper, kumesRepository)
        {
            _kumesRepository = kumesRepository;
            _donemRepository = donemRepository;
            _kumesLogTempRepository = kumesLogTempRepository;
            _kumesLogRepository = kumesLogRepository;
            //_kumesHayvanRepository = kumesHayvanRepository;
            _kumesHayvanLogRepository = kumesHayvanLogRepository;
            _alarmLogRepository = alarmLogRepository;
            _etiketLogTempRepository = etiketLogTempRepository;
            _kumesRaporRehberRepository = kumesRaporRehberRepository;
            _kumesHaftalikCatiKontrolRepository = kumesHaftalikCatiKontrolRepository;
            _serviceProvider = serviceProvider;
        }
        public async Task<List<Kumes>> GetAllKumesForYetki()
        {
            var listKumes = new List<Kumes>();

            var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true);
            listKumes.AddRange(kumesler);

            return listKumes;
        }
        public async Task<List<int>> GetLokasyonIdsByKumesIds(List<int> kumesIds)
        {
            var lokasyonList = (await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => kumesIds.Contains(x.Id) && x.Silindi != true)).Select(x => x.LokasyonId).ToList();
            return lokasyonList;
        }

        public async Task<List<int>> GetKumesIdsByLokasyonIds(List<int> lokasyonIds)
        {
            var kumesList = (await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => lokasyonIds.Contains(x.LokasyonId) && x.Silindi != true)).Select(x => x.Id).ToList();
            return kumesList;
        }

        public async Task<List<int>> GetKumesIdsByYetki(string userId)
        {
            List<int> kumesIds = new List<int>();
            var userKumesYetkis = await _kumesRepository.GetAllUserKumesYetkiByUserId(userId);
            var userKumesYetkiler = userKumesYetkis.Where(x => x.SorumluMu.GetValueOrDefault(false) && x.Aktif.GetValueOrDefault(true) && x.Silindi != true).ToList();

            foreach (var userKumesYetki in userKumesYetkiler)
            {
                // Her iterasyonda yeni bir NodeYetkiModel nesnesi oluştur
                var nodeYetki = new NodeYetkiModel();

                if (userKumesYetki.KumesId > 0)
                {
                    kumesIds.Add(userKumesYetki.Id);
                }
                else if (userKumesYetki.LokasyonId > 0)
                {
                    var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == userKumesYetki.LokasyonId && x.Silindi != true);
                    kumesIds.AddRange(kumesler.Select(x => x.Id).ToList());
                }
                else if (userKumesYetki.SirketId > 0)
                {
                    var sirketLokasyonlar = await _kumesRepository.GetAllLokasyonBySirketId(userKumesYetki.SirketId);

                    foreach (var lokasyon in sirketLokasyonlar)
                    {
                        var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyon.Id && x.Silindi != true);
                        kumesIds.AddRange(kumesler.Select(x => x.Id).ToList());
                    }
                }
            }
            return kumesIds;
        }

        public async Task<List<TesisGenelIzlemeModel>> GetTesisGenelIzlemeByLokasyonId(int lokasyonId, string userId)
        {
            try
            {
                //var yetkiliKumesler = await GetKumesIdsByYetki(userId);
                List<int> yetkiliKumesler = new List<int>();
                var userKumesYetkis = await GetAllUserKumesYetkiByUserIdWithApi(userId);
                var userKumesYetkiler = userKumesYetkis.Where(x => x.SorumluMu.GetValueOrDefault(false) && x.Aktif.GetValueOrDefault(true) && x.Silindi != true).ToList();
                var userKumesYemRasyon = userKumesYetkis.Where(x => x.YemRasyonYetki.GetValueOrDefault(false) && x.Aktif.GetValueOrDefault(true) && x.Silindi != true).ToList();
                var userKumesRaporRehber = userKumesYetkis.Where(x => x.RaporRehberYetki.GetValueOrDefault(false) && x.Aktif.GetValueOrDefault(true) && x.Silindi != true).ToList();
                foreach (var userKumesYetki in userKumesYetkiler)
                {
                    if (userKumesYetki.KumesId > 0)
                    {
                        yetkiliKumesler.Add(userKumesYetki.KumesId);
                    }
                    else if (userKumesYetki.LokasyonId > 0)
                    {
                        var kumesbul = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == userKumesYetki.LokasyonId && x.Silindi != true);
                        yetkiliKumesler.AddRange(kumesbul.Select(x => x.Id).ToList());
                    }
                    else if (userKumesYetki.SirketId > 0)
                    {
                        var sirketLokasyonlar = await _kumesRepository.GetAllLokasyonBySirketId(userKumesYetki.SirketId);

                        foreach (var lokasyon in sirketLokasyonlar)
                        {
                            var kumesbul = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyon.Id && x.Silindi != true);
                            yetkiliKumesler.AddRange(kumesbul.Select(x => x.Id).ToList());
                        }
                    }
                }

                List<TesisGenelIzlemeModel> result = new List<TesisGenelIzlemeModel>();
                //var lokasyon = await GetLokasyonByIdWithApi(lokasyonId);
                var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true);
                foreach (var kumes in kumesler)
                {
                    TesisGenelIzlemeModel model = new TesisGenelIzlemeModel();
                    model.KumesId = kumes.Id;
                    model.Aktif = kumes.Aktif.GetValueOrDefault(true);
                    model.KumesName = kumes.Name;
                    //model.CizimId = kumes.CizimId;
                    var donem = await _donemRepository.GetAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.Silindi != true && x.Aktif != false && x.CikisTarihi == null);
                    if (donem != null)
                    {
                        model.DonemId = donem.Id;
                        model.DonemName = donem == null ? "" : donem.Name == null ? "" : donem.Name;
                        var now = DateTime.Now;
                        if (donem.GirisTarihi != null)
                        {
                            model.DonemGun = (now - donem.GirisTarihi.Value).Days.ToString();
                            // Haftaları hesapla
                            int haftaSayisi = (int)Math.Ceiling(Convert.ToInt32(model.DonemGun) / 7.0);

                            // Haftanın başlangıç günü olarak Pazartesi hesaplama
                            DateTime ilkHaftaBaslangici = donem.GirisTarihi.Value;
                            if (donem.GirisTarihi.Value.DayOfWeek != DayOfWeek.Monday)
                            {
                                // İlk haftanın başına git
                                int daysToSubtract = ((int)donem.GirisTarihi.Value.DayOfWeek + 6) % 7;
                                ilkHaftaBaslangici = donem.GirisTarihi.Value.AddDays(-daysToSubtract);
                            }

                            // Haftalar arası günleri kontrol et
                            int toplamHaftaSayisi = (int)((now - ilkHaftaBaslangici).Days / 7) + 1;
                            model.DonemHafta = toplamHaftaSayisi.ToString();
                        }
                    }
                    var oluKumesHayvan = new List<KumesHayvanLog>();
                    if (donem != null)
                    {
                        oluKumesHayvan = await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.DonemId == donem.Id && x.Silindi != true && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu);
                    }
                    else
                    {
                        oluKumesHayvan = null;
                    }

                    //model.PlcBagli = await GetPlcConnectedAndReadingByKumesIdWithApi(kumes.Id, kumes.ServisApiUrl); 
                    var plcBagliEtiket = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.EtiketId == KumesEtiketTipler.Plc_Bagli && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-40))).OrderByDescending(x => x.Id).FirstOrDefault();
                    model.PlcBagli = plcBagliEtiket == null ? false : plcBagliEtiket.Deger == 1 ? true : false;

                    //todo 2 dk öncesine ayarlandı ama başka bir çözüm vardır belki
                    var kumesLog = (await _kumesLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-40) && x.Silindi != true)).OrderByDescending(x => x.Id).FirstOrDefault();
                    if (kumesLog != null)
                    {
                        model.SicaklikKumes = kumesLog.SicaklikKumes.GetValueOrDefault(0).ToString("N1", new CultureInfo("tr-TR"));
                        model.NemKumes = kumesLog.NemKumes.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR"));
                        model.Co2Kumes = kumesLog.Co2_1.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR"));
                        model.BasincKumes = kumesLog.Basinc1.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR"));
                        model.YumurtaAdet = kumesLog.YumurtaAdet.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR"));
                        model.SuTuketimBugun = kumesLog.SuTuketimBugun.GetValueOrDefault(0).ToString("N1", new CultureInfo("tr-TR"));
                        model.YemTuketimBugun = kumesLog.YemTuketimBugun.GetValueOrDefault(0).ToString("N1", new CultureInfo("tr-TR"));
                    }
                    model.HayvanAdet = (donem != null ? (donem.HayvanAdet.GetValueOrDefault(0) - oluKumesHayvan.Sum(x => x.Adet.GetValueOrDefault(0))) : 0).ToString("N0", new CultureInfo("tr-TR"));//hasta ve saglikli olanlarin toplami
                    model.OluHayvanAdet = oluKumesHayvan != null ? oluKumesHayvan.FirstOrDefault(x => x.KayitZamani == DateTime.Now.Date)?.Adet.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR")) : "0";

                    var aktifAlarm = await _alarmLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Aktif != false && x.Silindi != true);

                    if (aktifAlarm.Count() > 0)
                    {
                        model.AlarmVar = true;
                        if (plcBagliEtiket?.Deger == 1)
                        {
                            model.AlarmMesajList = aktifAlarm.Select(x => x.Mesaj ?? "").ToList();
                        }
                        else
                        {
                            model.AlarmMesajList = aktifAlarm.Where(x => x.Mesaj.ToLower().Contains("plc")).Select(x => x.Mesaj ?? "").ToList();
                        }
                    }
                    else
                    {
                        model.AlarmVar = false;
                        model.AlarmMesajList = null;
                    }

                    if (yetkiliKumesler.Contains(kumes.Id)) model.SorumluMu = true;
                    else model.SorumluMu = false;
                    if (userKumesYemRasyon.Count() > 0) model.YemRasyonYetki = true;
                    else model.YemRasyonYetki = false;
                    if (userKumesRaporRehber.Count() > 0) model.RaporRehberYetki = true;
                    else model.RaporRehberYetki = false;
                    //model.KumesServisApiUrl = kumes.ServisApiUrl;
                    result.Add(model);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<TesisGenelIzlemeModel>> GetTesisGenelIzlemeByLokasyonIdYeni(int lokasyonId, string userId)
        {
            try
            {
                //var yetkiliKumesler = await GetKumesIdsByYetki(userId);
                List<int> yetkiliKumesler = new List<int>();
                var userKumesYetkis =
    await GetAllUserKumesYetkiByUserIdWithApi(userId);

                var aktifYetkiler = userKumesYetkis
                    .Where(x =>
                        x.Aktif.GetValueOrDefault(true) &&
                        x.Silindi != true)
                    .ToList();

                var sorumluKumesIds = new HashSet<int>();
                var yemRasyonYetkiliKumesIds = new HashSet<int>();
                var raporRehberYetkiliKumesIds = new HashSet<int>();

                foreach (var yetki in aktifYetkiler)
                {
                    var yetkininKapsadigiKumesIds = new HashSet<int>();

                    // Yetki doğrudan kümese verilmişse
                    if (yetki.KumesId > 0)
                    {
                        yetkininKapsadigiKumesIds.Add(yetki.KumesId);
                    }
                    // Yetki lokasyona verilmişse lokasyondaki bütün kümesleri kapsar
                    else if (yetki.LokasyonId > 0)
                    {
                        var lokasyonKumesleri =
                            await _kumesRepository
                                .GetAllAsyncByPredicateWithAsNoTracking(
                                    x =>
                                        x.LokasyonId == yetki.LokasyonId &&
                                        x.Silindi != true);

                        yetkininKapsadigiKumesIds.UnionWith(
                            lokasyonKumesleri.Select(x => x.Id));
                    }
                    // Yetki şirkete verilmişse şirketin bütün lokasyonlarındaki
                    // kümesleri kapsar
                    else if (yetki.SirketId > 0)
                    {
                        var sirketLokasyonlari = await _kumesRepository.GetAllLokasyonBySirketId(yetki.SirketId);

                        foreach (var lokasyon in sirketLokasyonlari)
                        {
                            var lokasyonKumesleri = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyon.Id && x.Silindi != true);
                            yetkininKapsadigiKumesIds.UnionWith(lokasyonKumesleri.Select(x => x.Id));
                        }
                    }

                    if (yetki.SorumluMu.GetValueOrDefault(false))
                    {
                        sorumluKumesIds.UnionWith(yetkininKapsadigiKumesIds);
                    }

                    if (yetki.YemRasyonYetki.GetValueOrDefault(false))
                    {
                        yemRasyonYetkiliKumesIds.UnionWith(yetkininKapsadigiKumesIds);
                    }

                    if (yetki.RaporRehberYetki.GetValueOrDefault(false))
                    {
                        raporRehberYetkiliKumesIds.UnionWith(yetkininKapsadigiKumesIds);
                    }
                }

                List<TesisGenelIzlemeModel> result = new List<TesisGenelIzlemeModel>();
                //var lokasyon = await GetLokasyonByIdWithApi(lokasyonId);
                var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true);
                foreach (var kumes in kumesler)
                {
                    TesisGenelIzlemeModel model = new TesisGenelIzlemeModel();
                    model.KumesId = kumes.Id;
                    model.Aktif = kumes.Aktif.GetValueOrDefault(true);
                    model.KumesName = kumes.Name;
                    //model.CizimId = kumes.CizimId;
                    var donem = await _donemRepository.GetAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.Silindi != true && x.Aktif != false && x.CikisTarihi == null);
                    if (donem != null)
                    {
                        model.DonemId = donem.Id;
                        model.DonemName = donem == null ? "" : donem.Name == null ? "" : donem.Name;
                        var now = DateTime.Now;
                        if (donem.GirisTarihi != null)
                        {
                            model.DonemGun = (now - donem.GirisTarihi.Value).Days.ToString();
                            // Haftaları hesapla
                            int haftaSayisi = (int)Math.Ceiling(Convert.ToInt32(model.DonemGun) / 7.0);

                            // Haftanın başlangıç günü olarak Pazartesi hesaplama
                            DateTime ilkHaftaBaslangici = donem.GirisTarihi.Value;
                            if (donem.GirisTarihi.Value.DayOfWeek != DayOfWeek.Monday)
                            {
                                // İlk haftanın başına git
                                int daysToSubtract = ((int)donem.GirisTarihi.Value.DayOfWeek + 6) % 7;
                                ilkHaftaBaslangici = donem.GirisTarihi.Value.AddDays(-daysToSubtract);
                            }

                            // Haftalar arası günleri kontrol et
                            int toplamHaftaSayisi = (int)((now - ilkHaftaBaslangici).Days / 7) + 1;
                            model.DonemHafta = toplamHaftaSayisi.ToString();
                        }
                    }
                    var oluKumesHayvan = new List<KumesHayvanLog>();
                    if (donem != null)
                    {
                        oluKumesHayvan = await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.DonemId == donem.Id && x.Silindi != true && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu);
                    }
                    else
                    {
                        oluKumesHayvan = null;
                    }

                    //model.PlcBagli = await GetPlcConnectedAndReadingByKumesIdWithApi(kumes.Id, kumes.ServisApiUrl); 
                    var plcBagliEtiket = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.EtiketId == KumesEtiketTipler.Plc_Bagli && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-40))).OrderByDescending(x => x.Id).FirstOrDefault();
                    model.PlcBagli = plcBagliEtiket == null ? false : plcBagliEtiket.Deger == 1 ? true : false;

                    //todo 2 dk öncesine ayarlandı ama başka bir çözüm vardır belki
                    var kumesLog = (await _kumesLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-40) && x.Silindi != true)).OrderByDescending(x => x.Id).FirstOrDefault();
                    if (kumesLog != null)
                    {
                        model.SicaklikKumes = kumesLog.SicaklikKumes.GetValueOrDefault(0).ToString("N1", new CultureInfo("tr-TR"));
                        model.NemKumes = kumesLog.NemKumes.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR"));
                        model.Co2Kumes = kumesLog.Co2_1.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR"));
                        model.BasincKumes = kumesLog.Basinc1.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR"));
                        model.YumurtaAdet = kumesLog.YumurtaAdet.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR"));
                        model.SuTuketimBugun = kumesLog.SuTuketimBugun.GetValueOrDefault(0).ToString("N1", new CultureInfo("tr-TR"));
                        model.YemTuketimBugun = kumesLog.YemTuketimBugun.GetValueOrDefault(0).ToString("N1", new CultureInfo("tr-TR"));
                    }
                    model.HayvanAdet = (donem != null ? (donem.HayvanAdet.GetValueOrDefault(0) - oluKumesHayvan.Sum(x => x.Adet.GetValueOrDefault(0))) : 0).ToString("N0", new CultureInfo("tr-TR"));//hasta ve saglikli olanlarin toplami
                    model.OluHayvanAdet = oluKumesHayvan != null ? oluKumesHayvan.FirstOrDefault(x => x.KayitZamani == DateTime.Now.Date)?.Adet.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR")) : "0";

                    var aktifAlarm = await _alarmLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Aktif != false && x.Silindi != true);

                    if (aktifAlarm.Count() > 0)
                    {
                        model.AlarmVar = true;
                        if (plcBagliEtiket?.Deger == 1)
                        {
                            model.AlarmMesajList = aktifAlarm.Select(x => x.Mesaj ?? "").ToList();
                        }
                        else
                        {
                            model.AlarmMesajList = aktifAlarm.Where(x => x.Mesaj.ToLower().Contains("plc")).Select(x => x.Mesaj ?? "").ToList();
                        }
                    }
                    else
                    {
                        model.AlarmVar = false;
                        model.AlarmMesajList = null;
                    }

                    model.SorumluMu = sorumluKumesIds.Contains(kumes.Id);
                    model.YemRasyonYetki = yemRasyonYetkiliKumesIds.Contains(kumes.Id);
                    model.RaporRehberYetki = raporRehberYetkiliKumesIds.Contains(kumes.Id);
                    //model.KumesServisApiUrl = kumes.ServisApiUrl;
                    result.Add(model);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<TesisGenelIzlemeModel>> GetTesisGenelIzlemeByLokasyonId(int lokasyonId)
        {
            try
            {
                //var yetkiliKumesler = await GetKumesIdsByYetki(userId);

                List<TesisGenelIzlemeModel> result = new List<TesisGenelIzlemeModel>();
                //var lokasyon = await GetLokasyonByIdWithApi(lokasyonId);
                var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true);
                foreach (var kumes in kumesler)
                {
                    TesisGenelIzlemeModel model = new TesisGenelIzlemeModel();
                    model.KumesId = kumes.Id;
                    model.Aktif = kumes.Aktif.GetValueOrDefault(true);
                    model.KumesName = kumes.Name;
                    //model.CizimId = kumes.CizimId;
                    var donem = await _donemRepository.GetAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.Silindi != true && x.Aktif != false && x.CikisTarihi == null);
                    if (donem != null)
                    {
                        model.DonemId = donem.Id;
                        model.DonemName = donem == null ? "" : donem.Name == null ? "" : donem.Name;
                        var now = DateTime.Now;
                        if (donem.GirisTarihi != null)
                        {
                            model.DonemGun = (now - donem.GirisTarihi.Value).Days.ToString();
                            // Haftaları hesapla
                            int haftaSayisi = (int)Math.Ceiling(Convert.ToInt32(model.DonemGun) / 7.0);

                            // Haftanın başlangıç günü olarak Pazartesi hesaplama
                            DateTime ilkHaftaBaslangici = donem.GirisTarihi.Value;
                            if (donem.GirisTarihi.Value.DayOfWeek != DayOfWeek.Monday)
                            {
                                // İlk haftanın başına git
                                int daysToSubtract = ((int)donem.GirisTarihi.Value.DayOfWeek + 6) % 7;
                                ilkHaftaBaslangici = donem.GirisTarihi.Value.AddDays(-daysToSubtract);
                            }

                            // Haftalar arası günleri kontrol et
                            int toplamHaftaSayisi = (int)((now - ilkHaftaBaslangici).Days / 7) + 1;
                            model.DonemHafta = toplamHaftaSayisi.ToString();
                        }
                    }
                    var oluKumesHayvan = new List<KumesHayvanLog>();
                    if (donem != null)
                    {
                        oluKumesHayvan = await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.DonemId == donem.Id && x.Silindi != true && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu);
                    }
                    else
                    {
                        oluKumesHayvan = null;
                    }

                    //model.PlcBagli = await GetPlcConnectedAndReadingByKumesIdWithApi(kumes.Id, kumes.ServisApiUrl); 
                    var plcBagliEtiket = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.EtiketId == KumesEtiketTipler.Plc_Bagli && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-40))).OrderByDescending(x => x.Id).FirstOrDefault();
                    model.PlcBagli = plcBagliEtiket == null ? false : plcBagliEtiket.Deger == 1 ? true : false;

                    //todo 2 dk öncesine ayarlandı ama başka bir çözüm vardır belki
                    var kumesLog = (await _kumesLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-40) && x.Silindi != true)).OrderByDescending(x => x.Id).FirstOrDefault();
                    if (kumesLog != null)
                    {
                        model.SicaklikKumes = kumesLog.SicaklikKumes.GetValueOrDefault(0).ToString("N2", new CultureInfo("tr-TR"));
                        model.NemKumes = kumesLog.NemKumes.GetValueOrDefault(0).ToString("N2", new CultureInfo("tr-TR"));
                        model.Co2Kumes = kumesLog.Co2_1.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR"));
                        model.BasincKumes = kumesLog.Basinc1.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR"));
                        //model.YumurtaAdet = kumesLog.YumurtaAdet.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR"));
                        model.SuTuketimBugun = kumesLog.SuTuketimBugun.GetValueOrDefault(0).ToString("N2", new CultureInfo("tr-TR"));
                        model.YemTuketimBugun = kumesLog.YemTuketimBugun.GetValueOrDefault(0).ToString("N2", new CultureInfo("tr-TR"));
                    }
                    model.HayvanAdet = (donem != null ? (donem.HayvanAdet.GetValueOrDefault(0) - oluKumesHayvan.Sum(x => x.Adet.GetValueOrDefault(0))) : 0).ToString("N0", new CultureInfo("tr-TR"));//hasta ve saglikli olanlarin toplami
                    model.OluHayvanAdet = oluKumesHayvan != null ? oluKumesHayvan.FirstOrDefault(x => x.KayitZamani == DateTime.Now.Date)?.Adet.GetValueOrDefault(0).ToString("N0", new CultureInfo("tr-TR")) : "0";

                    var aktifAlarm = await _alarmLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id /*&& x.KayitZamani.Value.Date == DateTime.Now.Date*/ && x.Aktif != false && x.Silindi != true);

                    if (aktifAlarm.Count() > 0)
                    {
                        model.AlarmVar = true;
                        if (plcBagliEtiket?.Deger == 1)
                        {
                            model.AlarmMesajList = aktifAlarm.Select(x => x.Mesaj ?? "").ToList();
                        }
                        else
                        {
                            model.AlarmMesajList = aktifAlarm.Where(x => x.Mesaj.ToLower().Contains("plc")).Select(x => x.Mesaj ?? "").ToList();
                        }
                    }
                    else
                    {
                        model.AlarmVar = false;
                        model.AlarmMesajList = null;
                    }

                    //if (yetkiliKumesler.Contains(kumes.Id)) model.SorumluMu = true;
                    //else model.SorumluMu = false;
                    model.SorumluMu = false;
                    //model.KumesServisApiUrl = kumes.ServisApiUrl;
                    result.Add(model);
                }
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<TesisGenelKartModel> GetTesisGenelKartByLokasyonId(int lokasyonId)
        {
            try
            {
                var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true);
                if (!kumesler.Any())
                    return new TesisGenelKartModel();
                var kumesIdList = kumesler.Select(x => x.Id).ToList();
                var result = new TesisGenelKartModel
                {
                    KumesSayisi = kumesler.Count()
                };

                result.KumesSayisi = kumesler.Count();
                var aktifDonemIds = (await _donemRepository.GetAllAsyncByPredicateWithAsNoTracking(x => kumesIdList.Contains(x.KumesId.GetValueOrDefault(0)) && (x.Aktif ?? false) == true)).Select(x => new { x.Id, x.KumesId }).ToList();

                var aktifDonemIdListesi = aktifDonemIds.Select(y => y.Id).ToList();

                var tumHayvanKayitlar = await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(
                    x => aktifDonemIdListesi.Contains(x.DonemId ?? 0) && x.Silindi != true);

                var canliHayvanKayitlar = tumHayvanKayitlar.Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli).ToList();
                var oluHayvanKayitlar = tumHayvanKayitlar.Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Olu).ToList();

                // hiç hayvan kaydı yoksa null, varsa gerçek toplam
                result.CanliHayvanAdet = tumHayvanKayitlar.Any()
                    ? canliHayvanKayitlar.Sum(x => x.Adet.GetValueOrDefault(0)) - oluHayvanKayitlar.Sum(x => x.Adet.GetValueOrDefault(0))
                    : (int?)null;

                var bugunOlenler = oluHayvanKayitlar.Where(x => x.KayitZamani.HasValue && x.KayitZamani.Value.Date == DateTime.Now.Date).ToList();
                result.BugunOlenHayvanAdet = bugunOlenler.Any()
                    ? bugunOlenler.Sum(x => x.Adet.GetValueOrDefault(0))
                    : (int?)null;

                var dunOlenler = oluHayvanKayitlar.Where(x => x.KayitZamani.HasValue && x.KayitZamani.Value.Date == DateTime.Now.Date.AddDays(-1)).ToList();
                result.DunOlenHayvanAdet = dunOlenler.Any()
                    ? dunOlenler.Sum(x => x.Adet.GetValueOrDefault(0))
                    : (int?)null;

                if (result.CanliHayvanAdet.GetValueOrDefault(0) < 0)
                    return result;

                var sonKayitlar = await _kumesLogRepository.GetLastKumesLogsByKumesIds(kumesIdList);
                result.ToplamSuTuketim =
                    sonKayitlar != null && sonKayitlar.Any(x => x.SuTuketimBugun.HasValue)
                        ? sonKayitlar.Where(x => x.SuTuketimBugun.HasValue)
                                     .Sum(x => x.SuTuketimBugun!.Value)
                        : null; result.PetSuTuketim = null;
                result.ToplamYemTuketim =
                    sonKayitlar != null && sonKayitlar.Any(x => x.YemTuketimBugun.HasValue)
                        ? sonKayitlar.Where(x => x.YemTuketimBugun.HasValue)
                                     .Sum(x => x.YemTuketimBugun!.Value)
                        : null;                //result.HayvanBasiSuTuketim = (result.ToplamSuTuketim.GetValueOrDefault(0) * 1000000) / result.CanliHayvanAdet;
                //result.HayvanBasiYemTuketim = (result.ToplamYemTuketim.GetValueOrDefault(0) * 1000000) / result.CanliHayvanAdet;
                decimal? hayvanbasiYemTuketim = 0;
                decimal? hayvanbasiSuTuketim = 0;
                int yemCounter = 0;
                int suCounter = 0;
                foreach (var kumesLog in sonKayitlar)
                {
                    var canliKumesHayvanSayisi = canliHayvanKayitlar.Where(x => x.KumesId == kumesLog?.KumesId).Sum(x => x.Adet.GetValueOrDefault(0));
                    var oluKumesHayvanSayisi = oluHayvanKayitlar.Where(x => x.KumesId == kumesLog?.KumesId).Sum(x => x.Adet.GetValueOrDefault(0));
                    var kumesHayvanSayisi = canliKumesHayvanSayisi - oluKumesHayvanSayisi;
                    if (kumesHayvanSayisi > 0)
                    {
                        if (kumesLog?.YemTuketimBugun.GetValueOrDefault(0) > 0)
                        {
                            hayvanbasiYemTuketim += kumesLog?.YemTuketimBugun.GetValueOrDefault(0) / kumesHayvanSayisi;
                            yemCounter++;
                        }
                        if (kumesLog?.SuTuketimBugun.GetValueOrDefault(0) > 0)
                        {
                            hayvanbasiSuTuketim += kumesLog?.SuTuketimBugun.GetValueOrDefault(0) / kumesHayvanSayisi;
                            suCounter++;
                        }
                    }
                }
                if (yemCounter > 0)
                    result.HayvanBasiYemTuketim = (hayvanbasiYemTuketim * 1000000) / yemCounter;
                //else
                //    result.HayvanBasiYemTuketim = 0;

                if (suCounter > 0)
                    result.HayvanBasiSuTuketim = (hayvanbasiSuTuketim * 1000000) / suCounter;
                //else
                //    result.HayvanBasiSuTuketim = 0;

                var aktifKumesIdListesi = aktifDonemIds.Select(y => y.KumesId).ToList();
                var sicaklikListesi = sonKayitlar?
                    .Where(x => aktifKumesIdListesi.Contains(x.KumesId))
                    .Select(x => x.SicaklikKumes.GetValueOrDefault(0))
                    .ToList();

                result.OrtalamaSicaklik = (sicaklikListesi != null && sicaklikListesi.Any())
                    ? sicaklikListesi.Average()
                    : (decimal?)null;

                var nemListesi = sonKayitlar?
                    .Where(x => aktifKumesIdListesi.Contains(x.KumesId))
                    .Select(x => x.NemKumes.GetValueOrDefault(0))
                    .ToList();

                result.OrtalamaNem = (nemListesi != null && nemListesi.Any())
                    ? nemListesi.Average()
                    : (decimal?)null;
                result.YumurtaAdet = sonKayitlar != null && sonKayitlar.Any(x => x.YumurtaAdet.HasValue) ? sonKayitlar.Where(x => x.YumurtaAdet.HasValue).Sum(x => x.YumurtaAdet!.Value) : null;

                result.HayvanBasiYumurtaAdet =
                    result.YumurtaAdet.HasValue &&
                    result.CanliHayvanAdet.HasValue &&
                    result.CanliHayvanAdet.Value > 0
                        ? (decimal)result.YumurtaAdet.Value / result.CanliHayvanAdet.Value
                        : null;
                //result.YumurtaAdet = null;
                //result.HayvanBasiYumurtaAdet = null;
                return result;
            }
            catch (Exception)
            {
                return new TesisGenelKartModel();
            }
        }

        private async Task<List<TesisGenelIzlemeModel>> GetTesisGenelOrtakKumeslerByLokasyonId(int lokasyonId)
        {
            var result = new List<TesisGenelIzlemeModel>();
            var culture = new CultureInfo("tr-TR");
            var now = DateTime.Now;
            var sonVeriZamani = now.AddSeconds(-40);

            var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true);
            if (!kumesler.Any())
                return result;

            var kumesIdList = kumesler.Select(x => x.Id).ToList();

            /*
             * Eski yapıda kümes başına 5 ayrı sorgu foreach içinde await ediliyordu
             * (N kümes için 5*N round-trip). Aynı DbContext örneği request boyunca
             * paylaşıldığından (scoped) sorgular paralel de çalıştırılamıyor;
             * bu yüzden burada Task.WhenAll yerine sıralı ama toplu (kümes bazında
             * değil, lokasyon bazında tek seferlik) 5 sorguya indiriliyor.
             */
            var donemler = await _donemRepository.GetAllAsyncByPredicateWithAsNoTracking(
                x => kumesIdList.Contains(x.KumesId.GetValueOrDefault(0)) && x.Silindi != true && x.Aktif != false && x.CikisTarihi == null);

            var donemIdList = donemler.Select(x => x.Id).ToList();

            var oluKumesHayvanTumu = donemIdList.Count > 0
                ? await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(
                    x => donemIdList.Contains(x.DonemId.GetValueOrDefault(0)) && x.Silindi != true && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu)
                : new List<KumesHayvanLog>();

            var plcBagliEtiketlerTumu = await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(
                x => kumesIdList.Contains(x.KumesId.GetValueOrDefault(0)) && x.EtiketId == KumesEtiketTipler.Plc_Bagli && x.KayitZamani.Value >= sonVeriZamani);

            var kumesLoglariTumu = await _kumesLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(
                x => kumesIdList.Contains(x.KumesId.GetValueOrDefault(0)) && x.KayitZamani.Value >= sonVeriZamani && x.Silindi != true);

            var aktifAlarmlarTumu = await _alarmLogRepository.GetAllAsyncByPredicateWithAsNoTracking(
                x => kumesIdList.Contains(x.KumesId.GetValueOrDefault(0)) && x.Aktif != false && x.Silindi != true);

            // Aktif dönem, kümes başına tek kayıt varsayılır; birden fazlaysa ilki kullanılır (eski davranışla aynı).
            var donemByKumesId = donemler
                .GroupBy(x => x.KumesId.GetValueOrDefault(0))
                .ToDictionary(g => g.Key, g => g.First());

            var oluHayvanByKumesId = oluKumesHayvanTumu
                .GroupBy(x => x.KumesId.GetValueOrDefault(0))
                .ToDictionary(g => g.Key, g => g.ToList());

            var plcBagliByKumesId = plcBagliEtiketlerTumu
                .GroupBy(x => x.KumesId.GetValueOrDefault(0))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());

            var kumesLogByKumesId = kumesLoglariTumu
                .GroupBy(x => x.KumesId.GetValueOrDefault(0))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First());

            var alarmByKumesId = aktifAlarmlarTumu
                .GroupBy(x => x.KumesId.GetValueOrDefault(0))
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var kumes in kumesler)
            {
                var model = new TesisGenelIzlemeModel
                {
                    KumesId = kumes.Id,
                    Aktif = kumes.Aktif.GetValueOrDefault(true),
                    KumesName = kumes.Name,
                    // Bu üç alan ortak snapshot içinde kullanıcıya bağlı değildir.
                    SorumluMu = false,
                    YemRasyonYetki = false,
                    RaporRehberYetki = false
                };

                donemByKumesId.TryGetValue(kumes.Id, out var donem);

                if (donem != null)
                {
                    model.DonemId = donem.Id;
                    model.DonemName = donem.Name ?? string.Empty;

                    if (donem.GirisTarihi.HasValue)
                    {
                        var girisTarihi = donem.GirisTarihi.Value;
                        model.DonemGun = (now.Date - girisTarihi.Date).Days.ToString();

                        var ilkHaftaBaslangici = girisTarihi.Date;

                        if (ilkHaftaBaslangici.DayOfWeek != DayOfWeek.Monday)
                        {
                            var geriGidilecekGun = ((int)ilkHaftaBaslangici.DayOfWeek + 6) % 7;
                            ilkHaftaBaslangici = ilkHaftaBaslangici.AddDays(-geriGidilecekGun);
                        }
                        var toplamHaftaSayisi = ((now.Date - ilkHaftaBaslangici).Days / 7) + 1;
                        model.DonemHafta = toplamHaftaSayisi.ToString();
                    }
                }

                var oluKumesHayvan = donem != null && oluHayvanByKumesId.TryGetValue(kumes.Id, out var oluListesi)
                    ? oluListesi.Where(x => x.DonemId == donem.Id).ToList()
                    : new List<KumesHayvanLog>();

                plcBagliByKumesId.TryGetValue(kumes.Id, out var plcBagliEtiket);
                model.PlcBagli = plcBagliEtiket?.Deger == 1;

                kumesLogByKumesId.TryGetValue(kumes.Id, out var kumesLog);

                if (kumesLog != null)
                {
                    model.SicaklikKumes = kumesLog.SicaklikKumes.GetValueOrDefault(0).ToString("N1", culture);
                    model.NemKumes = kumesLog.NemKumes.GetValueOrDefault(0).ToString("N0", culture);
                    model.Co2Kumes = kumesLog.Co2_1.GetValueOrDefault(0).ToString("N0", culture);
                    model.BasincKumes = kumesLog.Basinc1.GetValueOrDefault(0).ToString("N0", culture);
                    model.YumurtaAdet = kumesLog.YumurtaAdet.HasValue ? kumesLog.YumurtaAdet.Value.ToString("N0", culture) : null; model.SuTuketimBugun = kumesLog.SuTuketimBugun.GetValueOrDefault(0).ToString("N0", culture);
                    model.YemTuketimBugun = kumesLog.YemTuketimBugun.GetValueOrDefault(0).ToString("N1", culture);
                }
                else
                {
                    model.SicaklikKumes = null;
                    model.NemKumes = null;
                    model.Co2Kumes = null;
                    model.BasincKumes = null;
                    model.YumurtaAdet = null;
                    model.SuTuketimBugun = null;
                    model.YemTuketimBugun = null;
                }

                var toplamOluHayvan = oluKumesHayvan.Sum(x => x.Adet.GetValueOrDefault(0));

                var mevcutHayvanAdet = donem != null ? donem.HayvanAdet.GetValueOrDefault(0) - toplamOluHayvan : 0;

                model.HayvanAdet = mevcutHayvanAdet.ToString("N0", culture);

                var bugununOluHayvanKaydi = oluKumesHayvan.FirstOrDefault(x => x.KayitZamani == now.Date);

                model.OluHayvanAdet = bugununOluHayvanKaydi?.Adet.GetValueOrDefault(0).ToString("N0", culture) ?? "0";

                var aktifAlarmListesi = alarmByKumesId.TryGetValue(kumes.Id, out var alarmListesi)
                    ? alarmListesi
                    : new List<PoultryHouseScada.Domain.Models.AlarmLog>();

                if (aktifAlarmListesi.Count > 0)
                {
                    model.AlarmVar = true;

                    if (model.PlcBagli.GetValueOrDefault(false))
                    {
                        model.AlarmMesajList = aktifAlarmListesi.Select(x => x.Mesaj ?? string.Empty).ToList();
                    }
                    else
                    {
                        model.AlarmMesajList = aktifAlarmListesi.Where(x => x.Mesaj?.Contains("plc", StringComparison.OrdinalIgnoreCase) == true).Select(x => x.Mesaj ?? string.Empty).ToList();
                    }
                }
                else
                {
                    model.AlarmVar = false;
                    model.AlarmMesajList = null;
                }

                result.Add(model);
            }
            return result;
        }

        public async Task<TesisGenelSnapshotModel> GetTesisGenelSnapshotByLokasyonId(int lokasyonId)
        {
            if (lokasyonId <= 0)
                throw new ArgumentException(
                    "Geçersiz lokasyon bilgisi.",
                    nameof(lokasyonId));

            var kumesler =
                await GetTesisGenelOrtakKumeslerByLokasyonId(
                    lokasyonId);

            // Buradaki metot adını mevcut servis metodunuza göre kullanın.
            var tesisOzet =
                await GetTesisGenelKartByLokasyonId(lokasyonId);

            return new TesisGenelSnapshotModel
            {
                LokasyonId = lokasyonId,
                Kumesler = kumesler,
                TesisOzet = tesisOzet,
            };
        }

        public async Task<TesisGenelIzlemeYetkiModel> GetTesisGenelIzlemeYetkiByLokasyonId(int lokasyonId, string userId)
        {
            if (lokasyonId <= 0)
                throw new ArgumentException("Geçersiz lokasyon bilgisi.", nameof(lokasyonId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Kullanıcı bilgisi bulunamadı.");

            var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true);
            var kumesListesi = kumesler.ToList();
            var kumesIds = kumesListesi.Select(x => x.Id).ToHashSet();
            var lokasyon = await _kumesRepository.GetLokasyonById(lokasyonId);
            if (lokasyon == null)
            {
                return new TesisGenelIzlemeYetkiModel
                {
                    LokasyonId = lokasyonId,
                    ErisimVar = false,
                    OlusturmaZamani = DateTime.Now
                };
            }

            var kullaniciYetkileri = await _kumesRepository.GetAllUserKumesYetkiByUserId(userId);
            var aktifYetkiler = kullaniciYetkileri.Where(x => x.Aktif.GetValueOrDefault(true) && x.Silindi != true).ToList();
            var sorumluKumesIds = new HashSet<int>();
            var yemRasyonKumesIds = new HashSet<int>();
            var raporRehberKumesIds = new HashSet<int>();

            var lokasyonKapsamindaYetkiVar = false;

            foreach (var yetki in aktifYetkiler)
            {
                IEnumerable<int> etkilenenKumesIds;

                /*
                 * 1. Doğrudan kümese verilmiş yetki
                 */
                if (yetki.KumesId > 0)
                {
                    if (!kumesIds.Contains(yetki.KumesId))
                        continue;

                    etkilenenKumesIds = new[] { yetki.KumesId };
                }
                /*
                 * 2. Lokasyon seviyesinde verilmiş yetki
                 */
                else if (yetki.LokasyonId > 0)
                {
                    if (yetki.LokasyonId != lokasyonId)
                        continue;

                    etkilenenKumesIds = kumesIds;
                }
                /*
                 * 3. Şirket seviyesinde verilmiş yetki
                 */
                else if (yetki.SirketId > 0)
                {
                    // Lokasyon modelinizde alan adı farklıysa
                    // lokasyon.SirketId kısmını değiştirin.
                    if (yetki.SirketId != lokasyon.SirketId)
                        continue;

                    etkilenenKumesIds = kumesIds;
                }
                else
                {
                    continue;
                }

                lokasyonKapsamindaYetkiVar = true;

                if (yetki.SorumluMu.GetValueOrDefault(false))
                {
                    sorumluKumesIds.UnionWith(etkilenenKumesIds);
                }

                if (yetki.YemRasyonYetki.GetValueOrDefault(false))
                {
                    yemRasyonKumesIds.UnionWith(etkilenenKumesIds);
                }

                if (yetki.RaporRehberYetki.GetValueOrDefault(false))
                {
                    raporRehberKumesIds.UnionWith(etkilenenKumesIds);
                }
            }

            var kumesYetkileri = kumesListesi
                .Select(
                    kumes => new TesisGenelIzlemeKumesYetkiModel
                    {
                        KumesId = kumes.Id,
                        SorumluMu = sorumluKumesIds.Contains(kumes.Id),
                        YemRasyonYetki = yemRasyonKumesIds.Contains(kumes.Id),
                        RaporRehberYetki = raporRehberKumesIds.Contains(kumes.Id)
                    })
                .ToList();

            return new TesisGenelIzlemeYetkiModel
            {
                LokasyonId = lokasyonId,
                /*
                 * Kullanıcının görüntüleme yetkisi farklı bir tabloda
                 * tutuluyorsa buradaki kontrol o servise bağlanmalıdır.
                 */
                ErisimVar = lokasyonKapsamindaYetkiVar,
                KumesYetkileri = kumesYetkileri,
                OlusturmaZamani = DateTime.Now
            };
        }

        public async Task<List<KumesRaporModel>> GetKumeslerOzetRaporByLokasyonId(int lokasyonId, string userId)
        {
            List<KumesRaporModel> result = new List<KumesRaporModel>();
            //var yetkiliKumesler = await GetKumesIdsByYetki(userId);

            var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => /*yetkiliKumesler.Contains(x.Id) &&*/ x.LokasyonId == lokasyonId && x.Silindi != true);
            foreach (var kumes in kumesler)
            {
                KumesRaporModel model = new KumesRaporModel();
                //todo 2 dk öncesine ayarlandı ama başka bir çözüm vardır belki
                var kumesLogs = (await _kumesLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-40) && x.Silindi != true));
                var kumesLog = kumesLogs.OrderByDescending(x => x.Id).FirstOrDefault();
                var plcBagliEtiket = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.EtiketId == KumesEtiketTipler.Plc_Bagli && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-40))).OrderByDescending(x => x.Id).FirstOrDefault();
                var silo1BagliEtiket = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.EtiketId == KumesEtiketTipler.Silo1_Bagli && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-40))).OrderByDescending(x => x.Id).FirstOrDefault();
                var silo2BagliEtiket = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.EtiketId == KumesEtiketTipler.Silo2_Bagli && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-40))).OrderByDescending(x => x.Id).FirstOrDefault();
                var modelNew = new KumesRaporModel();
                modelNew.KumesId = kumes.Id;
                modelNew.KumesName = kumes.Name;
                modelNew.Aktif = kumes.Aktif.GetValueOrDefault(true);
                modelNew.KumesServisApiUrl = kumes.ServisApiUrl;
                //modelNew.PlcBagliOkunuyor = await GetPlcConnectedAndReadingByKumesIdWithApi(kumes.Id, kumes.ServisApiUrl);
                if (kumesLog != null)
                {
                    //modelNew.DonemId = model.DonemId;
                    //modelNew.DonemName = model.DonemName;
                    //modelNew.DonemGun = model.DonemGun;
                    //modelNew.DonemHafta = model.DonemHafta;
                    modelNew.SicaklikKumes = kumesLog?.SicaklikKumes.GetValueOrDefault(0);
                    modelNew.SicaklikDis = kumesLog?.SicaklikDis.GetValueOrDefault(0);
                    modelNew.Sicaklik1 = kumesLog?.Sicaklik1.GetValueOrDefault(0);
                    modelNew.Sicaklik2 = kumesLog?.Sicaklik2.GetValueOrDefault(0);
                    modelNew.Sicaklik3 = kumesLog?.Sicaklik3.GetValueOrDefault(0);
                    modelNew.Sicaklik4 = kumesLog?.Sicaklik4.GetValueOrDefault(0);
                    modelNew.Sicaklik5 = kumesLog?.Sicaklik5.GetValueOrDefault(0);
                    modelNew.Sicaklik6 = kumesLog?.Sicaklik6.GetValueOrDefault(0);
                    //modelNew.Sicaklik7 = kumesLog?.Sicaklik7.GetValueOrDefault(0);
                    //modelNew.Sicaklik8 = kumesLog?.Sicaklik8.GetValueOrDefault(0);
                    modelNew.NemKumes = kumesLog?.NemKumes.GetValueOrDefault(0);
                    modelNew.NemDis = kumesLog?.NemDis.GetValueOrDefault(0);
                    modelNew.Nem1 = kumesLog?.Nem1.GetValueOrDefault(0);
                    modelNew.Nem2 = kumesLog?.Nem2.GetValueOrDefault(0);
                    modelNew.Nem3 = kumesLog?.Nem3.GetValueOrDefault(0);
                    modelNew.Nem4 = kumesLog?.Nem4.GetValueOrDefault(0);
                    modelNew.Nem5 = kumesLog?.Nem5.GetValueOrDefault(0);
                    modelNew.Nem6 = kumesLog?.Nem6.GetValueOrDefault(0);
                    //modelNew.Nem7 = kumesLog?.Nem7.GetValueOrDefault(0);
                    //modelNew.Nem8 = kumesLog?.Nem8.GetValueOrDefault(0);
                    modelNew.Silo1 = kumesLog?.Silo1.GetValueOrDefault(0);
                    modelNew.Silo2 = kumesLog?.Silo2.GetValueOrDefault(0);
                    modelNew.Silo1Bagli = silo1BagliEtiket == null ? false : silo1BagliEtiket.Deger == 1 ? true : false;
                    modelNew.Silo2Bagli = silo2BagliEtiket == null ? false : silo2BagliEtiket.Deger == 1 ? true : false;
                    modelNew.Co2Kumes = (kumesLog?.Co2_1.GetValueOrDefault(0));
                    modelNew.BasincKumes = kumesLog?.Basinc1.GetValueOrDefault(0);
                    //modelNew.HayvanAdet = nufusKumesHayvan.Sum(x => x.Adet.GetValueOrDefault(0));//hasta ve saglikli olanlarin toplami
                    modelNew.YumurtaAdet = kumesLog?.YumurtaAdet.GetValueOrDefault(0);
                    modelNew.SuTuketimBugun = kumesLog?.SuTuketimBugun.GetValueOrDefault(0);
                    modelNew.YemTuketimBugun = kumesLog?.YemTuketimBugun.GetValueOrDefault(0);
                    //modelNew.OlenSayisiBugun = kumesLog?.OlenSayisiBugun.GetValueOrDefault(0);
                    modelNew.KayitZamani = kumesLog?.KayitZamani.Value;
                    modelNew.PlcBagliOkunuyor = plcBagliEtiket == null ? false : plcBagliEtiket.Deger == 1 ? true : false;
                }
                result.Add(modelNew);
            }
            return result;
        }

        public async Task<List<KumesRaporModel>> GetKumeslerOzetRaporByLokasyonIdHub(int lokasyonId)
        {
            List<KumesRaporModel> result = new List<KumesRaporModel>();
            //var yetkiliKumesler = await GetKumesIdsByYetki(userId);

            var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => /*yetkiliKumesler.Contains(x.Id) &&*/ x.LokasyonId == lokasyonId && x.Silindi != true);
            foreach (var kumes in kumesler)
            {
                KumesRaporModel model = new KumesRaporModel();
                //todo 2 dk öncesine ayarlandı ama başka bir çözüm vardır belki
                var kumesLogs = (await _kumesLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-10) && x.Silindi != true));
                var kumesLog = kumesLogs.OrderByDescending(x => x.Id).FirstOrDefault();
                var plcBagliEtiket = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.EtiketId == KumesEtiketTipler.Plc_Bagli && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-10))).OrderByDescending(x => x.Id).FirstOrDefault();
                var silo1BagliEtiket = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.EtiketId == KumesEtiketTipler.Silo1_Bagli && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-10))).OrderByDescending(x => x.Id).FirstOrDefault();
                var silo2BagliEtiket = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.EtiketId == KumesEtiketTipler.Silo2_Bagli && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-10))).OrderByDescending(x => x.Id).FirstOrDefault();
                var modelNew = new KumesRaporModel();
                modelNew.KumesId = kumes.Id;
                modelNew.KumesName = kumes.Name;
                modelNew.Aktif = kumes.Aktif.GetValueOrDefault(true);
                modelNew.KumesServisApiUrl = kumes.ServisApiUrl;
                //modelNew.PlcBagliOkunuyor = await GetPlcConnectedAndReadingByKumesIdWithApi(kumes.Id, kumes.ServisApiUrl);
                if (kumesLog != null)
                {
                    //modelNew.DonemId = model.DonemId;
                    //modelNew.DonemName = model.DonemName;
                    //modelNew.DonemGun = model.DonemGun;
                    //modelNew.DonemHafta = model.DonemHafta;
                    modelNew.SicaklikKumes = kumesLog?.SicaklikKumes.GetValueOrDefault(0);
                    modelNew.SicaklikDis = kumesLog?.SicaklikDis.GetValueOrDefault(0);
                    modelNew.Sicaklik1 = kumesLog?.Sicaklik1.GetValueOrDefault(0);
                    modelNew.Sicaklik2 = kumesLog?.Sicaklik2.GetValueOrDefault(0);
                    modelNew.Sicaklik3 = kumesLog?.Sicaklik3.GetValueOrDefault(0);
                    modelNew.Sicaklik4 = kumesLog?.Sicaklik4.GetValueOrDefault(0);
                    modelNew.Sicaklik5 = kumesLog?.Sicaklik5.GetValueOrDefault(0);
                    modelNew.Sicaklik6 = kumesLog?.Sicaklik6.GetValueOrDefault(0);
                    //modelNew.Sicaklik7 = kumesLog?.Sicaklik7.GetValueOrDefault(0);
                    //modelNew.Sicaklik8 = kumesLog?.Sicaklik8.GetValueOrDefault(0);
                    modelNew.NemKumes = kumesLog?.NemKumes.GetValueOrDefault(0);
                    modelNew.NemDis = kumesLog?.NemDis.GetValueOrDefault(0);
                    modelNew.Nem1 = kumesLog?.Nem1.GetValueOrDefault(0);
                    modelNew.Nem2 = kumesLog?.Nem2.GetValueOrDefault(0);
                    modelNew.Nem3 = kumesLog?.Nem3.GetValueOrDefault(0);
                    modelNew.Nem4 = kumesLog?.Nem4.GetValueOrDefault(0);
                    modelNew.Nem5 = kumesLog?.Nem5.GetValueOrDefault(0);
                    modelNew.Nem6 = kumesLog?.Nem6.GetValueOrDefault(0);
                    //modelNew.Nem7 = kumesLog?.Nem7.GetValueOrDefault(0);
                    //modelNew.Nem8 = kumesLog?.Nem8.GetValueOrDefault(0);
                    modelNew.Silo1 = kumesLog?.Silo1.GetValueOrDefault(0);
                    modelNew.Silo2 = kumesLog?.Silo2.GetValueOrDefault(0);
                    modelNew.Silo1Bagli = silo1BagliEtiket == null ? false : silo1BagliEtiket.Deger == 1 ? true : false;
                    modelNew.Silo2Bagli = silo2BagliEtiket == null ? false : silo2BagliEtiket.Deger == 1 ? true : false;
                    modelNew.Co2Kumes = (kumesLog?.Co2_1.GetValueOrDefault(0));
                    modelNew.BasincKumes = kumesLog?.Basinc1.GetValueOrDefault(0);
                    //modelNew.HayvanAdet = nufusKumesHayvan.Sum(x => x.Adet.GetValueOrDefault(0));//hasta ve saglikli olanlarin toplami
                    //modelNew.YumurtaAdet = 0;
                    modelNew.SuTuketimBugun = kumesLog?.SuTuketimBugun.GetValueOrDefault(0);
                    modelNew.YemTuketimBugun = kumesLog?.YemTuketimBugun.GetValueOrDefault(0);
                    //modelNew.OlenSayisiBugun = kumesLog?.OlenSayisiBugun.GetValueOrDefault(0);
                    modelNew.KayitZamani = kumesLog?.KayitZamani.Value;
                    modelNew.PlcBagliOkunuyor = plcBagliEtiket == null ? false : plcBagliEtiket.Deger == 1 ? true : false;
                }
                result.Add(modelNew);
            }
            return result;
        }

        public async Task<List<KumesRaporModel>> GetKumesRaporByKumesIdEski(int kumesId, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval = 60)
        {
            List<KumesRaporModel> result = new List<KumesRaporModel>();

            var kumes = await _kumesRepository.GetAsyncByPredicate(x => x.Id == kumesId && x.Silindi != true);

            if (kumes == null)
                return result;
            DateTime yumurtaRaporBaslangicTarihi = new DateTime(2026, 5, 13, 12, 0, 0);
            var kumesLogs = (await _kumesLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.KayitZamani.Value >= baslangicZamani && x.KayitZamani.Value <= bitisZamani && x.Silindi != true)).OrderBy(x => x.KayitZamani).ToList();
            //var kumesHayvanLogs = (await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.KayitZamani.Value.Date >= baslangicZamani.Date && x.KayitZamani.Value.Date <= bitisZamani.Date && x.Silindi != true)).OrderBy(x => x.KayitZamani).ToList();
            var kumesDonems = await _donemRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.GirisTarihi.HasValue && (x.GirisTarihi.Value <= bitisZamani && (!x.CikisTarihi.HasValue || x.CikisTarihi.Value >= baslangicZamani))/*&& x.Silindi != true*/);
            var minDonemZamani = kumesDonems.MinBy(x => x.GirisTarihi) == null ? baslangicZamani.Date : kumesDonems.MinBy(x => x.GirisTarihi).GirisTarihi?.Date;
            var kumesHayvanLogs = (await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.KayitZamani.Value.Date >= minDonemZamani && x.KayitZamani.Value.Date <= bitisZamani.Date && x.Silindi != true)).OrderBy(x => x.KayitZamani).ToList();
            var grouped = kumesLogs.GroupBy(k =>
            {
                // Günlük
                if (dakikaInterval == 1440)
                {
                    return k.KayitZamani.Value.Date;
                }

                // Haftalık
                if (dakikaInterval == 10080) // 7 * 1440
                {
                    int diff = (7 + (k.KayitZamani.Value.DayOfWeek - DayOfWeek.Monday)) % 7;
                    return k.KayitZamani.Value.Date.AddDays(-diff);
                }

                // Aylık
                if (dakikaInterval >= 43200) // yaklaşık 30 gün
                {
                    return new DateTime(k.KayitZamani.Value.Year, k.KayitZamani.Value.Month, 1);
                }
                var totalMinutes = (int)(k.KayitZamani.Value - baslangicZamani).TotalMinutes;
                var rounded = (totalMinutes / dakikaInterval) * dakikaInterval;
                return baslangicZamani.AddMinutes(rounded);
            }).Select(g =>
            {
                var kayitTarihi = g.FirstOrDefault()?.KayitZamani.Value.Date ?? DateTime.MinValue;
                // 1. O tarihte aktif olan dönemi bul
                var donem = kumesDonems.FirstOrDefault(x => x.GirisTarihi.HasValue && x.GirisTarihi.Value.Date <= kayitTarihi && (!x.CikisTarihi.HasValue || x.CikisTarihi.Value.Date >= kayitTarihi) && x.Silindi != true);

                //int hayvanAdet = donem?.HayvanAdet.GetValueOrDefault(0) ?? 0;
                DateTime? donemGirisTarihi = donem?.GirisTarihi?.Date;

                // 2. O dönemin başlangıcından bu güne kadar ölen toplam hayvan sayısı
                int toplamOlenSayisi = 0;
                int toplamSaglikliSayisi = 0;

                if (donemGirisTarihi.HasValue)
                {
                    toplamSaglikliSayisi = kumesHayvanLogs.Where(x => x.KayitZamani.HasValue && x.KayitZamani.Value.Date >= donemGirisTarihi.Value && x.KayitZamani.Value.Date <= kayitTarihi && x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli && x.DonemId == donem.Id).Sum(x => x.Adet.GetValueOrDefault(0));
                    toplamOlenSayisi = kumesHayvanLogs.Where(x => x.KayitZamani.HasValue && x.KayitZamani.Value.Date >= donemGirisTarihi.Value && x.KayitZamani.Value.Date <= kayitTarihi && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu && x.DonemId == donem.Id).Sum(x => x.Adet.GetValueOrDefault(0));
                }

                int canliHayvanAdet = toplamSaglikliSayisi - toplamOlenSayisi;
                if (canliHayvanAdet < 0) canliHayvanAdet = 0; // Negatif olmasın

                // 3. Sadece bugünkü ölen hayvan sayısı (günlük)
                int bugunkuOlenSayisi = kumesHayvanLogs.Where(x => x.KayitZamani.HasValue && x.KayitZamani.Value.Date == kayitTarihi && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu).Sum(x => x.Adet.GetValueOrDefault(0));
                int? yumurtaAdet = (g.LastOrDefault()?.KayitZamani >= yumurtaRaporBaslangicTarihi ? g.LastOrDefault()?.YumurtaAdet.GetValueOrDefault(0) : 0);
                return new KumesRaporModel
                {
                    KayitZamani = g.FirstOrDefault()?.KayitZamani,
                    SicaklikKumes = g.FirstOrDefault()?.SicaklikKumes.GetValueOrDefault(0),
                    SicaklikDis = g.FirstOrDefault()?.SicaklikDis.GetValueOrDefault(0),
                    Sicaklik1 = g.FirstOrDefault()?.Sicaklik1.GetValueOrDefault(0),
                    Sicaklik2 = g.FirstOrDefault()?.Sicaklik2.GetValueOrDefault(0),
                    Sicaklik3 = g.FirstOrDefault()?.Sicaklik3.GetValueOrDefault(0),
                    Sicaklik4 = g.FirstOrDefault()?.Sicaklik4.GetValueOrDefault(0),
                    Sicaklik5 = g.FirstOrDefault()?.Sicaklik5.GetValueOrDefault(0),
                    Sicaklik6 = g.FirstOrDefault()?.Sicaklik6.GetValueOrDefault(0),
                    NemKumes = g.FirstOrDefault()?.NemKumes.GetValueOrDefault(0),
                    NemDis = g.FirstOrDefault()?.NemDis.GetValueOrDefault(0),
                    Nem1 = g.FirstOrDefault()?.Nem1.GetValueOrDefault(0),
                    Nem2 = g.FirstOrDefault()?.Nem2.GetValueOrDefault(0),
                    Nem3 = g.FirstOrDefault()?.Nem3.GetValueOrDefault(0),
                    Nem4 = g.FirstOrDefault()?.Nem4.GetValueOrDefault(0),
                    Nem5 = g.FirstOrDefault()?.Nem5.GetValueOrDefault(0),
                    Nem6 = g.FirstOrDefault()?.Nem6.GetValueOrDefault(0),
                    Co2Kumes = g.FirstOrDefault()?.Co2_1.GetValueOrDefault(0) / 10,
                    BasincKumes = g.FirstOrDefault()?.Basinc1.GetValueOrDefault(0),
                    SuTuketimBugun = g.LastOrDefault()?.SuTuketimBugun.GetValueOrDefault(0),
                    YemTuketimBugun = g.LastOrDefault()?.YemTuketimBugun.GetValueOrDefault(0),
                    YumurtaAdet = yumurtaAdet.GetValueOrDefault(0),
                    OlenSayisiBugun = bugunkuOlenSayisi,
                    CanliHayvanAdet = canliHayvanAdet,
                    HayvanBasinaSuTuketim = canliHayvanAdet > 0 ? g.LastOrDefault()?.SuTuketimBugun.GetValueOrDefault(0) * 1000000 / canliHayvanAdet : 0,
                    HayvanBasinaYemTuketim = canliHayvanAdet > 0 ? g.LastOrDefault()?.YemTuketimBugun.GetValueOrDefault(0) * 1000000 / canliHayvanAdet : 0,
                    HayvanBasinaYumurtaAdet = canliHayvanAdet > 0 ? yumurtaAdet.GetValueOrDefault(0) / canliHayvanAdet : 0
                };
            }).ToList();
            return grouped;
        }
        public async Task<List<KumesKarsilastirmaRaporModel>> GetKumesKarsilastirmaRaporu(List<int> kumesIds, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval = 60)
        {
            var result = new List<KumesKarsilastirmaRaporModel>();

            if (kumesIds == null || kumesIds.Count == 0)
                return result;

            var kumesler = (await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => kumesIds.Contains(x.Id) && x.Silindi != true)).OrderBy(x => x.Name).ToList();

            result = await GetKumesKarsilastirmaRaporuParalelAsync(kumesler, baslangicZamani, bitisZamani, dakikaInterval);

            return result;
        }

        /// <summary>
        /// Karşılaştırma raporunda birden fazla kümes seçildiğinde GetKumesRaporByKumesId'yi
        /// sırayla değil paralel çağırır (toplam süre en yavaş tek kümes sorgusuna yaklaşır,
        /// N kümesin toplamına değil). GetKumesRaporByKumesId'nin kendisi değiştirilmedi.
        /// Aynı DbContext instance'ı EF Core'da aynı anda birden fazla sorguda kullanılamadığından
        /// (aksi halde "a second operation was started on this context" hatası alınır), her kümes
        /// için ayrı bir DI scope'unda (dolayısıyla ayrı bir DbContext'te) çalışan ayrı bir
        /// IKumesAppService örneği üzerinden çağrılıyor.
        /// </summary>
        private async Task<List<KumesKarsilastirmaRaporModel>> GetKumesKarsilastirmaRaporuParalelAsync(
            List<Kumes> kumesler, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval)
        {
            var raporGorevleri = kumesler.Select(async kumes =>
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedKumesAppService = scope.ServiceProvider.GetRequiredService<IKumesAppService>();

                var rapor = await scopedKumesAppService.GetKumesRaporByKumesId(kumes.Id, baslangicZamani, bitisZamani, dakikaInterval);

                return new KumesKarsilastirmaRaporModel
                {
                    KumesId = kumes.Id,
                    KumesName = kumes.Name,
                    // Entity içerisindeki gerçek alan adına göre düzenle:
                    KumesKodu = kumes.KumesNo,
                    // Kayıt bulunmazsa boş liste döner.
                    Rapor = rapor ?? new List<KumesRaporModel>()
                };
            });

            var raporlar = await Task.WhenAll(raporGorevleri);

            return raporlar.ToList();
        }

        public async Task<List<KumesKarsilastirmaRaporModel>> GetKumesKarsilastirmaYumurtaRaporu(List<int> kumesIds, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval = 60)
        {
            var result = new List<KumesKarsilastirmaRaporModel>();

            if (kumesIds == null || kumesIds.Count == 0)
                return result;

            var kumesler = (await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => kumesIds.Contains(x.Id) && x.Silindi != true)).OrderBy(x => x.Name).ToList();

            result = await GetKumesKarsilastirmaYumurtaRaporuParalelAsync(kumesler, baslangicZamani, bitisZamani, dakikaInterval);

            return result;
        }

        /// <summary>
        /// kumes-topbar > Rahmiye Çiftlik > "Yumurta Raporu" popup'ının kullandığı
        /// karşılaştırma raporu. GetKumesKarsilastirmaRaporuParalelAsync ile aynı paralel
        /// çağrı deseni, sadece her kümes için GetKumesRaporByKumesId yerine
        /// GetKumesYumurtaRaporByKumesId çağrılır.
        /// </summary>
        private async Task<List<KumesKarsilastirmaRaporModel>> GetKumesKarsilastirmaYumurtaRaporuParalelAsync(
            List<Kumes> kumesler, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval)
        {
            var raporGorevleri = kumesler.Select(async kumes =>
            {
                using var scope = _serviceProvider.CreateScope();
                var scopedKumesAppService = scope.ServiceProvider.GetRequiredService<IKumesAppService>();

                var rapor = await scopedKumesAppService.GetKumesYumurtaRaporByKumesId(kumes.Id, baslangicZamani, bitisZamani, dakikaInterval);

                return new KumesKarsilastirmaRaporModel
                {
                    KumesId = kumes.Id,
                    KumesName = kumes.Name,
                    KumesKodu = kumes.KumesNo,
                    Rapor = rapor ?? new List<KumesRaporModel>()
                };
            });

            var raporlar = await Task.WhenAll(raporGorevleri);

            return raporlar.ToList();
        }

        public async Task<List<KumesRaporModel>> GetKumesRaporByKumesId(int kumesId, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval = 60)
        {
            List<KumesRaporModel> result = new List<KumesRaporModel>();

            var kumes = await _kumesRepository.GetAsyncByPredicate(x => x.Id == kumesId && x.Silindi != true);

            if (kumes == null)
                return result;          

            static decimal? SonDoluGunlukDecimalDeger(IEnumerable<KumesLogYeni> logs, DateTime referansZamani, Func<KumesLogYeni, decimal?> selector)
            {
                return logs
                    .Where(x =>
                        x.KayitZamani.HasValue &&
                        x.KayitZamani.Value.Date == referansZamani.Date &&
                        x.KayitZamani.Value <= referansZamani &&
                        selector(x).HasValue)
                    .OrderByDescending(x => x.KayitZamani)
                    .Select(selector)
                    .FirstOrDefault();
            }
            static Dictionary<DateTime, decimal?> GunlukMaksimumDecimalDegerler(IEnumerable<KumesLogYeni> logs, Func<KumesLogYeni, decimal?> selector)
            {
                return logs
                    .Where(x => x.KayitZamani.HasValue)
                    .GroupBy(x => x.KayitZamani!.Value.Date)
                    .Select(g => new
                    {
                        Tarih = g.Key,
                        Degerler = g
                            .Select(selector)
                            .Where(x => x.HasValue)
                            .Select(x => x!.Value)
                            .ToList()
                    })
                    .Where(x => x.Degerler.Any())
                    .ToDictionary(
                        x => x.Tarih,
                        x => (decimal?)x.Degerler.Max());
            }
            static bool RaporlanabilirLogMu(KumesLogYeni x)
            {
                return
                    x.SicaklikKumes.HasValue ||
                    x.SicaklikDis.HasValue ||
                    x.Sicaklik1.HasValue ||
                    x.Sicaklik2.HasValue ||
                    x.Sicaklik3.HasValue ||
                    x.Sicaklik4.HasValue ||
                    x.Sicaklik5.HasValue ||
                    x.Sicaklik6.HasValue ||

                    x.NemKumes.HasValue ||
                    x.NemDis.HasValue ||
                    x.Nem1.HasValue ||
                    x.Nem2.HasValue ||
                    x.Nem3.HasValue ||
                    x.Nem4.HasValue ||
                    x.Nem5.HasValue ||
                    x.Nem6.HasValue ||

                    x.Co2_1.HasValue ||
                    x.Basinc1.HasValue ||
                    x.SuTuketimBugun.HasValue ||
                    x.PedSuTuketimBugun.HasValue ||
                    x.YemTuketimBugun.HasValue ||
                    x.Silo1.HasValue ||
                    x.Silo2.HasValue;
            }

            static decimal? OrtalamaDecimal(IEnumerable<KumesLogYeni> logs, Func<KumesLogYeni, decimal?> selector)
            {
                var values = logs.Select(selector).Where(x => x.HasValue).Select(x => x!.Value).ToList();

                return values.Any() ? Math.Round(values.Average(), 2) : null;
            }

            static int? OrtalamaInt(IEnumerable<KumesLogYeni> logs, Func<KumesLogYeni, int?> selector)
            {
                var values = logs.Select(selector).Where(x => x.HasValue).Select(x => x!.Value).ToList();

                return values.Any() ? Convert.ToInt32(Math.Round(values.Average(), 0, MidpointRounding.AwayFromZero)) : null;
            }

            static decimal? OrtalamaDecimalListe(IEnumerable<decimal?> values, int basamak = 2)
            {
                var list = values.Where(x => x.HasValue).Select(x => x!.Value).ToList();
                return list.Any() ? Math.Round(list.Average(), basamak) : null;
            }

            static int? OrtalamaIntListe(IEnumerable<int?> values)
            {
                var list = values.Where(x => x.HasValue).Select(x => x.Value).ToList();
                return list.Any() ? Convert.ToInt32(Math.Round(list.Average(), 0, MidpointRounding.AwayFromZero)) : null;
            }

            var kumesLogs = (await _kumesLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.KayitZamani.HasValue && x.KayitZamani.Value >= baslangicZamani && x.KayitZamani.Value <= bitisZamani && x.Silindi != true))
                .Where(RaporlanabilirLogMu).OrderBy(x => x.KayitZamani).ToList();

            if (!kumesLogs.Any())
                return result;

            var kumesDonems = (await _donemRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.GirisTarihi.HasValue && x.GirisTarihi.Value <= bitisZamani && (!x.CikisTarihi.HasValue || x.CikisTarihi.Value >= baslangicZamani) && x.Silindi != true)).OrderBy(x => x.GirisTarihi).ToList();

            var minDonemZamani = kumesDonems.MinBy(x => x.GirisTarihi)?.GirisTarihi?.Date ?? baslangicZamani.Date;

            var kumesHayvanLogs =
                (await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x =>
                    x.KumesId == kumes.Id &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value.Date >= minDonemZamani &&
                    x.KayitZamani.Value.Date <= bitisZamani.Date &&
                    x.Silindi != true))
                .OrderBy(x => x.KayitZamani)
                .ToList();

            bool gunlukRapor = dakikaInterval == 1440;
            bool haftalikRapor = dakikaInterval == 10080;
            bool aylikRapor = dakikaInterval >= 43200;

            bool gunBazliOzetRapor = gunlukRapor || haftalikRapor || aylikRapor;

            var catiKontroller = haftalikRapor ? (await _kumesHaftalikCatiKontrolRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.Silindi != true)).ToList() : new List<KumesHaftalikCatiKontrol>();

            var grouped = kumesLogs.GroupBy<KumesLogYeni, System.DateTime>(k =>
                {
                    var kayitZamani = k.KayitZamani!.Value;

                    // Günlük
                    if (dakikaInterval == 1440)
                    {
                        return kayitZamani.Date;
                    }

                    // Haftalık - Pazartesi başlangıçlı
                    if (dakikaInterval == 10080)
                    {
                        int diff = (7 + (kayitZamani.DayOfWeek - DayOfWeek.Monday)) % 7;
                        return kayitZamani.Date.AddDays(-diff);
                    }

                    // Aylık
                    if (dakikaInterval >= 43200)
                    {
                        return new DateTime(kayitZamani.Year, kayitZamani.Month, 1);
                    }

                    // Saatlik / dakikalık interval:
                    // Kullanıcının seçtiği başlangıç zamanından itibaren gruplar.
                    var totalMinutes = (int)(kayitZamani - baslangicZamani).TotalMinutes;
                    var rounded = (totalMinutes / dakikaInterval) * dakikaInterval;

                    return baslangicZamani.AddMinutes(rounded);
                })
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var firstLog = g.OrderBy(x => x.KayitZamani).FirstOrDefault();
                    var lastLog = g.OrderBy(x => x.KayitZamani).LastOrDefault();

                    if (firstLog == null || lastLog == null || !lastLog.KayitZamani.HasValue)
                        return null;

                    var grupTarihi = g.Key;
                    var hesapTarihi = lastLog.KayitZamani.Value.Date;
                    var gunlukMaksimumSuTuketimMap = gunBazliOzetRapor ? GunlukMaksimumDecimalDegerler(g, x => x.SuTuketimBugun) : new Dictionary<DateTime, decimal?>();

                    var gunlukMaksimumPedSuTuketimMap = gunBazliOzetRapor ? GunlukMaksimumDecimalDegerler(g, x => x.PedSuTuketimBugun) : new Dictionary<DateTime, decimal?>();

                    var gunlukMaksimumYemTuketimMap = gunBazliOzetRapor ? GunlukMaksimumDecimalDegerler(g, x => x.YemTuketimBugun) : new Dictionary<DateTime, decimal?>();

                    var haftaHesapTarihi = haftalikRapor ? grupTarihi : hesapTarihi;

                    var gunlukSonKayitlar = haftalikRapor ? g.Where(x => x.KayitZamani.HasValue).GroupBy(x => x.KayitZamani!.Value.Date).Select(x => x.OrderByDescending(y => y.KayitZamani).First()).OrderBy(x => x.KayitZamani).ToList() : new List<KumesLogYeni>();

                    var donem = kumesDonems.FirstOrDefault(x => x.GirisTarihi.HasValue && x.GirisTarihi.Value.Date <= hesapTarihi && (!x.CikisTarihi.HasValue || x.CikisTarihi.Value.Date >= hesapTarihi) && x.Silindi != true);

                    DateTime? donemGirisTarihi = donem?.GirisTarihi?.Date;

                    int toplamOlenSayisi = 0;
                    int toplamSaglikliSayisi = 0;

                    if (donem != null && donemGirisTarihi.HasValue)
                    {
                        toplamSaglikliSayisi = kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date >= donemGirisTarihi.Value &&
                                x.KayitZamani.Value.Date <= hesapTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0));

                        toplamOlenSayisi = kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date >= donemGirisTarihi.Value &&
                                x.KayitZamani.Value.Date <= hesapTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0));
                    }

                    int canliHayvanAdet = toplamSaglikliSayisi - toplamOlenSayisi;

                    if (canliHayvanAdet < 0)
                        canliHayvanAdet = 0;

                    decimal? haftalikOlum = 0;

                    if (haftalikRapor && donem != null && donemGirisTarihi.HasValue)
                    {
                        var oncekiHaftaSonuTarihi = grupTarihi.AddDays(-1);

                        decimal buHaftaCanliOrani = toplamSaglikliSayisi > 0 ? ((decimal)canliHayvanAdet / toplamSaglikliSayisi) * 100 : 100;

                        int oncekiHaftaToplamSaglikliSayisi = kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date >= donemGirisTarihi.Value &&
                                x.KayitZamani.Value.Date <= oncekiHaftaSonuTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0));

                        int oncekiHaftaToplamOlenSayisi = kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date >= donemGirisTarihi.Value &&
                                x.KayitZamani.Value.Date <= oncekiHaftaSonuTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0));

                        int oncekiHaftaCanliHayvanAdet = oncekiHaftaToplamSaglikliSayisi - oncekiHaftaToplamOlenSayisi;

                        if (oncekiHaftaCanliHayvanAdet < 0)
                            oncekiHaftaCanliHayvanAdet = 0;

                        decimal oncekiHaftaCanliOrani = oncekiHaftaToplamSaglikliSayisi > 0 ? ((decimal)oncekiHaftaCanliHayvanAdet / oncekiHaftaToplamSaglikliSayisi) * 100 : 100;

                        haftalikOlum = Math.Round((oncekiHaftaCanliOrani - buHaftaCanliOrani) * 100, 2);

                        if (haftalikOlum < 0)
                            haftalikOlum = 0;
                    }

                    int bugunkuOlenSayisi = donem != null
                        ? kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date == hesapTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0))
                        : 0;

                    //decimal suTuketimBugun = lastLog.SuTuketimBugun.GetValueOrDefault(0);
                    //decimal yemTuketimBugun = lastLog.YemTuketimBugun.GetValueOrDefault(0);
                    var sonKayitZamani = lastLog.KayitZamani.Value;

                    decimal? suTuketimBugun;

                    decimal? pedSuTuketimBugun;

                    decimal? yemTuketimBugun;

                    if (gunBazliOzetRapor)
                    {
                        // Günlük raporda listede tek gün bulunacağından doğrudan
                        // o günün maksimumu döner.
                        //
                        // Haftalık ve aylık raporlarda ise her günün maksimum
                        // tüketim değerinin ortalaması alınır.
                        suTuketimBugun = OrtalamaDecimalListe(gunlukMaksimumSuTuketimMap.Values);
                        pedSuTuketimBugun = OrtalamaDecimalListe(gunlukMaksimumPedSuTuketimMap.Values);
                        yemTuketimBugun = OrtalamaDecimalListe(gunlukMaksimumYemTuketimMap.Values);
                    }
                    else
                    {
                        // Saatlik ve dakikalık raporların mevcut davranışı korunur.
                        suTuketimBugun = SonDoluGunlukDecimalDeger(kumesLogs, sonKayitZamani, x => x.SuTuketimBugun);
                        pedSuTuketimBugun = SonDoluGunlukDecimalDeger(kumesLogs, sonKayitZamani, x => x.PedSuTuketimBugun);
                        yemTuketimBugun = SonDoluGunlukDecimalDeger(kumesLogs, sonKayitZamani, x => x.YemTuketimBugun);
                    }

                    int? haftaSayisi = HesaplaHaftaSayisi(donem, haftaHesapTarihi);

                    var catiKontrol = haftalikRapor && haftaSayisi.HasValue && donem != null
                        ? catiKontroller.FirstOrDefault(x =>
                            x.DonemId == donem.Id &&
                            x.HaftaSayisi == haftaSayisi.Value &&
                            x.Silindi != true)
                        : null;

                
                    var gunlukHayvanBasinaSuTuketimler = new List<decimal?>();
                    var gunlukHayvanBasinaYemTuketimler = new List<decimal?>();
                    var gunlukCanliHayvanAdetleri = new List<int>();
                    var gunlukOlenSayilari = new List<int>();

                    if (haftalikRapor)
                    {
                        foreach (var gunlukLog in gunlukSonKayitlar)
                        {
                            if (!gunlukLog.KayitZamani.HasValue)
                                continue;

                            var gunTarihi = gunlukLog.KayitZamani.Value.Date;

                            decimal? gunSuTuketim = gunlukMaksimumSuTuketimMap.TryGetValue(gunTarihi, out var maksimumSuTuketim) ? maksimumSuTuketim : 0;

                            decimal? gunYemTuketim = gunlukMaksimumYemTuketimMap.TryGetValue(gunTarihi, out var maksimumYemTuketim) ? maksimumYemTuketim : 0;

                            var gunDonem = kumesDonems.FirstOrDefault(x =>
                                x.GirisTarihi.HasValue &&
                                x.GirisTarihi.Value.Date <= gunTarihi &&
                                (!x.CikisTarihi.HasValue || x.CikisTarihi.Value.Date >= gunTarihi) &&
                                x.Silindi != true);

                            if (gunDonem == null || !gunDonem.GirisTarihi.HasValue)
                                continue;

                            var gunDonemGirisTarihi = gunDonem.GirisTarihi.Value.Date;

                            int gunToplamSaglikliSayisi = kumesHayvanLogs
                                .Where(x =>
                                    x.KayitZamani.HasValue &&
                                    x.KayitZamani.Value.Date >= gunDonemGirisTarihi &&
                                    x.KayitZamani.Value.Date <= gunTarihi &&
                                    x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli &&
                                    x.DonemId == gunDonem.Id)
                                .Sum(x => x.Adet.GetValueOrDefault(0));

                            int gunToplamOlenSayisi = kumesHayvanLogs
                                .Where(x =>
                                    x.KayitZamani.HasValue &&
                                    x.KayitZamani.Value.Date >= gunDonemGirisTarihi &&
                                    x.KayitZamani.Value.Date <= gunTarihi &&
                                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                    x.DonemId == gunDonem.Id)
                                .Sum(x => x.Adet.GetValueOrDefault(0));

                            int gunCanliHayvanAdet = gunToplamSaglikliSayisi - gunToplamOlenSayisi;

                            if (gunCanliHayvanAdet < 0)
                                gunCanliHayvanAdet = 0;

                            gunlukCanliHayvanAdetleri.Add(gunCanliHayvanAdet);

                            int gunOlenSayisi = kumesHayvanLogs
                                .Where(x =>
                                    x.KayitZamani.HasValue &&
                                    x.KayitZamani.Value.Date == gunTarihi &&
                                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                    x.DonemId == gunDonem.Id)
                                .Sum(x => x.Adet.GetValueOrDefault(0));

                            gunlukOlenSayilari.Add(gunOlenSayisi);

                            if (gunCanliHayvanAdet > 0 )
                            {                             
                                decimal? gunHayvanBasinaSu = gunSuTuketim.HasValue ? (gunSuTuketim.Value * 1000000m) / gunCanliHayvanAdet : null;
                                decimal? gunHayvanBasinaYem = gunYemTuketim.HasValue ? (gunYemTuketim.Value * 1000000m) / gunCanliHayvanAdet : null;

                                gunlukHayvanBasinaSuTuketimler.Add(gunHayvanBasinaSu);
                                gunlukHayvanBasinaYemTuketimler.Add(gunHayvanBasinaYem);
                            }
                        }
                    }
                    else
                    {
                     
                    }

                    decimal? raporSicaklikKumes = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.SicaklikKumes) : firstLog.SicaklikKumes;

                    decimal? raporSicaklikDis = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.SicaklikDis) : firstLog.SicaklikDis;

                    decimal? raporSicaklik1 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik1) : firstLog.Sicaklik1;

                    decimal? raporSicaklik2 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik2) : firstLog.Sicaklik2;

                    decimal? raporSicaklik3 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik3) : firstLog.Sicaklik3;

                    decimal? raporSicaklik4 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik4) : firstLog.Sicaklik4;

                    decimal? raporSicaklik5 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik5) : firstLog.Sicaklik5;

                    decimal? raporSicaklik6 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik6) : firstLog.Sicaklik6;

                    decimal? raporNemKumes = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.NemKumes) : firstLog.NemKumes;

                    decimal? raporNemDis = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.NemDis) : firstLog.NemDis;

                    decimal? raporNem1 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem1) : firstLog.Nem1;

                    decimal? raporNem2 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem2) : firstLog.Nem2;

                    decimal? raporNem3 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem3) : firstLog.Nem3;

                    decimal? raporNem4 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem4) : firstLog.Nem4;

                    decimal? raporNem5 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem5) : firstLog.Nem5;

                    decimal? raporNem6 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem6) : firstLog.Nem6;

                    decimal? raporCo2Kumes;

                    if (haftalikRapor)
                    {
                        decimal? ortalamaCo2 = OrtalamaDecimal(gunlukSonKayitlar, x => x.Co2_1);
                        raporCo2Kumes = ortalamaCo2.HasValue ? Math.Round(ortalamaCo2.Value / 10m, 2) : null;
                    }
                    else
                    {
                        raporCo2Kumes = firstLog.Co2_1.HasValue ? Math.Round(firstLog.Co2_1.Value / 10m, 2) : null;
                    }
                    decimal? raporBasincKumes = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Basinc1) : firstLog.Basinc1;

                    //decimal raporSuTuketimBugun = haftalikRapor
                    //    ? OrtalamaDecimal(gunlukSonKayitlar, x => x.SuTuketimBugun)
                    //    : suTuketimBugun;

                    //decimal raporYemTuketimBugun = haftalikRapor
                    //    ? OrtalamaDecimal(gunlukSonKayitlar, x => x.YemTuketimBugun)
                    //    : yemTuketimBugun;

                    decimal? raporSuTuketimBugun = suTuketimBugun;

                    decimal? raporPedSuTuketimBugun = pedSuTuketimBugun;

                    decimal? raporYemTuketimBugun = yemTuketimBugun;

                    decimal? raporSilo1 = lastLog.Silo1;

                    decimal? raporSilo2 = lastLog.Silo2;

                    int raporOlenSayisiBugun = donem != null
                                              ? kumesHayvanLogs
                                                  .Where(x =>
                                                      x.KayitZamani.HasValue &&
                                                      x.KayitZamani.Value.Date >= grupTarihi.Date &&
                                                      x.KayitZamani.Value.Date <= grupTarihi.Date.AddDays(6) &&
                                                      x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                                      x.DonemId == donem.Id)
                                                  .Sum(x => x.Adet.GetValueOrDefault(0))
                                              : 0;

                    //int raporCanliHayvanAdet = haftalikRapor
                    //    ? OrtalamaIntListe(gunlukCanliHayvanAdetleri)
                    //    : canliHayvanAdet;

                    decimal? raporHayvanBasinaSuTuketim = haftalikRapor ? OrtalamaDecimalListe(gunlukHayvanBasinaSuTuketimler) : canliHayvanAdet > 0 && suTuketimBugun.HasValue ? Math.Round((suTuketimBugun.Value * 1000000m) / canliHayvanAdet, 2) : null;

                    decimal? raporHayvanBasinaYemTuketim = haftalikRapor ? OrtalamaDecimalListe(gunlukHayvanBasinaYemTuketimler) : canliHayvanAdet > 0 && yemTuketimBugun.HasValue ? Math.Round((yemTuketimBugun.Value * 1000000m) / canliHayvanAdet, 2) : null;

                    return new KumesRaporModel
                    {
                        KayitZamani = grupTarihi,

                        SicaklikKumes = raporSicaklikKumes,
                        SicaklikDis = raporSicaklikDis,
                        Sicaklik1 = raporSicaklik1,
                        Sicaklik2 = raporSicaklik2,
                        Sicaklik3 = raporSicaklik3,
                        Sicaklik4 = raporSicaklik4,
                        Sicaklik5 = raporSicaklik5,
                        Sicaklik6 = raporSicaklik6,

                        NemKumes = raporNemKumes,
                        NemDis = raporNemDis,
                        Nem1 = raporNem1,
                        Nem2 = raporNem2,
                        Nem3 = raporNem3,
                        Nem4 = raporNem4,
                        Nem5 = raporNem5,
                        Nem6 = raporNem6,

                        Co2Kumes = raporCo2Kumes,
                        BasincKumes = raporBasincKumes,

                        SuTuketimBugun = raporSuTuketimBugun,
                        PedSuTuketimBugun = raporPedSuTuketimBugun,
                        IcmeSuyuSayac = lastLog.IcmeSuyuSayac,
                        PedSuSayac = lastLog.PedSuSayac,
                        YemTuketimBugun = raporYemTuketimBugun,
                        Silo1 = raporSilo1,
                        Silo2 = raporSilo2,

                        OlenSayisiBugun = raporOlenSayisiBugun,
                        CanliHayvanAdet = canliHayvanAdet,

                        HayvanBasinaSuTuketim = raporHayvanBasinaSuTuketim,
                        HayvanBasinaYemTuketim = raporHayvanBasinaYemTuketim,

                        HaftaSayisi = haftaSayisi,
                        HaftalikOlum = haftalikOlum,
                        CatiKontrolDeger = catiKontrol?.Deger
                    };
                })
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();

            return grouped;
        }
        public async Task<List<KumesRaporModel>> GetKumesRaporByKumesId2(int kumesId, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval = 60)
        {
            List<KumesRaporModel> result = new List<KumesRaporModel>();

            var kumes = await _kumesRepository.GetAsyncByPredicate(x => x.Id == kumesId && x.Silindi != true);

            if (kumes == null)
                return result;

            var randimanRehberleri =
                (await _kumesRaporRehberRepository.GetAllAsyncByPredicateWithAsNoTracking(x =>
                    x.LokasyonId == kumes.LokasyonId &&
                    x.RehberAdi == KumesRaporRehberTipleri.Randiman &&
                    x.Silindi != true))
                .GroupBy(x => x.HaftaSayisi)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.GuncellemeZamani ?? y.KayitZamani)
                          .First()
                          .Deger);

            DateTime yumurtaRaporBaslangicTarihi = new DateTime(2026, 5, 13, 12, 0, 0);
            static decimal? SonDoluGunlukDecimalDeger(IEnumerable<KumesLogYeni> logs, DateTime referansZamani, Func<KumesLogYeni, decimal?> selector)
            {
                return logs
                    .Where(x =>
                        x.KayitZamani.HasValue &&
                        x.KayitZamani.Value.Date == referansZamani.Date &&
                        x.KayitZamani.Value <= referansZamani &&
                        selector(x).HasValue)
                    .OrderByDescending(x => x.KayitZamani)
                    .Select(selector)
                    .FirstOrDefault();
            }
            static Dictionary<DateTime, decimal?> GunlukMaksimumDecimalDegerler(IEnumerable<KumesLogYeni> logs, Func<KumesLogYeni, decimal?> selector)
            {
                return logs
                    .Where(x => x.KayitZamani.HasValue)
                    .GroupBy(x => x.KayitZamani!.Value.Date)
                    .Select(g => new
                    {
                        Tarih = g.Key,
                        Degerler = g
                            .Select(selector)
                            .Where(x => x.HasValue)
                            .Select(x => x!.Value)
                            .ToList()
                    })
                    .Where(x => x.Degerler.Any())
                    .ToDictionary(
                        x => x.Tarih,
                        x => (decimal?)x.Degerler.Max());
            }
            static bool RaporlanabilirLogMu(KumesLogYeni x)
            {
                return
                    x.SicaklikKumes.HasValue ||
                    x.SicaklikDis.HasValue ||
                    x.Sicaklik1.HasValue ||
                    x.Sicaklik2.HasValue ||
                    x.Sicaklik3.HasValue ||
                    x.Sicaklik4.HasValue ||
                    x.Sicaklik5.HasValue ||
                    x.Sicaklik6.HasValue ||

                    x.NemKumes.HasValue ||
                    x.NemDis.HasValue ||
                    x.Nem1.HasValue ||
                    x.Nem2.HasValue ||
                    x.Nem3.HasValue ||
                    x.Nem4.HasValue ||
                    x.Nem5.HasValue ||
                    x.Nem6.HasValue ||

                    x.Co2_1.HasValue ||
                    x.Basinc1.HasValue ||
                    x.YumurtaAdet.HasValue ||
                    x.SuTuketimBugun.HasValue ||
                    x.PedSuTuketimBugun.HasValue ||
                    x.YemTuketimBugun.HasValue ||
                    x.Silo1.HasValue ||
                    x.Silo2.HasValue;
            }

            static decimal? OrtalamaDecimal(IEnumerable<KumesLogYeni> logs, Func<KumesLogYeni, decimal?> selector)
            {
                var values = logs.Select(selector).Where(x => x.HasValue).Select(x => x!.Value).ToList();

                return values.Any() ? Math.Round(values.Average(), 2) : null;
            }

            static int? OrtalamaInt(IEnumerable<KumesLogYeni> logs, Func<KumesLogYeni, int?> selector)
            {
                var values = logs.Select(selector).Where(x => x.HasValue).Select(x => x!.Value).ToList();

                return values.Any() ? Convert.ToInt32(Math.Round(values.Average(), 0, MidpointRounding.AwayFromZero)) : null;
            }

            static decimal? OrtalamaDecimalListe(IEnumerable<decimal?> values, int basamak = 2)
            {
                var list = values.Where(x => x.HasValue).Select(x => x!.Value).ToList();
                return list.Any() ? Math.Round(list.Average(), basamak) : null;
            }

            static int? OrtalamaIntListe(IEnumerable<int?> values)
            {
                var list = values.Where(x => x.HasValue).Select(x => x.Value).ToList();
                return list.Any() ? Convert.ToInt32(Math.Round(list.Average(), 0, MidpointRounding.AwayFromZero)) : null;
            }

            var kumesLogs = (await _kumesLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.KayitZamani.HasValue && x.KayitZamani.Value >= baslangicZamani && x.KayitZamani.Value <= bitisZamani && x.Silindi != true))
                .Where(RaporlanabilirLogMu).OrderBy(x => x.KayitZamani).ToList();

            if (!kumesLogs.Any())
                return result;

            var kumesDonems = (await _donemRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.GirisTarihi.HasValue && x.GirisTarihi.Value <= bitisZamani && (!x.CikisTarihi.HasValue || x.CikisTarihi.Value >= baslangicZamani) && x.Silindi != true)).OrderBy(x => x.GirisTarihi).ToList();

            var minDonemZamani = kumesDonems.MinBy(x => x.GirisTarihi)?.GirisTarihi?.Date ?? baslangicZamani.Date;

            var kumesHayvanLogs =
                (await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x =>
                    x.KumesId == kumes.Id &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value.Date >= minDonemZamani &&
                    x.KayitZamani.Value.Date <= bitisZamani.Date &&
                    x.Silindi != true))
                .OrderBy(x => x.KayitZamani)
                .ToList();

            bool gunlukRapor = dakikaInterval == 1440;
            bool haftalikRapor = dakikaInterval == 10080;
            bool aylikRapor = dakikaInterval >= 43200;

            bool gunBazliOzetRapor = gunlukRapor || haftalikRapor || aylikRapor;

            var catiKontroller = haftalikRapor ? (await _kumesHaftalikCatiKontrolRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.Silindi != true)).ToList() : new List<KumesHaftalikCatiKontrol>();

            var grouped = kumesLogs.GroupBy<KumesLogYeni, System.DateTime>(k =>
            {
                var kayitZamani = k.KayitZamani!.Value;

                // Günlük
                if (dakikaInterval == 1440)
                {
                    return kayitZamani.Date;
                }

                // Haftalık - Pazartesi başlangıçlı
                if (dakikaInterval == 10080)
                {
                    int diff = (7 + (kayitZamani.DayOfWeek - DayOfWeek.Monday)) % 7;
                    return kayitZamani.Date.AddDays(-diff);
                }

                // Aylık
                if (dakikaInterval >= 43200)
                {
                    return new DateTime(kayitZamani.Year, kayitZamani.Month, 1);
                }

                // Saatlik / dakikalık interval:
                // Kullanıcının seçtiği başlangıç zamanından itibaren gruplar.
                var totalMinutes = (int)(kayitZamani - baslangicZamani).TotalMinutes;
                var rounded = (totalMinutes / dakikaInterval) * dakikaInterval;

                return baslangicZamani.AddMinutes(rounded);
            })
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var firstLog = g.OrderBy(x => x.KayitZamani).FirstOrDefault();
                    var lastLog = g.OrderBy(x => x.KayitZamani).LastOrDefault();

                    if (firstLog == null || lastLog == null || !lastLog.KayitZamani.HasValue)
                        return null;

                    var grupTarihi = g.Key;
                    var hesapTarihi = lastLog.KayitZamani.Value.Date;
                    var gunlukMaksimumSuTuketimMap = gunBazliOzetRapor ? GunlukMaksimumDecimalDegerler(g, x => x.SuTuketimBugun) : new Dictionary<DateTime, decimal?>();

                    var gunlukMaksimumPedSuTuketimMap = gunBazliOzetRapor ? GunlukMaksimumDecimalDegerler(g, x => x.PedSuTuketimBugun) : new Dictionary<DateTime, decimal?>();

                    var gunlukMaksimumYemTuketimMap = gunBazliOzetRapor ? GunlukMaksimumDecimalDegerler(g, x => x.YemTuketimBugun) : new Dictionary<DateTime, decimal?>();

                    var haftaHesapTarihi = haftalikRapor ? grupTarihi : hesapTarihi;

                    var gunlukSonKayitlar = haftalikRapor ? g.Where(x => x.KayitZamani.HasValue).GroupBy(x => x.KayitZamani!.Value.Date).Select(x => x.OrderByDescending(y => y.KayitZamani).First()).OrderBy(x => x.KayitZamani).ToList() : new List<KumesLogYeni>();

                    var donem = kumesDonems.FirstOrDefault(x => x.GirisTarihi.HasValue && x.GirisTarihi.Value.Date <= hesapTarihi && (!x.CikisTarihi.HasValue || x.CikisTarihi.Value.Date >= hesapTarihi) && x.Silindi != true);

                    DateTime? donemGirisTarihi = donem?.GirisTarihi?.Date;

                    int toplamOlenSayisi = 0;
                    int toplamSaglikliSayisi = 0;

                    if (donem != null && donemGirisTarihi.HasValue)
                    {
                        toplamSaglikliSayisi = kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date >= donemGirisTarihi.Value &&
                                x.KayitZamani.Value.Date <= hesapTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0));

                        toplamOlenSayisi = kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date >= donemGirisTarihi.Value &&
                                x.KayitZamani.Value.Date <= hesapTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0));
                    }

                    int canliHayvanAdet = toplamSaglikliSayisi - toplamOlenSayisi;

                    if (canliHayvanAdet < 0)
                        canliHayvanAdet = 0;

                    decimal? haftalikOlum = 0;

                    if (haftalikRapor && donem != null && donemGirisTarihi.HasValue)
                    {
                        var oncekiHaftaSonuTarihi = grupTarihi.AddDays(-1);

                        decimal buHaftaCanliOrani = toplamSaglikliSayisi > 0 ? ((decimal)canliHayvanAdet / toplamSaglikliSayisi) * 100 : 100;

                        int oncekiHaftaToplamSaglikliSayisi = kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date >= donemGirisTarihi.Value &&
                                x.KayitZamani.Value.Date <= oncekiHaftaSonuTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0));

                        int oncekiHaftaToplamOlenSayisi = kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date >= donemGirisTarihi.Value &&
                                x.KayitZamani.Value.Date <= oncekiHaftaSonuTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0));

                        int oncekiHaftaCanliHayvanAdet = oncekiHaftaToplamSaglikliSayisi - oncekiHaftaToplamOlenSayisi;

                        if (oncekiHaftaCanliHayvanAdet < 0)
                            oncekiHaftaCanliHayvanAdet = 0;

                        decimal oncekiHaftaCanliOrani = oncekiHaftaToplamSaglikliSayisi > 0 ? ((decimal)oncekiHaftaCanliHayvanAdet / oncekiHaftaToplamSaglikliSayisi) * 100 : 100;

                        haftalikOlum = Math.Round((oncekiHaftaCanliOrani - buHaftaCanliOrani) * 100, 2);

                        if (haftalikOlum < 0)
                            haftalikOlum = 0;
                    }

                    int bugunkuOlenSayisi = donem != null
                        ? kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date == hesapTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0))
                        : 0;

                    int? yumurtaAdet = lastLog.KayitZamani.Value >= yumurtaRaporBaslangicTarihi ? lastLog.YumurtaAdet : null;

                    //decimal suTuketimBugun = lastLog.SuTuketimBugun.GetValueOrDefault(0);
                    //decimal yemTuketimBugun = lastLog.YemTuketimBugun.GetValueOrDefault(0);
                    var sonKayitZamani = lastLog.KayitZamani.Value;

                    decimal? suTuketimBugun;

                    decimal? pedSuTuketimBugun;

                    decimal? yemTuketimBugun;

                    if (gunBazliOzetRapor)
                    {
                        // Günlük raporda listede tek gün bulunacağından doğrudan
                        // o günün maksimumu döner.
                        //
                        // Haftalık ve aylık raporlarda ise her günün maksimum
                        // tüketim değerinin ortalaması alınır.
                        suTuketimBugun = OrtalamaDecimalListe(gunlukMaksimumSuTuketimMap.Values);
                        pedSuTuketimBugun = OrtalamaDecimalListe(gunlukMaksimumPedSuTuketimMap.Values);
                        yemTuketimBugun = OrtalamaDecimalListe(gunlukMaksimumYemTuketimMap.Values);
                    }
                    else
                    {
                        // Saatlik ve dakikalık raporların mevcut davranışı korunur.
                        suTuketimBugun = SonDoluGunlukDecimalDeger(kumesLogs, sonKayitZamani, x => x.SuTuketimBugun);
                        pedSuTuketimBugun = SonDoluGunlukDecimalDeger(kumesLogs, sonKayitZamani, x => x.PedSuTuketimBugun);
                        yemTuketimBugun = SonDoluGunlukDecimalDeger(kumesLogs, sonKayitZamani, x => x.YemTuketimBugun);
                    }

                    int? haftaSayisi = HesaplaHaftaSayisi(donem, haftaHesapTarihi);

                    var catiKontrol = haftalikRapor && haftaSayisi.HasValue && donem != null
                        ? catiKontroller.FirstOrDefault(x =>
                            x.DonemId == donem.Id &&
                            x.HaftaSayisi == haftaSayisi.Value &&
                            x.Silindi != true)
                        : null;

                    decimal? randiman = 0;
                    decimal? yumurtaBasiYemTuketim = 0;

                    var gunlukRandimanlar = new List<decimal?>();
                    var gunlukYumurtaBasiYemler = new List<decimal?>();
                    var gunlukHayvanBasinaSuTuketimler = new List<decimal?>();
                    var gunlukHayvanBasinaYemTuketimler = new List<decimal?>();
                    var gunlukHayvanBasinaYumurtaAdetler = new List<decimal?>();
                    var gunlukCanliHayvanAdetleri = new List<int>();
                    var gunlukOlenSayilari = new List<int>();
                    var gunlukYumurtaAdetleri = new List<int?>();

                    if (haftalikRapor)
                    {
                        foreach (var gunlukLog in gunlukSonKayitlar)
                        {
                            if (!gunlukLog.KayitZamani.HasValue)
                                continue;

                            var gunTarihi = gunlukLog.KayitZamani.Value.Date;

                            int? gunYumurtaAdet = gunlukLog.KayitZamani.Value >= yumurtaRaporBaslangicTarihi ? gunlukLog.YumurtaAdet : null;
                            //decimal gunSuTuketim = gunlukLog.SuTuketimBugun.GetValueOrDefault(0);
                            //decimal gunYemTuketim = gunlukLog.YemTuketimBugun.GetValueOrDefault(0);
                            decimal? gunSuTuketim = gunlukMaksimumSuTuketimMap.TryGetValue(gunTarihi, out var maksimumSuTuketim) ? maksimumSuTuketim : 0;

                            decimal? gunYemTuketim = gunlukMaksimumYemTuketimMap.TryGetValue(gunTarihi, out var maksimumYemTuketim) ? maksimumYemTuketim : 0;

                            gunlukYumurtaAdetleri.Add(gunYumurtaAdet);

                            var gunDonem = kumesDonems.FirstOrDefault(x =>
                                x.GirisTarihi.HasValue &&
                                x.GirisTarihi.Value.Date <= gunTarihi &&
                                (!x.CikisTarihi.HasValue || x.CikisTarihi.Value.Date >= gunTarihi) &&
                                x.Silindi != true);

                            if (gunDonem == null || !gunDonem.GirisTarihi.HasValue)
                                continue;

                            var gunDonemGirisTarihi = gunDonem.GirisTarihi.Value.Date;

                            int gunToplamSaglikliSayisi = kumesHayvanLogs
                                .Where(x =>
                                    x.KayitZamani.HasValue &&
                                    x.KayitZamani.Value.Date >= gunDonemGirisTarihi &&
                                    x.KayitZamani.Value.Date <= gunTarihi &&
                                    x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli &&
                                    x.DonemId == gunDonem.Id)
                                .Sum(x => x.Adet.GetValueOrDefault(0));

                            int gunToplamOlenSayisi = kumesHayvanLogs
                                .Where(x =>
                                    x.KayitZamani.HasValue &&
                                    x.KayitZamani.Value.Date >= gunDonemGirisTarihi &&
                                    x.KayitZamani.Value.Date <= gunTarihi &&
                                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                    x.DonemId == gunDonem.Id)
                                .Sum(x => x.Adet.GetValueOrDefault(0));

                            int gunCanliHayvanAdet = gunToplamSaglikliSayisi - gunToplamOlenSayisi;

                            if (gunCanliHayvanAdet < 0)
                                gunCanliHayvanAdet = 0;

                            gunlukCanliHayvanAdetleri.Add(gunCanliHayvanAdet);

                            int gunOlenSayisi = kumesHayvanLogs
                                .Where(x =>
                                    x.KayitZamani.HasValue &&
                                    x.KayitZamani.Value.Date == gunTarihi &&
                                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                    x.DonemId == gunDonem.Id)
                                .Sum(x => x.Adet.GetValueOrDefault(0));

                            gunlukOlenSayilari.Add(gunOlenSayisi);

                            if (gunCanliHayvanAdet > 0 && gunYumurtaAdet.HasValue)
                            {
                                decimal gunRandiman = ((decimal)gunYumurtaAdet.Value / gunCanliHayvanAdet) * 100;
                                gunlukRandimanlar.Add(gunRandiman);

                                decimal? gunHayvanBasinaSu = gunSuTuketim.HasValue ? (gunSuTuketim.Value * 1000000m) / gunCanliHayvanAdet : null;

                                decimal? gunHayvanBasinaYem = gunYemTuketim.HasValue ? (gunYemTuketim.Value * 1000000m) / gunCanliHayvanAdet : null;

                                decimal gunHayvanBasinaYumurta = (decimal)gunYumurtaAdet.Value / gunCanliHayvanAdet;

                                gunlukHayvanBasinaSuTuketimler.Add(gunHayvanBasinaSu);
                                gunlukHayvanBasinaYemTuketimler.Add(gunHayvanBasinaYem);
                                gunlukHayvanBasinaYumurtaAdetler.Add(gunHayvanBasinaYumurta);
                            }

                            decimal? gunYumurtaBasiYem = gunYumurtaAdet.HasValue ? HesaplaYumurtaBasiYemTuketim(gunYemTuketim, gunYumurtaAdet.Value) : null;

                            if (gunYumurtaBasiYem.GetValueOrDefault(0) > 0)
                            {
                                gunlukYumurtaBasiYemler.Add(gunYumurtaBasiYem.GetValueOrDefault(0));
                            }
                        }

                        randiman = OrtalamaDecimalListe(gunlukRandimanlar);
                        yumurtaBasiYemTuketim = OrtalamaDecimalListe(gunlukYumurtaBasiYemler);
                    }
                    else
                    {
                        randiman = canliHayvanAdet > 0 && yumurtaAdet.HasValue ? Math.Round(((decimal)yumurtaAdet.Value / canliHayvanAdet) * 100, 2) : null;
                        yumurtaBasiYemTuketim = yumurtaAdet.HasValue ? HesaplaYumurtaBasiYemTuketim(yemTuketimBugun, yumurtaAdet.Value) : null;
                    }

                    decimal? raporSicaklikKumes = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.SicaklikKumes) : firstLog.SicaklikKumes;

                    decimal? raporSicaklikDis = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.SicaklikDis) : firstLog.SicaklikDis;

                    decimal? raporSicaklik1 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik1) : firstLog.Sicaklik1;

                    decimal? raporSicaklik2 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik2) : firstLog.Sicaklik2;

                    decimal? raporSicaklik3 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik3) : firstLog.Sicaklik3;

                    decimal? raporSicaklik4 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik4) : firstLog.Sicaklik4;

                    decimal? raporSicaklik5 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik5) : firstLog.Sicaklik5;

                    decimal? raporSicaklik6 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Sicaklik6) : firstLog.Sicaklik6;

                    decimal? raporNemKumes = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.NemKumes) : firstLog.NemKumes;

                    decimal? raporNemDis = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.NemDis) : firstLog.NemDis;

                    decimal? raporNem1 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem1) : firstLog.Nem1;

                    decimal? raporNem2 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem2) : firstLog.Nem2;

                    decimal? raporNem3 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem3) : firstLog.Nem3;

                    decimal? raporNem4 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem4) : firstLog.Nem4;

                    decimal? raporNem5 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem5) : firstLog.Nem5;

                    decimal? raporNem6 = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Nem6) : firstLog.Nem6;

                    decimal? raporCo2Kumes;

                    if (haftalikRapor)
                    {
                        decimal? ortalamaCo2 = OrtalamaDecimal(gunlukSonKayitlar, x => x.Co2_1);
                        raporCo2Kumes = ortalamaCo2.HasValue ? Math.Round(ortalamaCo2.Value / 10m, 2) : null;
                    }
                    else
                    {
                        raporCo2Kumes = firstLog.Co2_1.HasValue ? Math.Round(firstLog.Co2_1.Value / 10m, 2) : null;
                    }
                    decimal? raporBasincKumes = haftalikRapor ? OrtalamaDecimal(gunlukSonKayitlar, x => x.Basinc1) : firstLog.Basinc1;

                    //decimal raporSuTuketimBugun = haftalikRapor
                    //    ? OrtalamaDecimal(gunlukSonKayitlar, x => x.SuTuketimBugun)
                    //    : suTuketimBugun;

                    //decimal raporYemTuketimBugun = haftalikRapor
                    //    ? OrtalamaDecimal(gunlukSonKayitlar, x => x.YemTuketimBugun)
                    //    : yemTuketimBugun;

                    decimal? raporSuTuketimBugun = suTuketimBugun;

                    decimal? raporPedSuTuketimBugun = pedSuTuketimBugun;

                    decimal? raporYemTuketimBugun = yemTuketimBugun;

                    decimal? raporSilo1 = lastLog.Silo1;

                    decimal? raporSilo2 = lastLog.Silo2;

                    int? raporYumurtaAdet = haftalikRapor ? OrtalamaIntListe(gunlukYumurtaAdetleri) : yumurtaAdet;

                    int raporOlenSayisiBugun = donem != null
                                              ? kumesHayvanLogs
                                                  .Where(x =>
                                                      x.KayitZamani.HasValue &&
                                                      x.KayitZamani.Value.Date >= grupTarihi.Date &&
                                                      x.KayitZamani.Value.Date <= grupTarihi.Date.AddDays(6) &&
                                                      x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                                      x.DonemId == donem.Id)
                                                  .Sum(x => x.Adet.GetValueOrDefault(0))
                                              : 0;

                    //int raporCanliHayvanAdet = haftalikRapor
                    //    ? OrtalamaIntListe(gunlukCanliHayvanAdetleri)
                    //    : canliHayvanAdet;

                    decimal? raporHayvanBasinaSuTuketim = haftalikRapor ? OrtalamaDecimalListe(gunlukHayvanBasinaSuTuketimler) : canliHayvanAdet > 0 && suTuketimBugun.HasValue ? Math.Round((suTuketimBugun.Value * 1000000m) / canliHayvanAdet, 2) : null;

                    decimal? raporHayvanBasinaYemTuketim = haftalikRapor ? OrtalamaDecimalListe(gunlukHayvanBasinaYemTuketimler) : canliHayvanAdet > 0 && yemTuketimBugun.HasValue ? Math.Round((yemTuketimBugun.Value * 1000000m) / canliHayvanAdet, 2) : null;

                    decimal? haftalikHayvanBasinaYumurta = OrtalamaDecimalListe(gunlukHayvanBasinaYumurtaAdetler);

                    decimal? raporHayvanBasinaYumurtaAdet = haftalikRapor ? OrtalamaDecimalListe(gunlukHayvanBasinaYumurtaAdetler, 4) : canliHayvanAdet > 0 && yumurtaAdet.HasValue ? Math.Round((decimal)yumurtaAdet.Value / canliHayvanAdet, 4) : null;
                    return new KumesRaporModel
                    {
                        KayitZamani = grupTarihi,

                        SicaklikKumes = raporSicaklikKumes,
                        SicaklikDis = raporSicaklikDis,
                        Sicaklik1 = raporSicaklik1,
                        Sicaklik2 = raporSicaklik2,
                        Sicaklik3 = raporSicaklik3,
                        Sicaklik4 = raporSicaklik4,
                        Sicaklik5 = raporSicaklik5,
                        Sicaklik6 = raporSicaklik6,

                        NemKumes = raporNemKumes,
                        NemDis = raporNemDis,
                        Nem1 = raporNem1,
                        Nem2 = raporNem2,
                        Nem3 = raporNem3,
                        Nem4 = raporNem4,
                        Nem5 = raporNem5,
                        Nem6 = raporNem6,

                        Co2Kumes = raporCo2Kumes,
                        BasincKumes = raporBasincKumes,

                        SuTuketimBugun = raporSuTuketimBugun,
                        PedSuTuketimBugun = raporPedSuTuketimBugun,
                        IcmeSuyuSayac = lastLog.IcmeSuyuSayac,
                        PedSuSayac = lastLog.PedSuSayac,
                        YemTuketimBugun = raporYemTuketimBugun,
                        Silo1 = raporSilo1,
                        Silo2 = raporSilo2,

                        YumurtaAdet = raporYumurtaAdet,
                        OlenSayisiBugun = raporOlenSayisiBugun,
                        CanliHayvanAdet = canliHayvanAdet,

                        HayvanBasinaSuTuketim = raporHayvanBasinaSuTuketim,
                        HayvanBasinaYemTuketim = raporHayvanBasinaYemTuketim,
                        HayvanBasinaYumurtaAdet = raporHayvanBasinaYumurtaAdet,

                        HaftaSayisi = haftaSayisi,
                        YumurtaBasinaYemTuketim = yumurtaBasiYemTuketim,
                        Randiman = randiman,
                        RandimanGuide = GetRandimanGuideByHafta(haftaSayisi, randimanRehberleri),
                        HaftalikOlum = haftalikOlum,
                        CatiKontrolDeger = catiKontrol?.Deger
                    };
                })
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();

            return grouped;
        }

        /// <summary>
        /// kumes-topbar > Rahmiye Çiftlik > "Yumurta Raporu" popup'ı için ayrılmış rapor.
        /// GetKumesRaporByKumesId'nin (tam rapor) bir kopyasıdır; sadece Yumurta Adet,
        /// Yumurta Başı Yem Tüketim, Randıman ve Randıman Hedefi hesaplanıp döner.
        /// Diğer raporun (sıcaklık/nem/co2/basınç/su/silo/ölen sayısı/hayvan başı
        /// su-yem-yumurta/haftalık ölüm/çatı kontrol) ilgili satırları burada bilinçli
        /// olarak yorum satırına alındı - tam raporla karşılaştırmayı kolaylaştırmak ve
        /// gerekirse geri açılabilmelerini sağlamak için silinmediler.
        /// </summary>
        public async Task<List<KumesRaporModel>> GetKumesYumurtaRaporByKumesId(int kumesId, DateTime baslangicZamani, DateTime bitisZamani, int dakikaInterval = 60)
        {
            List<KumesRaporModel> result = new List<KumesRaporModel>();

            var kumes = await _kumesRepository.GetAsyncByPredicate(x => x.Id == kumesId && x.Silindi != true);

            if (kumes == null)
                return result;

            var randimanRehberleri =
                (await _kumesRaporRehberRepository.GetAllAsyncByPredicateWithAsNoTracking(x =>
                    x.LokasyonId == kumes.LokasyonId &&
                    x.RehberAdi == KumesRaporRehberTipleri.Randiman &&
                    x.Silindi != true))
                .GroupBy(x => x.HaftaSayisi)
                .ToDictionary(
                    x => x.Key,
                    x => x.OrderByDescending(y => y.GuncellemeZamani ?? y.KayitZamani)
                          .First()
                          .Deger);

            DateTime yumurtaRaporBaslangicTarihi = new DateTime(2026, 5, 13, 12, 0, 0);

            static Dictionary<DateTime, decimal?> GunlukMaksimumDecimalDegerler(IEnumerable<KumesLogYeni> logs, Func<KumesLogYeni, decimal?> selector)
            {
                return logs
                    .Where(x => x.KayitZamani.HasValue)
                    .GroupBy(x => x.KayitZamani!.Value.Date)
                    .Select(g => new
                    {
                        Tarih = g.Key,
                        Degerler = g
                            .Select(selector)
                            .Where(x => x.HasValue)
                            .Select(x => x!.Value)
                            .ToList()
                    })
                    .Where(x => x.Degerler.Any())
                    .ToDictionary(
                        x => x.Tarih,
                        x => (decimal?)x.Degerler.Max());
            }

            // Tam raporda RaporlanabilirLogMu (sıcaklık/nem/... dahil tüm alanlar)
            // kullanılıyordu; burada sadece Yumurta Adet ve Yem Tüketim alanı dolu
            // olan kayıtlar çekiliyor - sorgu ve işlenen veri seti daha küçük olur.
            static bool RaporlanabilirYumurtaLogMu(KumesLogYeni x)
            {
                return x.YumurtaAdet.HasValue || x.YemTuketimBugun.HasValue;
            }

            static decimal? OrtalamaDecimalListe(IEnumerable<decimal?> values, int basamak = 2)
            {
                var list = values.Where(x => x.HasValue).Select(x => x!.Value).ToList();
                return list.Any() ? Math.Round(list.Average(), basamak) : null;
            }

            static int? OrtalamaIntListe(IEnumerable<int?> values)
            {
                var list = values.Where(x => x.HasValue).Select(x => x.Value).ToList();
                return list.Any() ? Convert.ToInt32(Math.Round(list.Average(), 0, MidpointRounding.AwayFromZero)) : null;
            }

            var kumesLogs = (await _kumesLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.KayitZamani.HasValue && x.KayitZamani.Value >= baslangicZamani && x.KayitZamani.Value <= bitisZamani && x.Silindi != true))
                .Where(RaporlanabilirYumurtaLogMu).OrderBy(x => x.KayitZamani).ToList();

            if (!kumesLogs.Any())
                return result;

            var kumesDonems = (await _donemRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.GirisTarihi.HasValue && x.GirisTarihi.Value <= bitisZamani && (!x.CikisTarihi.HasValue || x.CikisTarihi.Value >= baslangicZamani) && x.Silindi != true)).OrderBy(x => x.GirisTarihi).ToList();

            var minDonemZamani = kumesDonems.MinBy(x => x.GirisTarihi)?.GirisTarihi?.Date ?? baslangicZamani.Date;

            // Randıman, canlı hayvan adedine ihtiyaç duyduğu için hayvan logları kalıyor.
            var kumesHayvanLogs =
                (await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x =>
                    x.KumesId == kumes.Id &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value.Date >= minDonemZamani &&
                    x.KayitZamani.Value.Date <= bitisZamani.Date &&
                    x.Silindi != true))
                .OrderBy(x => x.KayitZamani)
                .ToList();

            bool gunlukRapor = dakikaInterval == 1440;
            bool haftalikRapor = dakikaInterval == 10080;
            bool aylikRapor = dakikaInterval >= 43200;

            bool gunBazliOzetRapor = gunlukRapor || haftalikRapor || aylikRapor;

            // Çatı kontrol yumurta raporunda gösterilmiyor.
            // var catiKontroller = haftalikRapor ? (await _kumesHaftalikCatiKontrolRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.Silindi != true)).ToList() : new List<KumesHaftalikCatiKontrol>();

            var grouped = kumesLogs.GroupBy<KumesLogYeni, System.DateTime>(k =>
                {
                    var kayitZamani = k.KayitZamani!.Value;

                    // Günlük
                    if (dakikaInterval == 1440)
                    {
                        return kayitZamani.Date;
                    }

                    // Haftalık - Pazartesi başlangıçlı
                    if (dakikaInterval == 10080)
                    {
                        int diff = (7 + (kayitZamani.DayOfWeek - DayOfWeek.Monday)) % 7;
                        return kayitZamani.Date.AddDays(-diff);
                    }

                    // Aylık
                    if (dakikaInterval >= 43200)
                    {
                        return new DateTime(kayitZamani.Year, kayitZamani.Month, 1);
                    }

                    // Saatlik / dakikalık interval:
                    // Kullanıcının seçtiği başlangıç zamanından itibaren gruplar.
                    var totalMinutes = (int)(kayitZamani - baslangicZamani).TotalMinutes;
                    var rounded = (totalMinutes / dakikaInterval) * dakikaInterval;

                    return baslangicZamani.AddMinutes(rounded);
                })
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var firstLog = g.OrderBy(x => x.KayitZamani).FirstOrDefault();
                    var lastLog = g.OrderBy(x => x.KayitZamani).LastOrDefault();

                    if (firstLog == null || lastLog == null || !lastLog.KayitZamani.HasValue)
                        return null;

                    var grupTarihi = g.Key;
                    var hesapTarihi = lastLog.KayitZamani.Value.Date;

                    var gunlukMaksimumYemTuketimMap = gunBazliOzetRapor ? GunlukMaksimumDecimalDegerler(g, x => x.YemTuketimBugun) : new Dictionary<DateTime, decimal?>();

                    var haftaHesapTarihi = haftalikRapor ? grupTarihi : hesapTarihi;

                    var gunlukSonKayitlar = haftalikRapor ? g.Where(x => x.KayitZamani.HasValue).GroupBy(x => x.KayitZamani!.Value.Date).Select(x => x.OrderByDescending(y => y.KayitZamani).First()).OrderBy(x => x.KayitZamani).ToList() : new List<KumesLogYeni>();

                    var donem = kumesDonems.FirstOrDefault(x => x.GirisTarihi.HasValue && x.GirisTarihi.Value.Date <= hesapTarihi && (!x.CikisTarihi.HasValue || x.CikisTarihi.Value.Date >= hesapTarihi) && x.Silindi != true);

                    DateTime? donemGirisTarihi = donem?.GirisTarihi?.Date;

                    int toplamOlenSayisi = 0;
                    int toplamSaglikliSayisi = 0;

                    if (donem != null && donemGirisTarihi.HasValue)
                    {
                        toplamSaglikliSayisi = kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date >= donemGirisTarihi.Value &&
                                x.KayitZamani.Value.Date <= hesapTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0));

                        toplamOlenSayisi = kumesHayvanLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date >= donemGirisTarihi.Value &&
                                x.KayitZamani.Value.Date <= hesapTarihi &&
                                x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                x.DonemId == donem.Id)
                            .Sum(x => x.Adet.GetValueOrDefault(0));
                    }

                    int canliHayvanAdet = toplamSaglikliSayisi - toplamOlenSayisi;

                    if (canliHayvanAdet < 0)
                        canliHayvanAdet = 0;

                    // Haftalık ölüm oranı yumurta raporunda gösterilmiyor.
                    // decimal? haftalikOlum = 0;
                    // if (haftalikRapor && donem != null && donemGirisTarihi.HasValue) { ... }

                    // Ölen hayvan sayısı yumurta raporunda gösterilmiyor.
                    // int bugunkuOlenSayisi = ...;

                    int? yumurtaAdet = lastLog.KayitZamani.Value >= yumurtaRaporBaslangicTarihi ? lastLog.YumurtaAdet : null;

                    var sonKayitZamani = lastLog.KayitZamani.Value;

                    // Su/ped su tüketimi yumurta raporunda gösterilmiyor; sadece yem
                    // tüketimi hesaplanıyor (yumurta başı yem tüketim için gerekli).
                    decimal? yemTuketimBugun;

                    if (gunBazliOzetRapor)
                    {
                        yemTuketimBugun = OrtalamaDecimalListe(gunlukMaksimumYemTuketimMap.Values);
                    }
                    else
                    {
                        yemTuketimBugun = kumesLogs
                            .Where(x =>
                                x.KayitZamani.HasValue &&
                                x.KayitZamani.Value.Date == sonKayitZamani.Date &&
                                x.KayitZamani.Value <= sonKayitZamani &&
                                x.YemTuketimBugun.HasValue)
                            .OrderByDescending(x => x.KayitZamani)
                            .Select(x => x.YemTuketimBugun)
                            .FirstOrDefault();
                    }

                    int? haftaSayisi = HesaplaHaftaSayisi(donem, haftaHesapTarihi);

                    // Çatı kontrol yumurta raporunda gösterilmiyor.
                    // var catiKontrol = haftalikRapor && haftaSayisi.HasValue && donem != null
                    //     ? catiKontroller.FirstOrDefault(x => x.DonemId == donem.Id && x.HaftaSayisi == haftaSayisi.Value && x.Silindi != true)
                    //     : null;

                    decimal? randiman = 0;
                    decimal? yumurtaBasiYemTuketim = 0;

                    var gunlukRandimanlar = new List<decimal?>();
                    var gunlukYumurtaBasiYemler = new List<decimal?>();
                    var gunlukHayvanBasinaYumurtaAdetler = new List<decimal?>();
                    // Hayvan başı su/yem ve günlük ölüm sayıları yumurta raporunda gösterilmiyor.
                    // var gunlukHayvanBasinaSuTuketimler = new List<decimal?>();
                    // var gunlukHayvanBasinaYemTuketimler = new List<decimal?>();
                    // var gunlukCanliHayvanAdetleri = new List<int>();
                    // var gunlukOlenSayilari = new List<int>();
                    var gunlukYumurtaAdetleri = new List<int?>();

                    if (haftalikRapor)
                    {
                        foreach (var gunlukLog in gunlukSonKayitlar)
                        {
                            if (!gunlukLog.KayitZamani.HasValue)
                                continue;

                            var gunTarihi = gunlukLog.KayitZamani.Value.Date;

                            int? gunYumurtaAdet = gunlukLog.KayitZamani.Value >= yumurtaRaporBaslangicTarihi ? gunlukLog.YumurtaAdet : null;
                            decimal? gunYemTuketim = gunlukMaksimumYemTuketimMap.TryGetValue(gunTarihi, out var maksimumYemTuketim) ? maksimumYemTuketim : 0;

                            gunlukYumurtaAdetleri.Add(gunYumurtaAdet);

                            var gunDonem = kumesDonems.FirstOrDefault(x =>
                                x.GirisTarihi.HasValue &&
                                x.GirisTarihi.Value.Date <= gunTarihi &&
                                (!x.CikisTarihi.HasValue || x.CikisTarihi.Value.Date >= gunTarihi) &&
                                x.Silindi != true);

                            if (gunDonem == null || !gunDonem.GirisTarihi.HasValue)
                                continue;

                            var gunDonemGirisTarihi = gunDonem.GirisTarihi.Value.Date;

                            int gunToplamSaglikliSayisi = kumesHayvanLogs
                                .Where(x =>
                                    x.KayitZamani.HasValue &&
                                    x.KayitZamani.Value.Date >= gunDonemGirisTarihi &&
                                    x.KayitZamani.Value.Date <= gunTarihi &&
                                    x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli &&
                                    x.DonemId == gunDonem.Id)
                                .Sum(x => x.Adet.GetValueOrDefault(0));

                            int gunToplamOlenSayisi = kumesHayvanLogs
                                .Where(x =>
                                    x.KayitZamani.HasValue &&
                                    x.KayitZamani.Value.Date >= gunDonemGirisTarihi &&
                                    x.KayitZamani.Value.Date <= gunTarihi &&
                                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                                    x.DonemId == gunDonem.Id)
                                .Sum(x => x.Adet.GetValueOrDefault(0));

                            int gunCanliHayvanAdet = gunToplamSaglikliSayisi - gunToplamOlenSayisi;

                            if (gunCanliHayvanAdet < 0)
                                gunCanliHayvanAdet = 0;

                            // gunlukCanliHayvanAdetleri.Add(gunCanliHayvanAdet);

                            // int gunOlenSayisi = kumesHayvanLogs.Where(...).Sum(...);
                            // gunlukOlenSayilari.Add(gunOlenSayisi);

                            if (gunCanliHayvanAdet > 0 && gunYumurtaAdet.HasValue)
                            {
                                decimal gunRandiman = ((decimal)gunYumurtaAdet.Value / gunCanliHayvanAdet) * 100;
                                gunlukRandimanlar.Add(gunRandiman);

                                // decimal? gunHayvanBasinaSu = gunSuTuketim.HasValue ? (gunSuTuketim.Value * 1000000m) / gunCanliHayvanAdet : null;
                                // decimal? gunHayvanBasinaYem = gunYemTuketim.HasValue ? (gunYemTuketim.Value * 1000000m) / gunCanliHayvanAdet : null;
                                // gunlukHayvanBasinaSuTuketimler.Add(gunHayvanBasinaSu);
                                // gunlukHayvanBasinaYemTuketimler.Add(gunHayvanBasinaYem);

                                decimal gunHayvanBasinaYumurta = (decimal)gunYumurtaAdet.Value / gunCanliHayvanAdet;
                                gunlukHayvanBasinaYumurtaAdetler.Add(gunHayvanBasinaYumurta);
                            }

                            decimal? gunYumurtaBasiYem = gunYumurtaAdet.HasValue ? HesaplaYumurtaBasiYemTuketim(gunYemTuketim, gunYumurtaAdet.Value) : null;

                            if (gunYumurtaBasiYem.GetValueOrDefault(0) > 0)
                            {
                                gunlukYumurtaBasiYemler.Add(gunYumurtaBasiYem.GetValueOrDefault(0));
                            }
                        }

                        randiman = OrtalamaDecimalListe(gunlukRandimanlar);
                        yumurtaBasiYemTuketim = OrtalamaDecimalListe(gunlukYumurtaBasiYemler);
                    }
                    else
                    {
                        randiman = canliHayvanAdet > 0 && yumurtaAdet.HasValue ? Math.Round(((decimal)yumurtaAdet.Value / canliHayvanAdet) * 100, 2) : null;
                        yumurtaBasiYemTuketim = yumurtaAdet.HasValue ? HesaplaYumurtaBasiYemTuketim(yemTuketimBugun, yumurtaAdet.Value) : null;
                    }

                    // Sıcaklık/nem/co2/basınç yumurta raporunda gösterilmiyor.
                    // decimal? raporSicaklikKumes = ...; decimal? raporSicaklikDis = ...; ... vb.
                    // decimal? raporNemKumes = ...; ... vb.
                    // decimal? raporCo2Kumes = ...;
                    // decimal? raporBasincKumes = ...;

                    // Su/ped su/silo tüketimi yumurta raporunda gösterilmiyor.
                    // decimal? raporSuTuketimBugun = suTuketimBugun;
                    // decimal? raporPedSuTuketimBugun = pedSuTuketimBugun;
                    // decimal? raporSilo1 = lastLog.Silo1;
                    // decimal? raporSilo2 = lastLog.Silo2;

                    int? raporYumurtaAdet = haftalikRapor ? OrtalamaIntListe(gunlukYumurtaAdetleri) : yumurtaAdet;

                    // Ölen hayvan sayısı yumurta raporunda gösterilmiyor.
                    // int raporOlenSayisiBugun = ...;

                    // Hayvan başı su/yem yumurta raporunda gösterilmiyor.
                    // decimal? raporHayvanBasinaSuTuketim = ...;
                    // decimal? raporHayvanBasinaYemTuketim = ...;

                    decimal? raporHayvanBasinaYumurtaAdet = haftalikRapor
                        ? OrtalamaDecimalListe(gunlukHayvanBasinaYumurtaAdetler, 4)
                        : canliHayvanAdet > 0 && yumurtaAdet.HasValue
                            ? Math.Round((decimal)yumurtaAdet.Value / canliHayvanAdet, 4)
                            : null;

                    return new KumesRaporModel
                    {
                        KayitZamani = grupTarihi,

                        YumurtaAdet = raporYumurtaAdet,
                        CanliHayvanAdet = canliHayvanAdet,
                        HayvanBasinaYumurtaAdet = raporHayvanBasinaYumurtaAdet,

                        HaftaSayisi = haftaSayisi,
                        YumurtaBasinaYemTuketim = yumurtaBasiYemTuketim,
                        Randiman = randiman,
                        RandimanGuide = GetRandimanGuideByHafta(haftaSayisi, randimanRehberleri),
                    };
                })
                .Where(x => x != null)
                .Select(x => x!)
                .ToList();

            return grouped;
        }

        private static int? HesaplaHaftaSayisi(Donem? donem, DateTime kayitTarihi)
        {
            if (donem == null)
                return null;

            if (!donem.GirisTarihi.HasValue)
                return null;

            if (!donem.Yas.HasValue)
                return null;

            /*
             * donem.Yas:
             * Giriş yapılan haftanın Pazartesi bazlı hafta değeri.
             *
             * Bu yüzden hesap giriş tarihinden değil,
             * giriş tarihinin bulunduğu haftanın Pazartesi gününden başlar.
             */

            var girisHaftasiPazartesi = GetPazartesiBaslangici(donem.GirisTarihi.Value.Date);
            var kayitHaftasiPazartesi = GetPazartesiBaslangici(kayitTarihi.Date);

            var gunFarki = (kayitHaftasiPazartesi - girisHaftasiPazartesi).TotalDays;

            if (gunFarki < 0)
                return donem.Yas.Value;

            var gecenHafta = (int)(gunFarki / 7d);

            return donem.Yas.Value + gecenHafta;
        }

        private static decimal? GetRandimanGuideByHafta(int? haftaSayisi, Dictionary<int, decimal> randimanGuideDict)
        {
            if (!haftaSayisi.HasValue)
                return null;

            return randimanGuideDict.TryGetValue(haftaSayisi.Value, out var value)
                ? value
                : null;
        }

        private static decimal? HesaplaYumurtaBasiYemTuketim(decimal? yemTuketimBugun, int yumurtaAdet, decimal minDeger = 0, decimal maxDeger = 300)
        {
            if (yumurtaAdet <= 0)
                return 0;

            if (!yemTuketimBugun.HasValue)
                return 0;

            var deger = (yemTuketimBugun.Value * 1000000) / yumurtaAdet;

            // Gürültü / sapma veri filtresi
            if (deger < minDeger || deger > maxDeger)
                return 0;

            return Math.Round(deger, 2);
        }

        public async Task<KumesScadaModel> GetKumesBilgileriByKumesId(int kumesId)
        {
            var kumes = await _kumesRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Id == kumesId && x.Silindi != true);
            if (kumes == null)
            {
                throw new Exception("Kumes bulunamadı");
            }
            KumesScadaModel result = new KumesScadaModel();
            result.KumesId = kumes.Id;
            result.KumesName = kumes.Name;
            result.KumesHmiIpAdres = kumes.HmiIpAdres;
            result.CizimId = kumes.CizimId;
            result.CizimUrl = kumes.CizimUrl;
            var donem = await _donemRepository.GetAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.Silindi != true && x.Aktif != false && x.CikisTarihi == null);
            if (donem != null)
            {
                var oluhayvanAdet = (await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.DonemId == donem.Id && x.Silindi != true && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu)).Sum(x => x.Adet.GetValueOrDefault(0));
                result.DonemId = donem.Id;
                result.DonemName = donem == null ? "" : donem.Name == null ? "" : donem.Name;
                var now = DateTime.Now;
                if (donem.GirisTarihi != null)
                {
                    result.DonemGun = (now - donem.GirisTarihi.Value).Days;
                    // Haftaları hesapla
                    int haftaSayisi = (int)Math.Ceiling(result.DonemGun.Value / 7.0);

                    // Haftanın başlangıç günü olarak Pazartesi hesaplama
                    DateTime ilkHaftaBaslangici = donem.GirisTarihi.Value;
                    if (donem.GirisTarihi.Value.DayOfWeek != DayOfWeek.Monday)
                    {
                        // İlk haftanın başına git
                        int daysToSubtract = ((int)donem.GirisTarihi.Value.DayOfWeek + 6) % 7;
                        ilkHaftaBaslangici = donem.GirisTarihi.Value.AddDays(-daysToSubtract);
                    }

                    // Haftalar arası günleri kontrol et
                    int toplamHaftaSayisi = (int)((now - ilkHaftaBaslangici).Days / 7) + 1;
                    result.DonemHafta = toplamHaftaSayisi;
                    result.CanliHayvanAdet = donem.HayvanAdet.GetValueOrDefault(0) - oluhayvanAdet;
                }
            }
            return result;
        }

        public async Task<List<AlarmViewModel>> GetAlarmLogForKumes(int kumesId)
        {
            List<AlarmViewModel> result = new List<AlarmViewModel>();
            var alarmlar = await _kumesRepository.GetAlarmLogForKumes(kumesId);

            result.AddRange(_mapper.Map<List<AlarmViewModel>>(alarmlar));
            return result;
        }

        public async Task<List<AlarmViewModel>> GetAktifAlarmLogForKumes(int kumesId)
        {
            List<AlarmViewModel> result = new List<AlarmViewModel>();
            var alarmlar = await _kumesRepository.GetAktifAlarmLogForKumes(kumesId);

            result.AddRange(_mapper.Map<List<AlarmViewModel>>(alarmlar));
            return result;
        }

        public async Task<List<AlarmViewModel>> GetPasifAlarmLogForKumes(int kumesId)
        {
            List<AlarmViewModel> result = new List<AlarmViewModel>();
            var alarmlar = await _kumesRepository.GetPasifAlarmLogForKumes(kumesId);

            result.AddRange(_mapper.Map<List<AlarmViewModel>>(alarmlar));
            return result;
        }

        public async Task<KumesDonemModel?> GetKumesDonemBilgisiByKumesId(int kumesId)
        {
            KumesDonemModel result = new KumesDonemModel();

            var aktifDonem = await _donemRepository.GetAsyncByPredicateWithAsNoTracking(x =>
                x.KumesId == kumesId &&
                x.Silindi != true &&
                x.Aktif != false);

            if (aktifDonem == null)
                return result;

            result.KumesId = aktifDonem.KumesId;
            result.DonemId = aktifDonem.Id;
            result.DonemName = aktifDonem.Name;
            result.GirisTarihi = aktifDonem.GirisTarihi;
            result.CikisTarihi = aktifDonem.CikisTarihi;
            result.Aktif = aktifDonem.Aktif;
            result.Cins = aktifDonem.Cins;
            result.GirenHayvanAdet = aktifDonem.HayvanAdet;

            var olenHayvan = (await _kumesHayvanLogRepository
                .GetAllAsyncByPredicateWithAsNoTracking(x =>
                    x.KumesId == kumesId &&
                    x.DonemId == aktifDonem.Id &&
                    x.Silindi != true &&
                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu))
                .Sum(x => x.Adet.GetValueOrDefault(0));

            result.CanliHayvanAdet = aktifDonem.HayvanAdet - olenHayvan;

            var bugunOlenHayvan = (await _kumesHayvanLogRepository
                .GetAllAsyncByPredicateWithAsNoTracking(x =>
                    x.KumesId == kumesId &&
                    x.DonemId == aktifDonem.Id &&
                    x.Silindi != true &&
                    x.SaglikDurumu == KumesHayvanSaglikDurum.Olu &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value.Date == DateTime.Now.Date))
                .Sum(x => x.Adet.GetValueOrDefault(0));

            result.BugunOlenHayvanAdet = bugunOlenHayvan;
            result.ToplamOlenHayvanAdet = olenHayvan;

            if (aktifDonem.GirisTarihi.HasValue)
            {
                var bugun = DateTime.Now.Date;
                var girisTarihi = aktifDonem.GirisTarihi.Value.Date;

                result.DonemGun = (bugun - girisTarihi).Days.ToString();

                var girisHaftasiPazartesi = GetPazartesiBaslangici(girisTarihi);
                var bugununHaftasiPazartesi = GetPazartesiBaslangici(bugun);

                int gecenHaftaFarki = (int)((bugununHaftasiPazartesi - girisHaftasiPazartesi).TotalDays / 7d);

                if (gecenHaftaFarki < 0)
                    gecenHaftaFarki = 0;

                int girisYasi = aktifDonem.Yas.GetValueOrDefault(0);

                /*
                 * aktifDonem.Yas zaten giriş haftasının Pazartesi bazlı hafta değeri.
                 * Bu yüzden +1 yapılmaz.
                 */
                int hayvanHafta = girisYasi + gecenHaftaFarki;

                result.DonemHafta = gecenHaftaFarki.ToString();
                result.HayvanHafta = hayvanHafta.ToString();
            }

            return result;
        }

        private static DateTime GetPazartesiBaslangici(DateTime tarih)
        {
            int diff = (7 + (tarih.DayOfWeek - DayOfWeek.Monday)) % 7;
            return tarih.Date.AddDays(-diff);
        }

        public async Task<int?> GetOlenHayvanSayisiByDate(int kumesId, DateTime tarih)
        {
            int result = 0;
            var aktifDonem = await _donemRepository.GetAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumesId && x.Silindi != true && x.Aktif != false);
            if (aktifDonem == null)
            {
            }
            else
            {
                return (await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumesId && x.DonemId == aktifDonem.Id && x.Silindi != true && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu && x.KayitZamani.Value.Date == tarih.Date)).Sum(x => x.Adet.GetValueOrDefault(0));
            }
            return result;
        }

        public async Task AddKumesHayvanLog(int kumesId, int oluHayvanAdet, int saglikliHayvanAdet, DateTime tarih, string userId)
        {
            var aktifDonem = await _donemRepository.GetAsyncByPredicate(x => x.KumesId == kumesId && x.Silindi != true && x.Aktif != false);
            if (aktifDonem == null)
            {
                return;
            }
            var oluKumesHayvanLog = await _kumesHayvanLogRepository.GetAsyncByPredicate(x => x.KumesId == kumesId /*&& x.DonemId == aktifDonem.Id*/ && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu && x.KayitZamani.Value.Date == tarih.Date /*&& x.Silindi != true*/);
            //if (oluKumesHayvanLog == null)
            //{
            //    KumesHayvanLog kumesHayvanLogNew = new KumesHayvanLog();
            //    kumesHayvanLogNew.KumesId = kumesId;
            //    kumesHayvanLogNew.DonemId = aktifDonem.Id;
            //    kumesHayvanLogNew.Adet = oluHayvanAdet;
            //    kumesHayvanLogNew.SaglikDurumu = KumesHayvanSaglikDurum.Olu;
            //    kumesHayvanLogNew.GirisZamani = DateTime.Now;
            //    kumesHayvanLogNew.KayitZamani = tarih;
            //    kumesHayvanLogNew.KayitYapanUserId = userId;
            //    kumesHayvanLogNew.Silindi = false;
            //    await _kumesHayvanLogRepository.AddAsync(kumesHayvanLogNew);
            //}
            //else
            {
                oluKumesHayvanLog.DonemId = aktifDonem.Id;
                oluKumesHayvanLog.Adet = oluHayvanAdet;
                oluKumesHayvanLog.SaglikDurumu = KumesHayvanSaglikDurum.Olu;
                oluKumesHayvanLog.GirisZamani = DateTime.Now;
                oluKumesHayvanLog.KayitYapanUserId = userId;
                oluKumesHayvanLog.Silindi = false;
            }
            var saglikliKumesHayvanLogToplam = await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumesId && x.DonemId == aktifDonem.Id && x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli && x.KayitZamani.Value.Date != tarih.Date && x.Silindi != true);
            if (aktifDonem.HayvanAdet != saglikliHayvanAdet)
            {
                var fark = saglikliHayvanAdet - saglikliKumesHayvanLogToplam.Sum(x => x.Adet);

                var saglikliKumesHayvanLog = await _kumesHayvanLogRepository.GetAsyncByPredicate(x => x.KumesId == kumesId && x.DonemId == aktifDonem.Id && x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli && x.KayitZamani.Value.Date == tarih.Date && x.Silindi != true);
                if (saglikliKumesHayvanLog == null)
                {
                    KumesHayvanLog kumesHayvanLogNew = new KumesHayvanLog();
                    kumesHayvanLogNew.KumesId = kumesId;
                    kumesHayvanLogNew.DonemId = aktifDonem.Id;
                    kumesHayvanLogNew.Adet = fark;
                    kumesHayvanLogNew.SaglikDurumu = KumesHayvanSaglikDurum.Saglikli;
                    kumesHayvanLogNew.GirisZamani = DateTime.Now;
                    kumesHayvanLogNew.KayitZamani = tarih;
                    kumesHayvanLogNew.KayitYapanUserId = userId;
                    kumesHayvanLogNew.Silindi = false;
                    await _kumesHayvanLogRepository.AddAsync(kumesHayvanLogNew);
                }
                else
                {
                    saglikliKumesHayvanLog.Adet = fark;
                }
                await _kumesHayvanLogRepository.SaveChangesAsync();
                saglikliKumesHayvanLogToplam = await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumesId && x.DonemId == aktifDonem.Id && x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli /*&& x.KayitZamani.Value.Date == tarih.Date */&& x.Silindi != true);
                aktifDonem.HayvanAdet = saglikliKumesHayvanLogToplam.Sum(x => x.Adet.GetValueOrDefault(0));
            }
            await _kumesHayvanLogRepository.SaveChangesAsync();
        }

        public async Task KumesDonemBitir(int kumesId, string kumesDonemName, string userId)
        {
            var kumes = await _kumesRepository.GetByIdAsync(kumesId);
            if (kumes != null)
            {
                kumes.Aktif = false;
            }
            var aktifDonem = await _donemRepository.GetAsyncByPredicate(x => x.KumesId == kumesId && x.Silindi != true && x.Aktif != false);
            if (aktifDonem == null)
            {
                throw new Exception("Dönem bulunamadı!");
            }
            if (aktifDonem.Name == kumesDonemName)
            {
                aktifDonem.Aktif = false;
                aktifDonem.CikisTarihi = DateTime.Now;
                aktifDonem.GuncelleyenUserId = userId;
                aktifDonem.GuncellemeTarihi = DateTime.Now;
                await _donemRepository.SaveChangesAsync();
            }
            else
            {
                throw new Exception("Dönem adını doğru girmelisiniz!");
            }
        }

        public async Task KumesDonemBaslat(int kumesId, DateTime girisTarihi, string cins, int hayvanAdet, string donemAdi, int girisHaftaYas, string userId)
        {
            var kumes = await _kumesRepository.GetByIdAsync(kumesId);
            if (kumes != null)
            {
                kumes.Aktif = true;
            }
            Donem donemNew = new Donem();
            donemNew.KumesId = kumesId;
            donemNew.Name = donemAdi;
            donemNew.Cins = cins;
            donemNew.HayvanAdet = hayvanAdet;
            donemNew.GirisTarihi = girisTarihi;
            donemNew.Yas = girisHaftaYas;
            donemNew.Aktif = true;
            donemNew.Silindi = false;
            donemNew.GuncelleyenUserId = userId;
            donemNew.GuncellemeTarihi = DateTime.Now;
            await _donemRepository.AddAsync(donemNew);
            await _donemRepository.SaveChangesAsync();

            KumesHayvanLog newKumesHayvanLog = new();
            newKumesHayvanLog.KayitZamani = girisTarihi;
            newKumesHayvanLog.SaglikDurumu = KumesHayvanSaglikDurum.Saglikli;
            newKumesHayvanLog.Adet = hayvanAdet;
            newKumesHayvanLog.KumesId = kumesId;
            newKumesHayvanLog.DonemId = donemNew.Id;
            newKumesHayvanLog.KayitYapanUserId = userId;
            newKumesHayvanLog.Silindi = false;
            await _kumesHayvanLogRepository.AddAsync(newKumesHayvanLog);
            await _kumesHayvanLogRepository.SaveChangesAsync();
        }

        public async Task<List<HayvanCins>> GetAllHayvanCins()
        {
            return await _kumesRepository.GetAllHayvanCins();
        }

        public async Task<PlcHaberlesmeBilgiModel> GetKumesHaberlesmeBilgileriByKumesId(int kumesId)
        {
            PlcHaberlesmeBilgiModel result = new PlcHaberlesmeBilgiModel();
            //todo: servis kapandıysa bunları sıfır yap
            ///////////////////////////
            var now = DateTime.Now;
            var kumes = await _kumesRepository.GetByIdAsync(kumesId);
            //var connectedAndRead = await GetPlcBilgileriByKumesIdWithApi(kumesId, kumes.ServisApiUrl);
            //var byteDensity = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumesId && x.EtiketId == KumesEtiketTipler.Plc_ByteDensity && x.KayitZamani.Value.Date == now.Date)).OrderByDescending(x => x.Id).FirstOrDefault();
            //var plcLastPackageLength = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumesId && x.EtiketId == KumesEtiketTipler.Plc_LastPackageLength && x.KayitZamani.Value.Date == now.Date)).OrderByDescending(x => x.Id).FirstOrDefault();
            //var plcPingAverage = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumesId && x.EtiketId == KumesEtiketTipler.Plc_PingAverage && x.KayitZamani.Value.Date == now.Date)).OrderByDescending(x => x.Id).FirstOrDefault();
            var plcBagliEtiket = (await _etiketLogTempRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.KumesId == kumes.Id && x.EtiketId == KumesEtiketTipler.Plc_Bagli && x.KayitZamani.Value >= DateTime.Now.AddSeconds(-40))).OrderByDescending(x => x.Id).FirstOrDefault();
            var plcBagli = plcBagliEtiket == null ? false : plcBagliEtiket.Deger == 1 ? true : false;

            result.PlcIpAdres = kumes.PlcIpAdres;
            result.PlcAutoConnect = kumes.PlcAutoConnect;
            result.PlcCalisiyor = plcBagli;
            result.PlcBagli = plcBagli;
            result.ByteDensity = 0;
            result.PingAverage = 0;
            result.LastPackageLength = 0;
            result.PlcKumesAktif = kumes.Aktif;
            return result;
        }

        //public async Task<Lokasyon?> GetLokasyonByIdWithApi(int lokasyonId)
        //{
        //    try
        //    {
        //        using (var httpClient = new HttpClient())
        //        {
        //            var requestUri = new Uri($"{ApiUrls.SharedAppBaseUrl}/Lokasyon/GetById/{lokasyonId}");

        //            var request = new HttpRequestMessage
        //            {
        //                Method = HttpMethod.Get,
        //                RequestUri = requestUri
        //            };

        //            var response = await httpClient.SendAsync(request);

        //            if (response.IsSuccessStatusCode)
        //            {
        //                var result = await response.Content.ReadAsStringAsync();
        //                var lokasyon = JsonSerializer.Deserialize<Lokasyon?>(result, new JsonSerializerOptions
        //                {
        //                    PropertyNameCaseInsensitive = true
        //                });
        //                return lokasyon;
        //            }
        //            else
        //            {
        //                return null;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public async Task KumesHaberlesmeBilgileriDegistir(int kumesId, string plcIpAdres, bool plcAutoConnect, bool plcKumesAktif)
        {
            var kumes = await _kumesRepository.GetByIdAsync(kumesId);
            if (kumes == null)
            {
                throw new Exception("Kümes bulunamadı");
            }
            if (kumes.PlcIpAdres != plcIpAdres)
            {
                kumes.PlcIpAdresFlag = true;
                kumes.PlcNewIpAdres = plcIpAdres;
                await _kumesRepository.SaveChangesAsync();
                var result = await PlcCheckFlagByKumesIdWithApi(kumesId, kumes.ServisApiUrl);
            }
            else if (kumes.PlcAutoConnect != plcAutoConnect)
            {
                kumes.PlcAutoConnectFlag = true;
                kumes.PlcNewAutoConnect = plcAutoConnect;
                await _kumesRepository.SaveChangesAsync();
                var result = await PlcCheckFlagByKumesIdWithApi(kumesId, kumes.ServisApiUrl);
            }
            else if (kumes.Aktif != plcKumesAktif)
            {
                kumes.PlcAktifFlag = true;
                kumes.PlcNewAktif = plcKumesAktif;
                await _kumesRepository.SaveChangesAsync();
                var result = await PlcCheckFlagByKumesIdWithApi(kumesId, kumes.ServisApiUrl);
            }
        }

        //public async Task<bool> GetKumesAktifByKumesId(int kumesId)
        //{
        //    var kumesAktifMi = (await _kumesRepository.GetAsyncByPredicateWithAsNoTracking(x => x.Id == kumesId)).Aktif.GetValueOrDefault(true);
        //    return kumesAktifMi;
        //}

        //public async Task<Tuple<bool, bool>> GetPlcBilgileriByKumesIdWithApi(int kumesId, string serviceApiUrl)//todo kumestc no cevrilecek
        //{
        //    try
        //    {
        //        using (var httpClient = new HttpClient())
        //        {
        //            httpClient.Timeout = TimeSpan.FromSeconds(5);

        //            var request = new HttpRequestMessage
        //            {
        //                Method = HttpMethod.Get,
        //                RequestUri = new Uri(serviceApiUrl + RahmiyeTesisWorkerUrls.GetPlcBilgileriByKumesId)
        //            };

        //            var uriBuilder = new UriBuilder(request.RequestUri);
        //            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
        //            query["kumesId"] = kumesId.ToString();
        //            uriBuilder.Query = query.ToString();
        //            request.RequestUri = uriBuilder.Uri;

        //            var response = await httpClient.SendAsync(request);

        //            if (response.StatusCode == HttpStatusCode.NoContent)//todo bu blok silinecek galiba
        //            {
        //                return new Tuple<bool, bool>(false, false);
        //            }

        //            if (response.IsSuccessStatusCode)
        //            {
        //                var result = await response.Content.ReadAsStringAsync();
        //                // Sonuç boş veya hatalı ise false döndür
        //                if (string.IsNullOrEmpty(result))
        //                {
        //                    return new Tuple<bool, bool>(false, false);
        //                }

        //                var jsonDocument = JsonDocument.Parse(result);
        //                bool item1 = jsonDocument.RootElement.GetProperty("item1").GetBoolean();
        //                bool item2 = jsonDocument.RootElement.GetProperty("item2").GetBoolean();

        //                return new Tuple<bool, bool>(item1, item2);
        //            }
        //            else
        //            {
        //                return new Tuple<bool, bool>(false, false);
        //            }
        //        }
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        return new Tuple<bool, bool>(false, false);
        //    }
        //    catch (HttpRequestException)
        //    {
        //        return new Tuple<bool, bool>(false, false);
        //    }
        //    catch (Exception)
        //    {
        //        return new Tuple<bool, bool>(false, false);
        //    }
        //}

        //public async Task<List<Tuple<int, bool>>> GetAllPlcConnectedAndReadingByKumesIdWithApi(List<int> kumesIds, string serviceApiUrl)//todo kumestc no cevrilecek
        //{
        //    try
        //    {
        //        using (var httpClient = new HttpClient())
        //        {
        //            httpClient.Timeout = TimeSpan.FromSeconds(5);

        //            var request = new HttpRequestMessage
        //            {
        //                Method = HttpMethod.Get,
        //                RequestUri = new Uri(serviceApiUrl + RahmiyeTesisWorkerUrls.GetAllPlcConnectedAndReading)
        //            };

        //            var uriBuilder = new UriBuilder(request.RequestUri);
        //            string queryString = string.Join("&", kumesIds.Select(id => $"kumesIds={id}"));
        //            //url += "?" + queryString;
        //            //var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
        //            //query["kumesIds"] = string.Join(",", kumesIds);
        //            //uriBuilder.Query = query.ToString();
        //            uriBuilder.Query = queryString;
        //            request.RequestUri = uriBuilder.Uri;

        //            var response = await httpClient.SendAsync(request);

        //            if (response.StatusCode == HttpStatusCode.NoContent)//todo bu blok silinecek galiba
        //            {
        //                return null;
        //            }

        //            if (response.IsSuccessStatusCode)
        //            {
        //                var result = await response.Content.ReadAsStringAsync();
        //                // Sonuç boş veya hatalı ise false döndür
        //                if (string.IsNullOrEmpty(result))
        //                {
        //                    return null;
        //                }

        //                var isPlcConnected = JsonSerializer.Deserialize<List<Tuple<int, bool>>>(result, new JsonSerializerOptions
        //                {
        //                    PropertyNameCaseInsensitive = true
        //                });

        //                return isPlcConnected;
        //            }
        //            else
        //            {
        //                return null;
        //            }
        //        }
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        // Timeout durumu
        //        return null;
        //    }
        //    catch (HttpRequestException)
        //    {
        //        // HTTP isteği ile ilgili herhangi bir hata
        //        return null;
        //    }
        //    catch (Exception)
        //    {
        //        // Diğer genel hatalar
        //        return null;
        //    }
        //}

        //public async Task<bool> GetPlcConnectedAndReadingByKumesIdWithApi(int kumesId, string serviceApiUrl)//todo kumestc no cevrilecek
        //{
        //    try
        //    {
        //        using (var httpClient = new HttpClient())
        //        {
        //            httpClient.Timeout = TimeSpan.FromSeconds(5);

        //            var request = new HttpRequestMessage
        //            {
        //                Method = HttpMethod.Get,
        //                RequestUri = new Uri(serviceApiUrl + RahmiyeTesisWorkerUrls.GetPlcConnectedAndReading)
        //            };

        //            var uriBuilder = new UriBuilder(request.RequestUri);
        //            var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
        //            query["kumesId"] = kumesId.ToString();
        //            uriBuilder.Query = query.ToString();
        //            request.RequestUri = uriBuilder.Uri;

        //            var response = await httpClient.SendAsync(request);

        //            if (response.StatusCode == HttpStatusCode.NoContent)//todo bu blok silinecek galiba
        //            {
        //                return false;
        //            }

        //            if (response.IsSuccessStatusCode)
        //            {
        //                var result = await response.Content.ReadAsStringAsync();
        //                // Sonuç boş veya hatalı ise false döndür
        //                if (string.IsNullOrEmpty(result))
        //                {
        //                    return false;
        //                }

        //                var isPlcConnected = JsonSerializer.Deserialize<bool>(result, new JsonSerializerOptions
        //                {
        //                    PropertyNameCaseInsensitive = true
        //                });

        //                return isPlcConnected;
        //            }
        //            else
        //            {
        //                return false;
        //            }
        //        }
        //    }
        //    catch (TaskCanceledException)
        //    {
        //        // Timeout durumu
        //        return false;
        //    }
        //    catch (HttpRequestException)
        //    {
        //        // HTTP isteği ile ilgili herhangi bir hata
        //        return false;
        //    }
        //    catch (Exception)
        //    {
        //        // Diğer genel hatalar
        //        return false;
        //    }
        //}

        public async Task<bool> PlcCheckFlagByKumesIdWithApi(int kumesId, string serviceApiUrl)//todo kumestc no cevrilecek
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(5);

                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Put,
                        RequestUri = new Uri(serviceApiUrl + RahmiyeTesisWorkerUrls.CheckFlag)
                    };

                    var uriBuilder = new UriBuilder(request.RequestUri);
                    var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                    query["kumesId"] = kumesId.ToString();
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

        public async Task<List<UserKumesYetki?>> GetAllUserKumesYetkiByUserIdWithApi(string userId)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(ApiUrls.SharedAppBaseUrl + "/UserKumesYetki/GetAllUserKumesYetkiByUserId")
                    };

                    // Parametreleri Query String'e eklemek için
                    var uriBuilder = new UriBuilder(request.RequestUri);
                    var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                    query["userId"] = userId;
                    uriBuilder.Query = query.ToString();
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var userKumesYetkis = JsonSerializer.Deserialize<List<UserKumesYetki>>(result, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        return userKumesYetkis ?? new List<UserKumesYetki>();
                    }
                    else
                    {
                        return new List<UserKumesYetki>(); ;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<Lokasyon?>> GetAllLokasyonBySirketIdWithApi(int sirketId)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(ApiUrls.SharedAppBaseUrl + "/Lokasyon/GetAllLokasyonBySirketId")
                    };

                    // Parametreleri Query String'e eklemek için
                    var uriBuilder = new UriBuilder(request.RequestUri);
                    var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                    query["sirketId"] = sirketId.ToString();
                    uriBuilder.Query = query.ToString();
                    request.RequestUri = uriBuilder.Uri;

                    var response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadAsStringAsync();
                        var lokasyon = JsonSerializer.Deserialize<List<Lokasyon?>>(result, new JsonSerializerOptions
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


        //public async Task<List<FarmDataDto>> GetFarmDataDto()
        //{
        //    return new List<FarmDataDto>();
        //    try
        //    {
        //        //rahmiye,merkez,verimgida,kvx ve matli dondurulecek sabit
        //        //Rahmiye
        //        var result = new List<FarmDataDto>();
        //        var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == 12 && x.Silindi != true);
        //        if (!kumesler.Any())
        //            return new List<FarmDataDto>();

        //        var resultRahmiye = new FarmDataDto
        //        {
        //            Id = "1",
        //            Name = "Rahmiye Çiftlik",
        //        };

        //        resultRahmiye.Water = new ResourceStatsDto();
        //        resultRahmiye.Feed = new ResourceStatsDto();
        //        resultRahmiye.Death = new DeathStatsDto();
        //        resultRahmiye.Egg = new EggStatsDto();

        //        var aktifDonemIds = (await _donemRepository.GetAllAsyncByPredicateWithAsNoTracking(x => (x.Aktif ?? false) == true)).Select(x => new { x.Id, x.KumesId }).ToList();
        //        var tumHayvanKayitlar = await _kumesHayvanLogRepository.GetAllAsyncByPredicateWithAsNoTracking(x => aktifDonemIds.Select(y => y.Id).ToList().Contains(x.DonemId ?? 0) && x.Silindi != true);
        //        var canliHayvanKayitlar = tumHayvanKayitlar.Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli).ToList();
        //        var oluHayvanKayitlar = tumHayvanKayitlar.Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Olu).ToList();
        //        resultRahmiye.Pop = canliHayvanKayitlar.Sum(x => x.Adet.GetValueOrDefault(0)) - oluHayvanKayitlar.Sum(x => x.Adet.GetValueOrDefault(0));
        //        resultRahmiye.Death.Today = oluHayvanKayitlar.Where(x => x.KayitZamani.HasValue && x.KayitZamani.Value.Date == DateTime.Now.Date).Sum(x => x.Adet.GetValueOrDefault(0));
        //        resultRahmiye.Death.Yesterday = oluHayvanKayitlar.Where(x => x.KayitZamani.HasValue && x.KayitZamani.Value.Date == DateTime.Now.Date.AddDays(-1)).Sum(x => x.Adet.GetValueOrDefault(0));
        //        //if (resultRahmiye.Pop <= 0)
        //        //    return result;

        //        var son10Kayitlar = await _kumesLogRepository.GetLastTenKumesLogs();
        //        var sonKayitlar = await _kumesLogRepository.GetLastKumesLogs();
        //        resultRahmiye.Water.Today = (double?)sonKayitlar?.Sum(x => x.SuTuketimBugun.GetValueOrDefault(0));



        //        resultRahmiye.Feed.Today = (double?)sonKayitlar?.Sum(x => x.YemTuketimBugun.GetValueOrDefault(0));
        //        decimal? hayvanbasiYemTuketim = 0;
        //        decimal? hayvanbasiSuTuketim = 0;
        //        int yemCounter = 0;
        //        int suCounter = 0;
        //        foreach (var kumesLog in sonKayitlar)
        //        {
        //            var canliKumesHayvanSayisi = canliHayvanKayitlar.Where(x => x.KumesId == kumesLog?.KumesId).Sum(x => x.Adet.GetValueOrDefault(0));
        //            var oluKumesHayvanSayisi = oluHayvanKayitlar.Where(x => x.KumesId == kumesLog?.KumesId).Sum(x => x.Adet.GetValueOrDefault(0));
        //            var kumesHayvanSayisi = canliKumesHayvanSayisi - oluKumesHayvanSayisi;
        //            if (kumesHayvanSayisi > 0)
        //            {
        //                if (kumesLog?.YemTuketimBugun.GetValueOrDefault(0) > 0)
        //                {
        //                    hayvanbasiYemTuketim += kumesLog?.YemTuketimBugun.GetValueOrDefault(0) / kumesHayvanSayisi;
        //                    yemCounter++;
        //                }
        //                if (kumesLog?.SuTuketimBugun.GetValueOrDefault(0) > 0)
        //                {
        //                    hayvanbasiSuTuketim += kumesLog?.SuTuketimBugun.GetValueOrDefault(0) / kumesHayvanSayisi;
        //                    suCounter++;
        //                }
        //            }
        //        }
        //        if (yemCounter > 0)
        //            resultRahmiye.Feed.TodayPer = (double?)(hayvanbasiYemTuketim * 1000000) / yemCounter;

        //        if (suCounter > 0)
        //            resultRahmiye.Water.TodayPer = (double?)((hayvanbasiSuTuketim * 1000000) / suCounter);

        //        resultRahmiye.Egg.Today = sonKayitlar?.Sum(x => x.YumurtaAdet.GetValueOrDefault(0));
        //        resultRahmiye.Egg.TodayPer = resultRahmiye.Egg.Today.GetValueOrDefault(0) / resultRahmiye.Pop;

        //        result.Add(resultRahmiye);
        //        return result;
        //    }
        //    catch (Exception)
        //    {
        //        return new List<FarmDataDto>();
        //    }
        //}

        public async Task<List<FarmDataDto>> GetFarmDataDto()
        {
            var lokasyonBilgileri = new Dictionary<int, (string Id, string Name)>
            {
                { 12, ("1", "Rahmiye Çiftlik") },
                { 47, ("3", "Verim Gıda") }
            };

            return await GetFarmDataDtoByKumesTip(KumesTip.TicariYumurta, lokasyonBilgileri);
        }

        public async Task<List<FarmDataDto>> GetDamizlikFarmDataDto()
        {
            var lokasyonBilgileri = new Dictionary<int, (string Id, string Name)>
            {
                { 32, ("1", "Kayışlar Çiftlik-1") }
            };

            return await GetFarmDataDtoByKumesTip(KumesTip.DamizlikUretim, lokasyonBilgileri);
        }

        private async Task<List<FarmDataDto>> GetFarmDataDtoByKumesTip(int kumesTip, Dictionary<int, (string Id, string Name)> lokasyonBilgileri)
        {
            try
            {
                var result = new List<FarmDataDto>();

                var tumKumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(
                    x => x.KumesTip == kumesTip && x.Silindi != true);

                if (!tumKumesler.Any())
                    return new List<FarmDataDto>();

                var lokasyonGruplari = tumKumesler.GroupBy(x => x.LokasyonId).ToList();

                // ✅ Scope'u dışarı aldık
                var today = DateTime.Today;
                var startDate = today.AddDays(-9);
                var endExclusive = today.AddDays(1);
                var dates = Enumerable.Range(0, 10).Select(i => startDate.AddDays(i)).ToList();

                var globalFarmPopByDate = new Dictionary<DateTime, long>();

                foreach (var grup in lokasyonGruplari)
                {
                    var lokasyonId = grup.Key;
                    var lokasyonBilgisi = lokasyonBilgileri.TryGetValue(lokasyonId, out var bilgi)
                        ? bilgi
                        : (Id: lokasyonId.ToString(), Name: $"Lokasyon {lokasyonId}");
                    var kumesIds = grup.Select(x => x.Id).ToList();

                    var aktifDonemIds = (await _donemRepository.GetAllAsyncByPredicateWithAsNoTracking(
                        x => (x.Aktif ?? false) == true && kumesIds.Contains(x.KumesId.GetValueOrDefault(0))))
                        .Select(x => x.Id).ToList();

                    var tumHayvanKayitlar = await _kumesHayvanLogRepository
                        .GetAllAsyncByPredicateWithAsNoTracking(x =>
                            x.Silindi != true &&
                            aktifDonemIds.Contains(x.DonemId ?? 0) &&
                            kumesIds.Contains(x.KumesId ?? 0));

                    var canliHayvanKayitlar = tumHayvanKayitlar
                        .Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli).ToList();

                    var oluHayvanKayitlar = tumHayvanKayitlar
                        .Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Olu).ToList();

                    long farmPop =
                        canliHayvanKayitlar.Sum(x => x.Adet.GetValueOrDefault(0)) -
                        oluHayvanKayitlar.Sum(x => x.Adet.GetValueOrDefault(0));

                    var deathByDate = oluHayvanKayitlar
                        .Where(x => x.KayitZamani.HasValue
                                 && x.KayitZamani.Value.Date >= startDate
                                 && x.KayitZamani.Value.Date < endExclusive)
                        .GroupBy(x => x.KayitZamani!.Value.Date)
                        .ToDictionary(g => g.Key, g => g.Sum(r => r.Adet.GetValueOrDefault(0)));

                    var deathTrend = dates
                        .Select(d => (double)(deathByDate.TryGetValue(d, out var v) ? v : 0))
                        .ToList();

                    var addByKumesDate = tumHayvanKayitlar
                        .Where(x => x.KayitZamani.HasValue
                                 && x.KayitZamani.Value.Date < endExclusive
                                 && x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli)
                        .GroupBy(x => new { KumesId = x.KumesId!.Value, Date = x.KayitZamani!.Value.Date })
                        .ToDictionary(g => (g.Key.KumesId, g.Key.Date), g => g.Sum(r => r.Adet.GetValueOrDefault(0)));

                    var deathByKumesDate = tumHayvanKayitlar
                        .Where(x => x.KayitZamani.HasValue
                                 && x.KayitZamani.Value.Date < endExclusive
                                 && x.SaglikDurumu == KumesHayvanSaglikDurum.Olu)
                        .GroupBy(x => new { KumesId = x.KumesId!.Value, Date = x.KayitZamani!.Value.Date })
                        .ToDictionary(g => (g.Key.KumesId, g.Key.Date), g => g.Sum(r => r.Adet.GetValueOrDefault(0)));

                    long BasePopBefore(DateTime day, int kumesId)
                    {
                        var d = day.Date;
                        long adds = addByKumesDate.Where(kv => kv.Key.Item1 == kumesId && kv.Key.Item2 < d).Sum(kv => (long)kv.Value);
                        long deaths = deathByKumesDate.Where(kv => kv.Key.Item1 == kumesId && kv.Key.Item2 < d).Sum(kv => (long)kv.Value);
                        var pop = adds - deaths;
                        return pop < 0 ? 0 : pop;
                    }

                    var popByKumesByDate = new Dictionary<int, Dictionary<DateTime, long>>(kumesIds.Count);

                    foreach (var kumesId in kumesIds)
                    {
                        var dayPop = new Dictionary<DateTime, long>(10);
                        long pop = BasePopBefore(startDate, kumesId);

                        foreach (var d in dates)
                        {
                            addByKumesDate.TryGetValue((kumesId, d.Date), out var addToday);
                            deathByKumesDate.TryGetValue((kumesId, d.Date), out var deathToday);
                            pop += (long)addToday - (long)deathToday;
                            if (pop < 0) pop = 0;
                            dayPop[d.Date] = pop;
                        }

                        popByKumesByDate[kumesId] = dayPop;
                    }

                    var farmPopByDate = dates.ToDictionary(
                        d => d.Date,
                        d => popByKumesByDate.Sum(kv => kv.Value.TryGetValue(d.Date, out var p) ? p : 0L));

                    long GetKumesPopForDay(int kumesId, DateTime day) =>
                        popByKumesByDate.TryGetValue(kumesId, out var map) && map.TryGetValue(day.Date, out var p) ? p : 0L;

                    var son10Kayitlar = await _kumesLogRepository.GetLastTenKumesLogs();

                    var logsFarm10 = (son10Kayitlar ?? new List<KumesLogYeni>())
                        .Where(x => kumesIds.Contains(x.KumesId.GetValueOrDefault(0))).ToList();

                    var logsByDate = logsFarm10
                        .GroupBy(x => x.KayitZamani.Value.Date)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    (double total, double per) CalcResourceForDay(DateTime day, Func<KumesLogYeni, decimal?> selector)
                    {
                        if (!logsByDate.TryGetValue(day.Date, out var dayLogs) || dayLogs.Count == 0)
                            return (0d, 0d);

                        decimal totalVal = 0m;

                        foreach (var log in dayLogs)
                        {
                            var val = selector(log).GetValueOrDefault(0m);
                            if (val <= 0) continue;
                            totalVal += val;
                        }

                        // ✅ totalPop olarak o günün tüm farm pop'unu kullan
                        long totalPop = farmPopByDate.TryGetValue(day.Date, out var fp) ? fp : 0L;

                        double totalDay = (double)totalVal;
                        double perDay = totalPop > 0 ? (double)((totalVal / totalPop) * 1_000_000m) : 0d;
                        return (totalDay, perDay);
                    }

                    double EggForDay(DateTime day)
                    {
                        if (!logsByDate.TryGetValue(day.Date, out var dayLogs) || dayLogs.Count == 0)
                            return 0d;
                        return dayLogs.Sum(x => (double)x.YumurtaAdet.GetValueOrDefault(0));
                    }

                    var waterTrendTotal = new List<double>(10);
                    var waterTrendPer = new List<double>(10);
                    var feedTrendTotal = new List<double>(10);
                    var feedTrendPer = new List<double>(10);
                    var eggTrend = new List<double>(10);

                    foreach (var d in dates)
                    {
                        var (wTotal, wPer) = CalcResourceForDay(d, x => x.SuTuketimBugun);
                        waterTrendTotal.Add(wTotal);
                        waterTrendPer.Add(wPer);

                        var (fTotal, fPer) = CalcResourceForDay(d, x => x.YemTuketimBugun);
                        feedTrendTotal.Add(fTotal);
                        feedTrendPer.Add(fPer);

                        eggTrend.Add(EggForDay(d));
                    }

                    var dto = new FarmDataDto
                    {
                        Id = lokasyonBilgisi.Id,
                        Name = lokasyonBilgisi.Name,
                        Pop = farmPop,
                        Stock = son10Kayitlar?
                            .Where(x => x.KayitZamani.Value.Date == DateTime.Now.Date && kumesIds.Contains(x.KumesId.GetValueOrDefault(0)))
                            .Sum(x => x.Silo1.GetValueOrDefault(0) + x.Silo2.GetValueOrDefault(0))
                            .ToString(),

                        Death = new DeathStatsDto
                        {
                            Trend = deathTrend,
                            Today = (int?)deathTrend.Last(),
                            Yesterday = deathTrend.Count >= 2 ? (int?)deathTrend[^2] : 0,
                            Total10 = (int?)deathTrend.Sum()
                        },
                        Water = new ResourceStatsDto
                        {
                            TrendTotal = waterTrendTotal,
                            TrendPer = waterTrendPer,
                            Today = waterTrendTotal.LastOrDefault(),
                            TodayPer = waterTrendPer.LastOrDefault(),
                            Total10 = waterTrendTotal.Sum(),
                            Total10Per = waterTrendPer.Count > 0 ? waterTrendPer.Average() : 0
                        },
                        Feed = new ResourceStatsDto
                        {
                            TrendTotal = feedTrendTotal,
                            TrendPer = feedTrendPer,
                            Today = feedTrendTotal.LastOrDefault(),
                            TodayPer = feedTrendPer.LastOrDefault(),
                            Total10 = feedTrendTotal.Sum(),
                            Total10Per = feedTrendPer.Count > 0 ? feedTrendPer.Average() : 0
                        },
                        Egg = new EggStatsDto
                        {
                            Trend = eggTrend,
                            Today = (int?)eggTrend.LastOrDefault(),
                            TodayPer = farmPop > 0 ? eggTrend.LastOrDefault() / farmPop : 0,
                            Total10 = (int?)eggTrend.Sum(),
                            Total10Per = farmPop > 0 ? eggTrend.Sum() / (farmPop * 10d) : 0
                        },
                    };

                    // ✅ globalFarmPopByDate'i biriktir
                    foreach (var kvp in farmPopByDate)
                    {
                        if (globalFarmPopByDate.ContainsKey(kvp.Key))
                            globalFarmPopByDate[kvp.Key] += kvp.Value;
                        else
                            globalFarmPopByDate[kvp.Key] = kvp.Value;
                    }

                    result.Add(dto);
                }

                // =========================
                // 7) MATLI AGGREGATE
                // =========================
                if (result.Any())
                {
                    var matliPop = result.Sum(r => r.Pop);
                    var matliStock = result.Sum(r => decimal.TryParse(r.Stock, out var s) ? s : 0m).ToString("0.##");

                    var matliDeathTrend = new List<double>(10);
                    var matliWaterTrendTotal = new List<double>(10);
                    var matliFeedTrendTotal = new List<double>(10);
                    var matliEggTrend = new List<double>(10);

                    // ✅ Önce Total trendleri doldur
                    for (int i = 0; i < 10; i++)
                    {
                        matliDeathTrend.Add(result.Sum(r => r.Death.Trend?.Count > i ? r.Death.Trend[i] : 0));
                        matliWaterTrendTotal.Add(result.Sum(r => r.Water.TrendTotal?.Count > i ? r.Water.TrendTotal[i] : 0));
                        matliFeedTrendTotal.Add(result.Sum(r => r.Feed.TrendTotal?.Count > i ? r.Feed.TrendTotal[i] : 0));
                        matliEggTrend.Add(result.Sum(r => r.Egg.Trend?.Count > i ? r.Egg.Trend[i] : 0));
                    }

                    // ✅ Per trendleri globalFarmPopByDate'den günlük pop ile hesapla
                    var matliWaterTrendPer = new List<double>(10);
                    var matliFeedTrendPer = new List<double>(10);

                    for (int i = 0; i < 10; i++)
                    {
                        var day = startDate.AddDays(i);
                        double dayPop = globalFarmPopByDate.TryGetValue(day.Date, out var dp) ? dp : 0;

                        matliWaterTrendPer.Add(dayPop > 0 ? (matliWaterTrendTotal[i] / dayPop) * 1_000_000d : 0d);
                        matliFeedTrendPer.Add(dayPop > 0 ? (matliFeedTrendTotal[i] / dayPop) * 1_000_000d : 0d);
                    }

                    // ✅ Today için bugünün pop'u
                    double todayPop = globalFarmPopByDate.TryGetValue(today, out var tp) ? tp : 0;

                    var matliDto = new FarmDataDto
                    {
                        Id = "0",
                        Name = "Matlı",
                        Pop = matliPop,
                        Stock = matliStock,
                        Death = new DeathStatsDto
                        {
                            Trend = matliDeathTrend,
                            Today = result.Sum(r => r.Death.Today ?? 0),
                            Yesterday = result.Sum(r => r.Death.Yesterday ?? 0),
                            Total10 = result.Sum(r => r.Death.Total10 ?? 0)
                        },
                        Water = new ResourceStatsDto
                        {
                            TrendTotal = matliWaterTrendTotal,
                            TrendPer = matliWaterTrendPer,
                            Today = matliWaterTrendTotal.LastOrDefault(),
                            TodayPer = todayPop > 0 ? matliWaterTrendPer.LastOrDefault() : 0d,
                            Total10 = matliWaterTrendTotal.Sum(),
                            Total10Per = matliWaterTrendPer.Count > 0 ? matliWaterTrendPer.Average() : 0d
                        },
                        Feed = new ResourceStatsDto
                        {
                            TrendTotal = matliFeedTrendTotal,
                            TrendPer = matliFeedTrendPer,
                            Today = matliFeedTrendTotal.LastOrDefault(),
                            TodayPer = todayPop > 0 ? matliFeedTrendPer.LastOrDefault() : 0d,
                            Total10 = matliFeedTrendTotal.Sum(),
                            Total10Per = matliFeedTrendPer.Count > 0 ? matliFeedTrendPer.Average() : 0d
                        },
                        Egg = new EggStatsDto
                        {
                            Trend = matliEggTrend,
                            Today = (int?)matliEggTrend.LastOrDefault(),
                            TodayPer = todayPop > 0 ? matliEggTrend.LastOrDefault() / todayPop : 0d,
                            Total10 = (int?)matliEggTrend.Sum(),
                            Total10Per = matliPop > 0 ? matliEggTrend.Sum() / (matliPop * 10d) : 0d
                        }
                    };

                    result.Insert(0, matliDto);
                }

                return result.OrderBy(x => x.Id).ToList();
            }
            catch
            {
                return new List<FarmDataDto>();
            }
        }

        public async Task<List<AnimalCountRangeItem>> GetTicariYumurtaKumesleriHayvanSayisi(int[] nodeIds, DateTime startDate, DateTime endDate, CancellationToken ct)
        {
            // endExclusive: dahil olan endDate için +1 gün
            var endExclusive = endDate.Date.AddDays(1);

            var nodeLokasyonEslemeleri = new Dictionary<int, int>
            {
                { 258, 12 },
                { 259, 12 },
                { 267, 47 },
                { 287, 47 }
            };

            // Tanımsız node'larda mevcut Rahmiye davranışını koru.
            var nodeLokasyonlari = nodeIds
                .Distinct()
                .ToDictionary(
                    nodeId => nodeId,
                    nodeId => nodeLokasyonEslemeleri.TryGetValue(nodeId, out var lokasyonId) ? lokasyonId : 12);

            var lokasyonIds = nodeLokasyonlari.Values.Distinct().ToList();

            // 1) İstenen enerji node'larına bağlı ticari yumurta kümesleri
            var kumesler = await _kumesRepository
                .GetAllAsyncByPredicateWithAsNoTracking(x =>
                    lokasyonIds.Contains(x.LokasyonId) &&
                    x.KumesTip == KumesTip.TicariYumurta &&
                    x.Silindi != true);

            if (!kumesler.Any()) return new List<AnimalCountRangeItem>();

            var kumesIds = kumesler.Select(x => x.Id).ToList();

            // 2) aktif dönemler
            var aktifDonemIds = (await _donemRepository
                    .GetAllAsyncByPredicateWithAsNoTracking(x => (x.Aktif ?? false) == true && kumesIds.Contains(x.KumesId.GetValueOrDefault(0))))
                .Select(x => x.Id)
                .ToList();

            // ✅ 3) hayvan loglarını KESİN filtrele (performans)
            // base pop için startDate öncesi kayıtlar da lazım, ama endExclusive sonrası gereksiz
            var tumHayvanKayitlar = await _kumesHayvanLogRepository
                .GetAllAsyncByPredicateWithAsNoTracking(x =>
                    x.Silindi != true &&
                    aktifDonemIds.Contains(x.DonemId ?? 0) &&
                    kumesIds.Contains(x.KumesId ?? 0) &&
                    x.KayitZamani.HasValue &&
                    x.KayitZamani.Value < endExclusive);

            // 4) Gün listesi (startDate..endDate dahil)
            var dayCount = (endDate.Date - startDate.Date).Days + 1;
            var dates = Enumerable.Range(0, dayCount).Select(i => startDate.Date.AddDays(i)).ToList();

            // 5) add/death günlük dict
            var addByKumesDate = tumHayvanKayitlar
                .Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Saglikli)
                .GroupBy(x => new { KumesId = x.KumesId!.Value, Date = x.KayitZamani!.Value.Date })
                .ToDictionary(g => (g.Key.KumesId, g.Key.Date), g => g.Sum(r => r.Adet.GetValueOrDefault(0)));

            var deathByKumesDate = tumHayvanKayitlar
                .Where(x => x.SaglikDurumu == KumesHayvanSaglikDurum.Olu)
                .GroupBy(x => new { KumesId = x.KumesId!.Value, Date = x.KayitZamani!.Value.Date })
                .ToDictionary(g => (g.Key.KumesId, g.Key.Date), g => g.Sum(r => r.Adet.GetValueOrDefault(0)));

            long BasePopBefore(DateTime day, int kumesId)
            {
                var d = day.Date;
                long adds = addByKumesDate.Where(kv => kv.Key.Item1 == kumesId && kv.Key.Item2 < d).Sum(kv => (long)kv.Value);
                long deaths = deathByKumesDate.Where(kv => kv.Key.Item1 == kumesId && kv.Key.Item2 < d).Sum(kv => (long)kv.Value);
                var pop = adds - deaths;
                return pop < 0 ? 0 : pop;
            }

            // 6) Lokasyon bazında günlük farm pop üret
            var farmPopByLokasyonByDate = lokasyonIds.ToDictionary(
                lokasyonId => lokasyonId,
                _ => dates.ToDictionary(d => d.Date, _ => 0L));

            foreach (var kumes in kumesler)
            {
                long pop = BasePopBefore(startDate, kumes.Id);

                foreach (var d in dates)
                {
                    addByKumesDate.TryGetValue((kumes.Id, d.Date), out var addToday);
                    deathByKumesDate.TryGetValue((kumes.Id, d.Date), out var deathToday);

                    pop += (long)addToday - (long)deathToday;
                    if (pop < 0) pop = 0;

                    farmPopByLokasyonByDate[kumes.LokasyonId][d.Date] += pop;
                }
            }

            // ✅ 7) ÇIKTI: nodeId başına her gün item üret
            var result = new List<AnimalCountRangeItem>(nodeIds.Length * dates.Count);

            foreach (var nodeId in nodeIds.Distinct())
            {
                var lokasyonId = nodeLokasyonlari[nodeId];
                var farmPopByDate = farmPopByLokasyonByDate[lokasyonId];

                foreach (var d in dates)
                {
                    result.Add(new AnimalCountRangeItem
                    {
                        NodeId = nodeId,
                        Date = d.Date,
                        Count = farmPopByDate.TryGetValue(d.Date, out var p) ? p : 0L
                    });
                }
            }

            return result;
        }

        public async Task<TesisYemRasyonAyarModel?> GetTesisYemRasyonAyarlariByLokasyonId(int lokasyonId, CancellationToken cancellationToken)
        {
            return await _kumesRepository.GetTesisYemRasyonAyarlariByLokasyonId(lokasyonId, cancellationToken);
        }

        public async Task<bool> SaveTesisYemRasyonAyarlariByLokasyonId(TesisYemRasyonAyarModel tesisYemRasyonAyarModel, CancellationToken cancellationToken)
        {
            return await _kumesRepository.SaveTesisYemRasyonAyarlariByLokasyonId(tesisYemRasyonAyarModel, cancellationToken);
        }

        public async Task<List<KumesViewModel>> GetKarsilastirilabilirKumeslerByKumesId(int kumesId)
        {
            var kumes = await _kumesRepository.GetByIdAsync(kumesId);
            var karsilastirilabilirKumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == kumes.LokasyonId && x.Silindi != true && x.Aktif != false);
            return _mapper.Map<List<KumesViewModel>>(karsilastirilabilirKumesler);
        }

        public async Task<List<PlcBaglantiKesintiGunlukRaporModel>> GetPlcBaglantiKesintiRaporuByLokasyonId(int lokasyonId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            var kumesler = await _kumesRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.LokasyonId == lokasyonId && x.Silindi != true);
            var kumesIds = kumesler.Select(x => x.Id).ToList();
            var gunlukVeri = await _kumesRepository.GetPlcBaglantiKesintiGunlukRaporuByKumesIds(kumesIds, baslangicZamani, bitisZamani);

            foreach (var satir in gunlukVeri)
            {
                satir.KumesName = kumesler.FirstOrDefault(x => x.Id == satir.KumesId)?.Name;
            }

            return gunlukVeri
                .OrderBy(x => x.KumesName)
                .ThenByDescending(x => x.Tarih)
                .ToList();
        }

        public async Task<List<PlcBaglantiKesintiLog>> GetPlcBaglantiKesintiDetayByKumesId(int kumesId, DateTime baslangicZamani, DateTime bitisZamani)
        {
            return await _kumesRepository.GetPlcBaglantiKesintiDetayByKumesId(kumesId, baslangicZamani, bitisZamani);
        }
    }
}
