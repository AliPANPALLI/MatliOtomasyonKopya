using EnergyMonitoring.Infrastructure;
using PoultryHouseScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using SharedApp.Domain.Models.ReportModels;
using SharedApp.Infrastructure;
using SharedApp.Infrastructure.Sender;
using SharedKernel;
using WaterLineScada.Infrastructure;
using static SharedKernel.ApplicationConstants;
using System.Text.Json;

namespace AlarmWorker
{
    public class AlarmBilgilendirmeService : BackgroundService
    {
        //private readonly ILogger<AlarmBilgilendirmeService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly EmailSender _emailSender;

        public AlarmBilgilendirmeService(/*ILogger<AlarmBilgilendirmeService> logger, */IServiceProvider serviceProvider, EmailSender emailSender)
        {
            //_logger = logger;
            _serviceProvider = serviceProvider;
            _emailSender = emailSender;
            _emailSender = emailSender;
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {     //_logger.LogInformation("AlarmBilgilendirmeService working: {time}", DateTimeOffset.Now);
                await UpdateServisDurumAsync("AlarmBilgilendirmeService", 2, "Servis başlatılıyor");
                await base.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await UpdateServisDurumAsync("AlarmBilgilendirmeService", 0, ex.Message);

            }
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //_logger.LogInformation("AlarmBilgilendirmeService running at: {time}", DateTimeOffset.Now);
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var kumesDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                        //Kümes aktif değilse alarm gelmesin diye kontrol eklemek gerekiyormuş
                        var aktifKumesler = await kumesDbContext.Kumes.Where(x => x.Aktif != false && x.Silindi != true).ToListAsync(stoppingToken);
                        var aktifKumesAlarmlar = await kumesDbContext.AlarmLog.Where(x => aktifKumesler.Select(k => k.Id).ToList().Contains(x.KumesId.GetValueOrDefault(0)) && x.Aktif != false && x.Silindi != true).ToListAsync(stoppingToken);
                        await KumesScadaAlarmGonderAsync(aktifKumesAlarmlar);

                        var aktifYTKAlarmlar = await kumesDbContext.AlarmLog.Where(x => x.KonveyorId.GetValueOrDefault(0) > 0 && x.Aktif != false && x.Silindi != true).ToListAsync(stoppingToken);
                        await YTKAlarmGonderAsync(aktifYTKAlarmlar);

                        var suHatDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                        var aktifSuIzlemeAlarmlar = await suHatDbContext.AlarmLog.Where(x => x.Aktif != false && x.Silindi != true).ToListAsync(stoppingToken);
                        await SuHatScadaAlarmGonderAsync(aktifSuIzlemeAlarmlar);

                        var enerjiIzlemeDbContext = scope.ServiceProvider.GetRequiredService<EnerjiIzlemeDbContext>();
                        var aktifEnerjiIzlemeAlarmlar = await enerjiIzlemeDbContext.AlarmLog.Where(x => x.Aktif != false && x.Silindi != true).ToListAsync(stoppingToken);
                        await EnerjiIzlemeAlarmGonderAsync(aktifEnerjiIzlemeAlarmlar);

                        var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
                        var aktifSharedAlarmlar = await sharedDbContext.AlarmLog.Where(x => x.Aktif != false && x.Silindi != true).ToListAsync(stoppingToken);
                        if (aktifSharedAlarmlar.Count > 0)
                        {
                            await SharedAlarmGonderAsync(aktifSharedAlarmlar);
                        }

                        await BasarisizAramalaraSmsGonderAsync(stoppingToken);//todo:cancellationtoken koy
                        await SuresiDolanAlarmlariPasifeCekAsync(stoppingToken);
                        await UpdateServisDurumAsync("AlarmBilgilendirmeService", 1, "Servis çalışıyor", true);
                    }
                }
                catch (Exception ex)
                {
                    await UpdateServisDurumAsync("AlarmBilgilendirmeService", 0, ex.Message);
                }
                finally
                {
                    //_logger.LogInformation("AlarmBilgilendirmeService completed at: {time}", DateTimeOffset.Now);
                    await Task.Delay(10000, stoppingToken);
                }
            }
        }

        public async Task KumesScadaAlarmGonderAsync(List<PoultryHouseScada.Domain.Models.AlarmLog> kumesAlarmLogs)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

                var alarmIds = kumesAlarmLogs.Select(x => x.AlarmId.GetValueOrDefault()).Distinct().ToList();

                var alarmGrupEslemeleri = await sharedDbContext.AlarmGrupAlarmEsleme
                    .Where(x => alarmIds.Contains(x.AlarmId.GetValueOrDefault()) && x.Silindi != true)
                    .ToListAsync();

                var alarmlar = await sharedDbContext.vwAlarmlar
                    .Where(x => alarmIds.Contains(x.Id) && x.Silindi != true)
                    .ToDictionaryAsync(x => x.Id);

                var alarmGrupIds = alarmGrupEslemeleri.Select(x => x.AlarmGrupId).Distinct().ToList();

                var kullaniciEslemelerEposta = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.EPosta)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                var kullaniciEslemelerSesliMesaj = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                     .Where(x =>
                         alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                         (x.SeviyeAktif ?? false) &&
                         (x.Aktif ?? false) &&
                         (x.Silindi ?? false) != true &&
                         x.BildirimYontemi == BildirimYontemi.SesliMesaj)
                     .OrderBy(x => x.SeviyeNo)
                     .ToListAsync();

                // ======================
                // 1) EPosta Bildirimleri
                // ======================
                // Kullanıcı bazlı alarm listesi
                var kullaniciAlarmMap = new Dictionary<string, (vwAlarmKullaniciEslemeForNotification kullanici, List<(vwAlarmlar, int, DateTime)> alarmlar)>();

                foreach (var log in kumesAlarmLogs)
                {
                    if (!alarmlar.TryGetValue(log.AlarmId ?? 0, out var alarm))
                        continue;

                    var ilgiliAlarmGrupIdList = alarmGrupEslemeleri
                        .Where(x => x.AlarmId == alarm.Id)
                        .Select(x => x.AlarmGrupId)
                        .Distinct()
                        .ToList();

                    var eslesenKullanicilar = kullaniciEslemelerEposta
                        .Where(x => ilgiliAlarmGrupIdList.Contains(x.AlarmGrupId ?? 0))
                        .GroupBy(x => new { x.UserId, x.SeviyeNo })
                        .Select(g => g.First())
                        .OrderBy(x => x.SeviyeNo)
                        .ToList();

                    var seviyelereGoreKullaniciGruplari = eslesenKullanicilar
                        .GroupBy(x => x.SeviyeNo)
                        .OrderBy(x => x.Key);

                    foreach (var seviyeGrubu in seviyelereGoreKullaniciGruplari)
                    {
                        var seviyeNo = seviyeGrubu.Key.GetValueOrDefault();
                        var kullanicilar = seviyeGrubu.ToList();
                        var takipTipi = BildirimYontemi.EPosta;
                        var kaynakTuru = AlarmKaynakTuru.Kumes;

                        foreach (var kullanici in kullanicilar)
                        {
                            // Mail zaten gönderildi mi kontrolü
                            var mailZatenGonderildiMi = await (from m in sharedDbContext.MailGonderimTakip.AsNoTracking()
                                                               join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                                                                 on m.Id equals j.TakipId
                                                               where m.UserId == kullanici.UserId
                                                                  && m.SeviyeNo == seviyeNo
                                                                  && j.TakipTipi == takipTipi
                                                                  && j.AlarmKaynakTuru == kaynakTuru
                                                                  && j.AlarmLogId == log.Id
                                                                  && ((m.GonderimBasarili ?? false) == true || !(m.KalanDenemeSayisi == 0 && (m.GonderimBasarili ?? false) == false))
                                                               select 1).AnyAsync();

                            if (mailZatenGonderildiMi)
                                continue;

                            // Zaman kontrolü
                            var bildirimZamani = log.KayitZamani?.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi) ?? DateTime.Now;
                            if (DateTime.Now >= bildirimZamani)
                            {
                                // Kullanıcıyı anahtar olarak kullan (UserId bazlı)
                                if (!kullaniciAlarmMap.ContainsKey(kullanici.UserId))
                                {
                                    kullaniciAlarmMap[kullanici.UserId] = (kullanici, new List<(vwAlarmlar, int, DateTime)>());
                                }

                                kullaniciAlarmMap[kullanici.UserId].alarmlar.Add((alarm, log.Id, log.KayitZamani ?? DateTime.Now));
                            }
                        }
                    }
                }

                // Tüm kullanıcılar için e-postaları gönder
                foreach (var (_, (kullanici, alarmlarList)) in kullaniciAlarmMap)
                {
                    await BildirimGonderAsync(kullanici, alarmlarList, AlarmKaynakTuru.Kumes);
                }

                // ==========================
                // 2) Sesli Mesaj Bildirimleri (AlarmGrup bazlı)
                // ==========================
                foreach (var kullaniciSesliMesaj in kullaniciEslemelerSesliMesaj)
                {
                    // 1) Bu kullanıcıya ait alarm grubu
                    var ilgiliAlarmGrupId = kullaniciSesliMesaj.AlarmGrupId ?? 0;

                    // 2) Bu gruba bağlı alarmId’ler
                    var grupAlarmIds = alarmGrupEslemeleri
                        .Where(x => x.AlarmGrupId == ilgiliAlarmGrupId)
                        .Select(x => x.AlarmId.GetValueOrDefault())
                        .ToList();

                    // 3) Loglardan bu gruba girenler
                    var ilgiliAlarmLoglar = kumesAlarmLogs
                        .Where(x => x.AlarmId != null && grupAlarmIds.Contains(x.AlarmId.Value))
                        .ToList();

                    if (!ilgiliAlarmLoglar.Any())
                        continue;

                    var gonderilebilirLoglar = ilgiliAlarmLoglar.Where(log => { var bildirimZamani = log.KayitZamani?.Add(kullaniciSesliMesaj.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSesliMesaj.BildirimSuresi) ?? DateTime.Now; return DateTime.Now >= bildirimZamani; }).ToList();

                    if (!gonderilebilirLoglar.Any())
                        continue;

                    // 4) Sesli mesaj içeriğini oluştur
                    var mesajlar = SesliMesajMetinleriOlustur(gonderilebilirLoglar, alarmlar);

                    foreach (var mesaj in mesajlar)
                    {
                        var mesajAlarmLogIds = mesaj.AlarmLogIds;
                        if (mesajAlarmLogIds == null || mesajAlarmLogIds.Count == 0)
                            continue;

                        // daha önce gönderilmiş mi kontrolü 
                        var alarmLogIdList = mesajAlarmLogIds.Distinct().ToList();

                        var sonGonderim = await (from t in sharedDbContext.MesajGonderimTakip.AsNoTracking()
                                                 join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                                                     on t.Id equals j.TakipId
                                                 where t.UserId == kullaniciSesliMesaj.UserId
                                                    && t.SeviyeNo == (kullaniciSesliMesaj.SeviyeNo ?? 0)
                                                    && j.TakipTipi == BildirimYontemi.SesliMesaj
                                                    && j.AlarmKaynakTuru == AlarmKaynakTuru.Kumes
                                                    && alarmLogIdList.Contains(j.AlarmLogId)
                                                 orderby t.KayitZamani descending
                                                 select t
                                                ).FirstOrDefaultAsync();

                        bool gonderilsinMi = false;
                        if (sonGonderim == null)
                        {
                            gonderilsinMi = true;
                        }
                        else if ((kullaniciSesliMesaj.TekrarlansinMi ?? false) &&
                                 kullaniciSesliMesaj.TekrarlamaSuresi != null &&
                                 DateTime.Now - sonGonderim.KayitZamani >= kullaniciSesliMesaj.TekrarlamaSuresi)
                        {
                            gonderilsinMi = true;
                        }

                        // 6) Gönderim yapılacaksa
                        if (gonderilsinMi && !string.IsNullOrEmpty(mesaj.Mesaj))
                        {
                            await using var tx = await sharedDbContext.Database.BeginTransactionAsync();
                            try
                            {
                                var takip = new MesajGonderimTakip
                                {
                                    KayitZamani = DateTime.Now,
                                    SmsIcerik = mesaj.Mesaj,
                                    TelefonNo = kullaniciSesliMesaj.PhoneNumber,
                                    UserId = kullaniciSesliMesaj.UserId,
                                    SeviyeNo = kullaniciSesliMesaj.SeviyeNo ?? 0,
                                    KalanDenemeSayisi = 10,
                                    SesDosyaYolu = null,
                                    AramaSure = null,
                                    CevaplandiMi = null,
                                    GonderimBasarili = false,
                                    //KumesAlarmLogIds = string.Join(",", mesaj.AlarmLogIds)
                                };
                                sharedDbContext.MesajGonderimTakip.Add(takip);
                                await sharedDbContext.SaveChangesAsync();

                                var takipId = takip.Id;
                                var takipTipi = BildirimYontemi.SesliMesaj;
                                var kaynakTuru = AlarmKaynakTuru.Kumes;

                                var joinRows = mesaj.AlarmLogIds.Distinct().Select(alarmLogId => new MesajGonderimTakipAlarmLog
                                {
                                    TakipId = takipId,
                                    TakipTipi = takipTipi,
                                    AlarmKaynakTuru = kaynakTuru,
                                    AlarmLogId = alarmLogId
                                }).ToList();

                                sharedDbContext.MesajGonderimTakipAlarmLog.AddRange(joinRows);
                                await sharedDbContext.SaveChangesAsync();
                                await tx.CommitAsync();
                            }
                            catch
                            {
                                await tx.RollbackAsync();
                            }
                        }
                    }
                }

                // ==========================
                // 3) SMS Bildirimleri (AlarmGrup bazlı)
                // ==========================
                var kullaniciEslemelerSms = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.SMS)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                foreach (var kullaniciSms in kullaniciEslemelerSms)
                {
                    var ilgiliAlarmGrupId = kullaniciSms.AlarmGrupId ?? 0;

                    var grupAlarmIds = alarmGrupEslemeleri
                        .Where(x => x.AlarmGrupId == ilgiliAlarmGrupId)
                        .Select(x => x.AlarmId.GetValueOrDefault())
                        .ToList();

                    var ilgiliAlarmLoglar = kumesAlarmLogs.Where(x => x.AlarmId != null && grupAlarmIds.Contains(x.AlarmId.Value)).ToList();

                    if (!ilgiliAlarmLoglar.Any())
                        continue;

                    var gonderilebilirLoglar = ilgiliAlarmLoglar
                        .Where(log =>
                        {
                            var bildirimZamani = log.KayitZamani?.Add(
                                kullaniciSms.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSms.BildirimSuresi) ?? DateTime.Now;
                            return DateTime.Now >= bildirimZamani;
                        }).ToList();

                    if (!gonderilebilirLoglar.Any())
                        continue;

                    var mesajlar = SmsMetinleriOlusturForKumes(gonderilebilirLoglar, alarmlar);

                    foreach (var mesaj in mesajlar)
                    {
                        if (mesaj.AlarmLogIds == null || mesaj.AlarmLogIds.Count == 0)
                            continue;

                        await SmsGonderAsync(
                            sharedDbContext,
                            kullaniciSms.UserId,
                            kullaniciSms.SeviyeNo ?? 0,
                            kullaniciSms.PhoneNumber,
                            mesaj.Mesaj,
                            AlarmKaynakTuru.Kumes,
                            mesaj.AlarmLogIds);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task SuHatScadaAlarmGonderAsync(List<WaterLineScada.Domain.Models.AlarmLog> suHatAlarmLogs)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

                var alarmIds = suHatAlarmLogs.Select(x => x.AlarmId.GetValueOrDefault()).Distinct().ToList();

                var alarmGrupEslemeleri = await sharedDbContext.AlarmGrupAlarmEsleme
                    .Where(x => alarmIds.Contains(x.AlarmId.GetValueOrDefault()) && x.Silindi != true)
                    .ToListAsync();

                var alarmlar = await sharedDbContext.vwAlarmlar
                    .Where(x => alarmIds.Contains(x.Id) && x.Silindi != true)
                    .ToDictionaryAsync(x => x.Id);

                var alarmGrupIds = alarmGrupEslemeleri.Select(x => x.AlarmGrupId).Distinct().ToList();

                var kullaniciEslemeler = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.EPosta)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                var kullaniciEslemelerSesliMesaj = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.SesliMesaj)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                // Kullanıcı bazlı alarm listesi
                var kullaniciAlarmMap = new Dictionary<string, (vwAlarmKullaniciEslemeForNotification kullanici, List<(vwAlarmlar, int, DateTime)> alarmlar)>();

                foreach (var log in suHatAlarmLogs)
                {
                    if (!alarmlar.TryGetValue(log.AlarmId ?? 0, out var alarm))
                        continue;

                    var ilgiliAlarmGrupIdList = alarmGrupEslemeleri
                        .Where(x => x.AlarmId == alarm.Id)
                        .Select(x => x.AlarmGrupId)
                        .Distinct()
                        .ToList();

                    var eslesenKullanicilar = kullaniciEslemeler
                        .Where(x => ilgiliAlarmGrupIdList.Contains(x.AlarmGrupId ?? 0))
                        .GroupBy(x => new { x.UserId, x.SeviyeNo })
                        .Select(g => g.First())
                        .OrderBy(x => x.SeviyeNo)
                        .ToList();

                    var seviyelereGoreKullaniciGruplari = eslesenKullanicilar
                        .GroupBy(x => x.SeviyeNo)
                        .OrderBy(x => x.Key);

                    DateTime? oncekiSeviyeGonderimZamani = null;
                    foreach (var seviyeGrubu in seviyelereGoreKullaniciGruplari)
                    {
                        var seviyeNo = seviyeGrubu.Key.GetValueOrDefault();
                        var kullanicilar = seviyeGrubu.ToList();

                        if (oncekiSeviyeGonderimZamani.HasValue &&
                            DateTime.Now < oncekiSeviyeGonderimZamani.Value)
                        {
                            continue;
                        }

                        foreach (var kullanici in kullanicilar)
                        {
                            string match = $",{log.Id},";
                            // Mail zaten gönderildi mi kontrolü                     
                            var takipTipi = BildirimYontemi.EPosta;
                            var kaynakTuru = AlarmKaynakTuru.SuHat;
                            var mailZatenGonderildiMi = await (from m in sharedDbContext.MailGonderimTakip.AsNoTracking()
                                                               join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                                                                 on m.Id equals j.TakipId
                                                               where m.UserId == kullanici.UserId
                                                                  && m.SeviyeNo == seviyeNo
                                                                  && j.TakipTipi == takipTipi
                                                                  && j.AlarmKaynakTuru == kaynakTuru
                                                                  && j.AlarmLogId == log.Id
                                                                  && ((m.GonderimBasarili ?? false) == true || !(m.KalanDenemeSayisi == 0 && (m.GonderimBasarili ?? false) == false))
                                                               select 1).AnyAsync();

                            if (mailZatenGonderildiMi)
                                continue;

                            // Zaman kontrolü
                            var bildirimZamani = log.KayitZamani?.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi) ?? DateTime.Now;
                            if (DateTime.Now >= bildirimZamani)
                            {
                                // Kullanıcıyı anahtar olarak kullan (UserId bazlı)
                                if (!kullaniciAlarmMap.ContainsKey(kullanici.UserId))
                                {
                                    kullaniciAlarmMap[kullanici.UserId] = (kullanici, new List<(vwAlarmlar, int, DateTime)>());
                                }

                                kullaniciAlarmMap[kullanici.UserId].alarmlar.Add((alarm, log.Id, log.KayitZamani ?? DateTime.Now));

                                oncekiSeviyeGonderimZamani = DateTime.Now.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi);
                            }
                        }
                    }
                }
                // Tüm kullanıcılar için e-postaları gönder
                foreach (var (_, (kullanici, alarmlarList)) in kullaniciAlarmMap)
                {
                    await BildirimGonderAsync(kullanici, alarmlarList, AlarmKaynakTuru.SuHat);
                }

                // ==========================
                // 2) Sesli Mesaj Bildirimleri (AlarmGrup bazlı)
                // ==========================
                foreach (var kullaniciSesliMesaj in kullaniciEslemelerSesliMesaj)
                {
                    // 1) Bu kullanıcıya ait alarm grubu
                    var ilgiliAlarmGrupId = kullaniciSesliMesaj.AlarmGrupId ?? 0;

                    // 2) Bu gruba bağlı alarmId’ler
                    var grupAlarmIds = alarmGrupEslemeleri
                        .Where(x => x.AlarmGrupId == ilgiliAlarmGrupId)
                        .Select(x => x.AlarmId.GetValueOrDefault())
                        .ToList();

                    // 3) Loglardan bu gruba girenler
                    var ilgiliAlarmLoglar = suHatAlarmLogs
                        .Where(x => x.AlarmId != null && grupAlarmIds.Contains(x.AlarmId.Value))
                        .ToList();

                    if (!ilgiliAlarmLoglar.Any())
                        continue;

                    var gonderilebilirLoglar = ilgiliAlarmLoglar.Where(log => { var bildirimZamani = log.KayitZamani?.Add(kullaniciSesliMesaj.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSesliMesaj.BildirimSuresi) ?? DateTime.Now; return DateTime.Now >= bildirimZamani; }).ToList();

                    if (!gonderilebilirLoglar.Any())
                        continue;
                    // 4) Sesli mesaj içeriğini oluştur
                    var mesajlar = SesliMesajMetinleriOlusturSuHat(gonderilebilirLoglar, alarmlar);

                    foreach (var mesaj in mesajlar)
                    {
                        // Bu mesajın log ID’leri
                        var mesajAlarmLogIds = mesaj.AlarmLogIds;

                        // 5) Daha önce gönderilmiş mi kontrolü                     
                        var seviyeNo = kullaniciSesliMesaj.SeviyeNo ?? 0;
                        var alarmLogIdList = mesajAlarmLogIds.Distinct().ToList();

                        var sonGonderim = await (
                            from t in sharedDbContext.MesajGonderimTakip.AsNoTracking()
                            join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                                on t.Id equals j.TakipId
                            where t.UserId == kullaniciSesliMesaj.UserId
                               && t.SeviyeNo == seviyeNo
                               && j.TakipTipi == BildirimYontemi.SesliMesaj
                               && j.AlarmKaynakTuru == AlarmKaynakTuru.SuHat
                               && alarmLogIdList.Contains(j.AlarmLogId)
                            orderby t.KayitZamani descending
                            select t
                        ).FirstOrDefaultAsync();

                        bool gonderilsinMi = false;
                        if (sonGonderim == null)
                        {
                            gonderilsinMi = true;
                        }
                        else if ((kullaniciSesliMesaj.TekrarlansinMi ?? false) &&
                                 kullaniciSesliMesaj.TekrarlamaSuresi != null &&
                                 DateTime.Now - sonGonderim.KayitZamani >= kullaniciSesliMesaj.TekrarlamaSuresi)
                        {
                            gonderilsinMi = true;
                        }

                        // 6) Gönderim yapılacaksa                     
                        if (gonderilsinMi && !string.IsNullOrEmpty(mesaj.Mesaj))
                        {
                            await using var tx = await sharedDbContext.Database.BeginTransactionAsync();
                            try
                            {
                                var takip = new MesajGonderimTakip
                                {
                                    KayitZamani = DateTime.Now,
                                    SmsIcerik = mesaj.Mesaj,
                                    TelefonNo = kullaniciSesliMesaj.PhoneNumber,
                                    UserId = kullaniciSesliMesaj.UserId,
                                    SeviyeNo = kullaniciSesliMesaj.SeviyeNo ?? 0,
                                    KalanDenemeSayisi = 10,
                                    SesDosyaYolu = null,
                                    AramaSure = null,
                                    CevaplandiMi = null,
                                    GonderimBasarili = false,
                                    //SuHatAlarmLogIds = string.Join(",", mesaj.AlarmLogIds)
                                };
                                sharedDbContext.MesajGonderimTakip.Add(takip);
                                await sharedDbContext.SaveChangesAsync();

                                var takipId = takip.Id;
                                var takipTipi = BildirimYontemi.SesliMesaj;
                                var kaynakTuru = AlarmKaynakTuru.SuHat;

                                var joinRows = mesaj.AlarmLogIds.Distinct().Select(alarmLogId => new MesajGonderimTakipAlarmLog
                                {
                                    TakipId = takipId,
                                    TakipTipi = takipTipi,
                                    AlarmKaynakTuru = kaynakTuru,
                                    AlarmLogId = alarmLogId
                                }).ToList();

                                sharedDbContext.MesajGonderimTakipAlarmLog.AddRange(joinRows);
                                await sharedDbContext.SaveChangesAsync();
                                await tx.CommitAsync();
                            }
                            catch (Exception ex)
                            {
                                await tx.RollbackAsync();
                            }
                        }
                    }
                }

                // ==========================
                // 3) SMS Bildirimleri (AlarmGrup bazlı)
                // ==========================
                var kullaniciEslemelerSms = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.SMS)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                foreach (var kullaniciSms in kullaniciEslemelerSms)
                {
                    var ilgiliAlarmGrupId = kullaniciSms.AlarmGrupId ?? 0;

                    var grupAlarmIds = alarmGrupEslemeleri
                        .Where(x => x.AlarmGrupId == ilgiliAlarmGrupId)
                        .Select(x => x.AlarmId.GetValueOrDefault())
                        .ToList();

                    var ilgiliAlarmLoglar = suHatAlarmLogs.Where(x => x.AlarmId != null && grupAlarmIds.Contains(x.AlarmId.Value)).ToList();

                    if (!ilgiliAlarmLoglar.Any())
                        continue;

                    var gonderilebilirLoglar = ilgiliAlarmLoglar
                        .Where(log =>
                        {
                            var bildirimZamani = log.KayitZamani?.Add(
                                kullaniciSms.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSms.BildirimSuresi) ?? DateTime.Now;
                            return DateTime.Now >= bildirimZamani;
                        }).ToList();

                    if (!gonderilebilirLoglar.Any())
                        continue;

                    var mesajlar = SmsMetinleriOlusturSuHat(gonderilebilirLoglar, alarmlar);

                    foreach (var mesaj in mesajlar)
                    {
                        if (mesaj.AlarmLogIds == null || mesaj.AlarmLogIds.Count == 0)
                            continue;

                        await SmsGonderAsync(
                            sharedDbContext,
                            kullaniciSms.UserId,
                            kullaniciSms.SeviyeNo ?? 0,
                            kullaniciSms.PhoneNumber,
                            mesaj.Mesaj,
                            AlarmKaynakTuru.SuHat,
                            mesaj.AlarmLogIds);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task EnerjiIzlemeAlarmGonderAsync(List<EnergyMonitoring.Domain.Models.AlarmLog> enerjiIzlemeAlarmLogs)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

                var alarmIds = enerjiIzlemeAlarmLogs.Select(x => x.AlarmId.GetValueOrDefault()).Distinct().ToList();

                var alarmGrupEslemeleri = await sharedDbContext.AlarmGrupAlarmEsleme
                    .Where(x => alarmIds.Contains(x.AlarmId.GetValueOrDefault()) && x.Silindi != true)
                    .ToListAsync();

                var alarmlar = await sharedDbContext.vwAlarmlar
                    .Where(x => alarmIds.Contains(x.Id) && x.Silindi != true)
                    .ToDictionaryAsync(x => x.Id);

                var alarmGrupIds = alarmGrupEslemeleri.Select(x => x.AlarmGrupId).Distinct().ToList();

                var kullaniciEslemeler = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.EPosta)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                var kullaniciEslemelerSesliMesaj = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.SesliMesaj)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                // Kullanıcı bazlı alarm listesi
                var kullaniciAlarmMap = new Dictionary<string, (vwAlarmKullaniciEslemeForNotification kullanici, List<(vwAlarmlar, int, DateTime)> alarmlar)>();

                foreach (var log in enerjiIzlemeAlarmLogs)
                {
                    if (!alarmlar.TryGetValue(log.AlarmId ?? 0, out var alarm))
                        continue;

                    var ilgiliAlarmGrupIdList = alarmGrupEslemeleri
                        .Where(x => x.AlarmId == alarm.Id)
                        .Select(x => x.AlarmGrupId)
                        .Distinct()
                        .ToList();

                    var eslesenKullanicilar = kullaniciEslemeler
                        .Where(x => ilgiliAlarmGrupIdList.Contains(x.AlarmGrupId ?? 0))
                        .GroupBy(x => new { x.UserId, x.SeviyeNo })
                        .Select(g => g.First())
                        .OrderBy(x => x.SeviyeNo)
                        .ToList();

                    var seviyelereGoreKullaniciGruplari = eslesenKullanicilar
                        .GroupBy(x => x.SeviyeNo)
                        .OrderBy(x => x.Key);

                    DateTime? oncekiSeviyeGonderimZamani = null;
                    foreach (var seviyeGrubu in seviyelereGoreKullaniciGruplari)
                    {
                        var seviyeNo = seviyeGrubu.Key.GetValueOrDefault();
                        var kullanicilar = seviyeGrubu.ToList();

                        if (oncekiSeviyeGonderimZamani.HasValue &&
                            DateTime.Now < oncekiSeviyeGonderimZamani.Value)
                        {
                            continue;
                        }

                        foreach (var kullanici in kullanicilar)
                        {
                            string match = $",{log.Id},";
                            // Mail zaten gönderildi mi kontrolü                     
                            var takipTipi = BildirimYontemi.EPosta;
                            var kaynakTuru = AlarmKaynakTuru.EnerjiIzleme;
                            var mailZatenGonderildiMi = await (from m in sharedDbContext.MailGonderimTakip.AsNoTracking()
                                                               join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                                                                 on m.Id equals j.TakipId
                                                               where m.UserId == kullanici.UserId
                                                                  && m.SeviyeNo == seviyeNo
                                                                  && j.TakipTipi == takipTipi
                                                                  && j.AlarmKaynakTuru == kaynakTuru
                                                                  && j.AlarmLogId == log.Id
                                                                  && ((m.GonderimBasarili ?? false) == true || !(m.KalanDenemeSayisi == 0 && (m.GonderimBasarili ?? false) == false))
                                                               select 1).AnyAsync();

                            if (mailZatenGonderildiMi)
                                continue;

                            // Zaman kontrolü
                            var bildirimZamani = log.KayitZamani?.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi) ?? DateTime.Now;
                            if (DateTime.Now >= bildirimZamani)
                            {
                                // Kullanıcıyı anahtar olarak kullan (UserId bazlı)
                                if (!kullaniciAlarmMap.ContainsKey(kullanici.UserId))
                                {
                                    kullaniciAlarmMap[kullanici.UserId] = (kullanici, new List<(vwAlarmlar, int, DateTime)>());
                                }

                                kullaniciAlarmMap[kullanici.UserId].alarmlar.Add((alarm, log.Id, log.KayitZamani ?? DateTime.Now));

                                oncekiSeviyeGonderimZamani = DateTime.Now.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi);
                            }
                        }
                    }
                }

                // Tüm kullanıcılar için e-postaları gönder
                foreach (var (_, (kullanici, alarmlarList)) in kullaniciAlarmMap)
                {
                    await BildirimGonderAsync(kullanici, alarmlarList, AlarmKaynakTuru.EnerjiIzleme);
                }

                // ==========================
                // 2) Sesli Mesaj Bildirimleri (AlarmGrup bazlı)
                // ==========================
                foreach (var kullaniciSesliMesaj in kullaniciEslemelerSesliMesaj)
                {
                    // 1) Bu kullanıcıya ait alarm grubu
                    var ilgiliAlarmGrupId = kullaniciSesliMesaj.AlarmGrupId ?? 0;

                    // 2) Bu gruba bağlı alarmId’ler
                    var grupAlarmIds = alarmGrupEslemeleri
                        .Where(x => x.AlarmGrupId == ilgiliAlarmGrupId)
                        .Select(x => x.AlarmId.GetValueOrDefault())
                        .ToList();

                    // 3) Loglardan bu gruba girenler
                    var ilgiliAlarmLoglar = enerjiIzlemeAlarmLogs
                        .Where(x => x.AlarmId != null && grupAlarmIds.Contains(x.AlarmId.Value))
                        .ToList();

                    if (!ilgiliAlarmLoglar.Any())
                        continue;

                    var gonderilebilirLoglar = ilgiliAlarmLoglar.Where(log => { var bildirimZamani = log.KayitZamani?.Add(kullaniciSesliMesaj.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSesliMesaj.BildirimSuresi) ?? DateTime.Now; return DateTime.Now >= bildirimZamani; }).ToList();

                    if (!gonderilebilirLoglar.Any())
                        continue;
                    // 4) Sesli mesaj içeriğini oluştur
                    var mesajlar = SesliMesajMetinleriOlusturEnerjiIzleme(gonderilebilirLoglar, alarmlar);

                    foreach (var mesaj in mesajlar)
                    {
                        // Bu mesajın log ID’leri
                        var mesajAlarmLogIds = mesaj.AlarmLogIds;

                        // 5) Daha önce gönderilmiş mi kontrolü                     
                        var seviyeNo = kullaniciSesliMesaj.SeviyeNo ?? 0;
                        var alarmLogIdList = mesajAlarmLogIds.Distinct().ToList();

                        var sonGonderim = await (
                            from t in sharedDbContext.MesajGonderimTakip.AsNoTracking()
                            join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                                on t.Id equals j.TakipId
                            where t.UserId == kullaniciSesliMesaj.UserId
                               && t.SeviyeNo == seviyeNo
                               && j.TakipTipi == BildirimYontemi.SesliMesaj
                               && j.AlarmKaynakTuru == AlarmKaynakTuru.EnerjiIzleme
                               && alarmLogIdList.Contains(j.AlarmLogId)
                            orderby t.KayitZamani descending
                            select t
                        ).FirstOrDefaultAsync();

                        bool gonderilsinMi = false;
                        if (sonGonderim == null)
                        {
                            gonderilsinMi = true;
                        }
                        else if ((kullaniciSesliMesaj.TekrarlansinMi ?? false) &&
                                 kullaniciSesliMesaj.TekrarlamaSuresi != null &&
                                 DateTime.Now - sonGonderim.KayitZamani >= kullaniciSesliMesaj.TekrarlamaSuresi)
                        {
                            gonderilsinMi = true;
                        }

                        // 6) Gönderim yapılacaksa                     
                        if (gonderilsinMi && !string.IsNullOrEmpty(mesaj.Mesaj))
                        {
                            await using var tx = await sharedDbContext.Database.BeginTransactionAsync();
                            try
                            {
                                var takip = new MesajGonderimTakip
                                {
                                    KayitZamani = DateTime.Now,
                                    SmsIcerik = mesaj.Mesaj,
                                    TelefonNo = kullaniciSesliMesaj.PhoneNumber,
                                    UserId = kullaniciSesliMesaj.UserId,
                                    SeviyeNo = kullaniciSesliMesaj.SeviyeNo ?? 0,
                                    KalanDenemeSayisi = 10,
                                    SesDosyaYolu = null,
                                    AramaSure = null,
                                    CevaplandiMi = null,
                                    GonderimBasarili = false,
                                    //SuHatAlarmLogIds = string.Join(",", mesaj.AlarmLogIds)
                                };
                                sharedDbContext.MesajGonderimTakip.Add(takip);
                                await sharedDbContext.SaveChangesAsync();

                                var takipId = takip.Id;
                                var takipTipi = BildirimYontemi.SesliMesaj;
                                var kaynakTuru = AlarmKaynakTuru.EnerjiIzleme;

                                var joinRows = mesaj.AlarmLogIds.Distinct().Select(alarmLogId => new MesajGonderimTakipAlarmLog
                                {
                                    TakipId = takipId,
                                    TakipTipi = takipTipi,
                                    AlarmKaynakTuru = kaynakTuru,
                                    AlarmLogId = alarmLogId
                                }).ToList();

                                sharedDbContext.MesajGonderimTakipAlarmLog.AddRange(joinRows);
                                await sharedDbContext.SaveChangesAsync();
                                await tx.CommitAsync();
                            }
                            catch (Exception ex)
                            {
                                await tx.RollbackAsync();
                            }
                        }
                    }
                }

                // ==========================
                // 3) SMS Bildirimleri (AlarmGrup bazlı)
                // ==========================
                var kullaniciEslemelerSms = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.SMS)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                foreach (var kullaniciSms in kullaniciEslemelerSms)
                {
                    var ilgiliAlarmGrupId = kullaniciSms.AlarmGrupId ?? 0;

                    var grupAlarmIds = alarmGrupEslemeleri
                        .Where(x => x.AlarmGrupId == ilgiliAlarmGrupId)
                        .Select(x => x.AlarmId.GetValueOrDefault())
                        .ToList();

                    var ilgiliAlarmLoglar = enerjiIzlemeAlarmLogs.Where(x => x.AlarmId != null && grupAlarmIds.Contains(x.AlarmId.Value)).ToList();

                    if (!ilgiliAlarmLoglar.Any())
                        continue;

                    var gonderilebilirLoglar = ilgiliAlarmLoglar
                        .Where(log =>
                        {
                            var bildirimZamani = log.KayitZamani?.Add(
                                kullaniciSms.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSms.BildirimSuresi) ?? DateTime.Now;
                            return DateTime.Now >= bildirimZamani;
                        }).ToList();

                    if (!gonderilebilirLoglar.Any())
                        continue;

                    var mesajlar = SmsMetinleriOlusturEnerjiIzleme(gonderilebilirLoglar, alarmlar);

                    foreach (var mesaj in mesajlar)
                    {
                        if (mesaj.AlarmLogIds == null || mesaj.AlarmLogIds.Count == 0)
                            continue;

                        await SmsGonderAsync(
                            sharedDbContext,
                            kullaniciSms.UserId,
                            kullaniciSms.SeviyeNo ?? 0,
                            kullaniciSms.PhoneNumber,
                            mesaj.Mesaj,
                            AlarmKaynakTuru.EnerjiIzleme,
                            mesaj.AlarmLogIds);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task YTKAlarmGonderAsync(List<PoultryHouseScada.Domain.Models.AlarmLog> ytkAlarmLogs)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

                var alarmIds = ytkAlarmLogs.Select(x => x.AlarmId.GetValueOrDefault()).Distinct().ToList();

                var alarmGrupEslemeleri = await sharedDbContext.AlarmGrupAlarmEsleme
                    .Where(x => alarmIds.Contains(x.AlarmId.GetValueOrDefault()) && x.Silindi != true)
                    .ToListAsync();

                var alarmlar = await sharedDbContext.vwAlarmlar
                    .Where(x => alarmIds.Contains(x.Id) && x.Silindi != true)
                    .ToDictionaryAsync(x => x.Id);

                var alarmGrupIds = alarmGrupEslemeleri.Select(x => x.AlarmGrupId).Distinct().ToList();

                var kullaniciEslemeler = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.EPosta)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                var kullaniciEslemelerSesliMesaj = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.SesliMesaj)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                // Kullanıcı bazlı alarm listesi
                var kullaniciAlarmMap = new Dictionary<string, (vwAlarmKullaniciEslemeForNotification kullanici, List<(vwAlarmlar, int, DateTime)> alarmlar)>();

                foreach (var log in ytkAlarmLogs)
                {
                    if (!alarmlar.TryGetValue(log.AlarmId ?? 0, out var alarm))
                        continue;

                    var ilgiliAlarmGrupIdList = alarmGrupEslemeleri
                        .Where(x => x.AlarmId == alarm.Id)
                        .Select(x => x.AlarmGrupId)
                        .Distinct()
                        .ToList();

                    var eslesenKullanicilar = kullaniciEslemeler
                        .Where(x => ilgiliAlarmGrupIdList.Contains(x.AlarmGrupId ?? 0))
                        .GroupBy(x => new { x.UserId, x.SeviyeNo })
                        .Select(g => g.First())
                        .OrderBy(x => x.SeviyeNo)
                        .ToList();

                    var seviyelereGoreKullaniciGruplari = eslesenKullanicilar
                        .GroupBy(x => x.SeviyeNo)
                        .OrderBy(x => x.Key);

                    DateTime? oncekiSeviyeGonderimZamani = null;
                    foreach (var seviyeGrubu in seviyelereGoreKullaniciGruplari)
                    {
                        var seviyeNo = seviyeGrubu.Key.GetValueOrDefault();
                        var kullanicilar = seviyeGrubu.ToList();

                        if (oncekiSeviyeGonderimZamani.HasValue &&
                            DateTime.Now < oncekiSeviyeGonderimZamani.Value)
                        {
                            continue;
                        }

                        foreach (var kullanici in kullanicilar)
                        {
                            string match = $",{log.Id},";
                            // Mail zaten gönderildi mi kontrolü                     
                            var takipTipi = BildirimYontemi.EPosta;
                            var kaynakTuru = AlarmKaynakTuru.Konveyor;
                            var mailZatenGonderildiMi = await (from m in sharedDbContext.MailGonderimTakip.AsNoTracking()
                                                               join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                                                                 on m.Id equals j.TakipId
                                                               where m.UserId == kullanici.UserId
                                                                  && m.SeviyeNo == seviyeNo
                                                                  && j.TakipTipi == takipTipi
                                                                  && j.AlarmKaynakTuru == kaynakTuru
                                                                  && j.AlarmLogId == log.Id
                                                                  && ((m.GonderimBasarili ?? false) == true || !(m.KalanDenemeSayisi == 0 && (m.GonderimBasarili ?? false) == false))
                                                               select 1).AnyAsync();

                            if (mailZatenGonderildiMi)
                                continue;

                            // Zaman kontrolü
                            var bildirimZamani = log.KayitZamani?.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi) ?? DateTime.Now;
                            if (DateTime.Now >= bildirimZamani)
                            {
                                // Kullanıcıyı anahtar olarak kullan (UserId bazlı)
                                if (!kullaniciAlarmMap.ContainsKey(kullanici.UserId))
                                {
                                    kullaniciAlarmMap[kullanici.UserId] = (kullanici, new List<(vwAlarmlar, int, DateTime)>());
                                }

                                kullaniciAlarmMap[kullanici.UserId].alarmlar.Add((alarm, log.Id, log.KayitZamani ?? DateTime.Now));

                                oncekiSeviyeGonderimZamani = DateTime.Now.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi);
                            }
                        }
                    }
                }
                // Tüm kullanıcılar için e-postaları gönder
                foreach (var (_, (kullanici, alarmlarList)) in kullaniciAlarmMap)
                {
                    await BildirimGonderAsync(kullanici, alarmlarList, AlarmKaynakTuru.Konveyor);
                }

                // ==========================
                // 2) Sesli Mesaj Bildirimleri (AlarmGrup bazlı)
                // ==========================
                foreach (var kullaniciSesliMesaj in kullaniciEslemelerSesliMesaj)
                {
                    // 1) Bu kullanıcıya ait alarm grubu
                    var ilgiliAlarmGrupId = kullaniciSesliMesaj.AlarmGrupId ?? 0;

                    // 2) Bu gruba bağlı alarmId’ler
                    var grupAlarmIds = alarmGrupEslemeleri
                        .Where(x => x.AlarmGrupId == ilgiliAlarmGrupId)
                        .Select(x => x.AlarmId.GetValueOrDefault())
                        .ToList();

                    // 3) Loglardan bu gruba girenler
                    var ilgiliAlarmLoglar = ytkAlarmLogs
                        .Where(x => x.AlarmId != null && grupAlarmIds.Contains(x.AlarmId.Value))
                        .ToList();

                    if (!ilgiliAlarmLoglar.Any())
                        continue;

                    var gonderilebilirLoglar = ilgiliAlarmLoglar.Where(log => { var bildirimZamani = log.KayitZamani?.Add(kullaniciSesliMesaj.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSesliMesaj.BildirimSuresi) ?? DateTime.Now; return DateTime.Now >= bildirimZamani; }).ToList();

                    if (!gonderilebilirLoglar.Any())
                        continue;
                    // 4) Sesli mesaj içeriğini oluştur
                    var mesajlar = SesliMesajMetinleriOlusturYTK(gonderilebilirLoglar, alarmlar);

                    foreach (var mesaj in mesajlar)
                    {
                        // Bu mesajın log ID’leri
                        var mesajAlarmLogIds = mesaj.AlarmLogIds;

                        // 5) Daha önce gönderilmiş mi kontrolü                     
                        var seviyeNo = kullaniciSesliMesaj.SeviyeNo ?? 0;
                        var alarmLogIdList = mesajAlarmLogIds.Distinct().ToList();

                        var sonGonderim = await (
                            from t in sharedDbContext.MesajGonderimTakip.AsNoTracking()
                            join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                                on t.Id equals j.TakipId
                            where t.UserId == kullaniciSesliMesaj.UserId
                               && t.SeviyeNo == seviyeNo
                               && j.TakipTipi == BildirimYontemi.SesliMesaj
                               && j.AlarmKaynakTuru == AlarmKaynakTuru.Konveyor
                               && alarmLogIdList.Contains(j.AlarmLogId)
                            orderby t.KayitZamani descending
                            select t
                        ).FirstOrDefaultAsync();

                        bool gonderilsinMi = false;
                        if (sonGonderim == null)
                        {
                            gonderilsinMi = true;
                        }
                        else if ((kullaniciSesliMesaj.TekrarlansinMi ?? false) &&
                                 kullaniciSesliMesaj.TekrarlamaSuresi != null &&
                                 DateTime.Now - sonGonderim.KayitZamani >= kullaniciSesliMesaj.TekrarlamaSuresi)
                        {
                            gonderilsinMi = true;
                        }

                        // 6) Gönderim yapılacaksa                     
                        if (gonderilsinMi && !string.IsNullOrEmpty(mesaj.Mesaj))
                        {
                            await using var tx = await sharedDbContext.Database.BeginTransactionAsync();
                            try
                            {
                                var takip = new MesajGonderimTakip
                                {
                                    KayitZamani = DateTime.Now,
                                    SmsIcerik = mesaj.Mesaj,
                                    TelefonNo = kullaniciSesliMesaj.PhoneNumber,
                                    UserId = kullaniciSesliMesaj.UserId,
                                    SeviyeNo = kullaniciSesliMesaj.SeviyeNo ?? 0,
                                    KalanDenemeSayisi = 10,
                                    SesDosyaYolu = null,
                                    AramaSure = null,
                                    CevaplandiMi = null,
                                    GonderimBasarili = false,
                                    //SuHatAlarmLogIds = string.Join(",", mesaj.AlarmLogIds)
                                };
                                sharedDbContext.MesajGonderimTakip.Add(takip);
                                await sharedDbContext.SaveChangesAsync();

                                var takipId = takip.Id;
                                var takipTipi = BildirimYontemi.SesliMesaj;
                                var kaynakTuru = AlarmKaynakTuru.Konveyor;

                                var joinRows = mesaj.AlarmLogIds.Distinct().Select(alarmLogId => new MesajGonderimTakipAlarmLog
                                {
                                    TakipId = takipId,
                                    TakipTipi = takipTipi,
                                    AlarmKaynakTuru = kaynakTuru,
                                    AlarmLogId = alarmLogId
                                }).ToList();

                                sharedDbContext.MesajGonderimTakipAlarmLog.AddRange(joinRows);
                                await sharedDbContext.SaveChangesAsync();
                                await tx.CommitAsync();
                            }
                            catch (Exception ex)
                            {
                                await tx.RollbackAsync();
                            }
                        }
                    }
                }

                // ==========================
                // 3) SMS Bildirimleri (AlarmGrup bazlı)
                // ==========================
                var kullaniciEslemelerSms = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.SMS)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                foreach (var kullaniciSms in kullaniciEslemelerSms)
                {
                    var ilgiliAlarmGrupId = kullaniciSms.AlarmGrupId ?? 0;

                    var grupAlarmIds = alarmGrupEslemeleri
                        .Where(x => x.AlarmGrupId == ilgiliAlarmGrupId)
                        .Select(x => x.AlarmId.GetValueOrDefault())
                        .ToList();

                    var ilgiliAlarmLoglar = ytkAlarmLogs.Where(x => x.AlarmId != null && grupAlarmIds.Contains(x.AlarmId.Value)).ToList();

                    if (!ilgiliAlarmLoglar.Any())
                        continue;

                    var gonderilebilirLoglar = ilgiliAlarmLoglar
                        .Where(log =>
                        {
                            var bildirimZamani = log.KayitZamani?.Add(
                                kullaniciSms.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSms.BildirimSuresi) ?? DateTime.Now;
                            return DateTime.Now >= bildirimZamani;
                        }).ToList();

                    if (!gonderilebilirLoglar.Any())
                        continue;

                    var mesajlar = SmsMetinleriOlusturYTK(gonderilebilirLoglar, alarmlar);

                    foreach (var mesaj in mesajlar)
                    {
                        if (mesaj.AlarmLogIds == null || mesaj.AlarmLogIds.Count == 0)
                            continue;

                        await SmsGonderAsync(
                            sharedDbContext,
                            kullaniciSms.UserId,
                            kullaniciSms.SeviyeNo ?? 0,
                            kullaniciSms.PhoneNumber,
                            mesaj.Mesaj,
                            AlarmKaynakTuru.Konveyor,
                            mesaj.AlarmLogIds);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
        private sealed class SharedAlarmBildirimGrubu
        {
            public SharedApp.Domain.Models.AlarmLog TemsilciLog { get; set; } = default!;

            public vwAlarmlar TemsilciAlarm { get; set; } = default!;

            public List<SharedApp.Domain.Models.AlarmLog> AlarmLoglari { get; set; } = new();

            public int? FizikselLokasyonId { get; set; }

            public string? FizikselLokasyonAdi { get; set; }

            public List<int> AlarmLogIds => AlarmLoglari.Select(x => x.Id).Distinct().ToList();

            public DateTime? KayitZamani => AlarmLoglari.Count > 0 ? AlarmLoglari.Min(x => x.KayitZamani) : DateTime.Now;
            //AlarmLoglari.Select(x => x.KayitZamani).DefaultIfEmpty(DateTime.Now).Min();
        }
        private static string SharedAlarmBildirimAnahtariOlustur(SharedApp.Domain.Models.AlarmLog log, vwAlarmlar alarm)
        {
            var eslemeSozcugu = alarm.AlarmEslemeSozcugu?
                .Trim()
                .ToUpperInvariant();

            /*
             * Fiziksel lokasyon yoksa alarm birleştirilmez.
             * Böylece farklı tesislerdeki alarmlar yanlışlıkla birleşmez.
             */
            if (!alarm.FizikselLokasyonId.HasValue)
                return $"ALARM_LOG:{log.Id}";

            var fizikselLokasyonId = alarm.FizikselLokasyonId.Value;

            return eslemeSozcugu switch
            {
                "DUMAN_1" or "DUMAN_1-1"
                    => $"DUMAN:{fizikselLokasyonId}",

                "YANGIN_1" or "YANGIN_1-1"
                    => $"YANGIN:{fizikselLokasyonId}",

                _ => $"ALARM_LOG:{log.Id}"
            };
        }
        private static async Task<List<MailEkBilgisi>> SonAlarmFotografBilgileriniGetirAsync(SharedDbContext sharedDbContext, IEnumerable<int> alarmLogIds, CancellationToken cancellationToken = default)
        {
            var alarmLogIdListesi = alarmLogIds.Distinct().ToList();

            if (alarmLogIdListesi.Count == 0)
                return new List<MailEkBilgisi>();

            /*
             * Her AlarmLogId için en son YanginAlgilamaLogs
             * kaydının ID değerini alıyoruz.
             */
            var sonFotografKayitIdleri = await sharedDbContext.YanginAlgilamaLogs
                    .AsNoTracking()
                    .Where(x =>
                        x.AlarmLogId.HasValue &&
                        alarmLogIdListesi.Contains(x.AlarmLogId.Value) &&
                        x.Silindi != true &&
                        x.AnlikFotografDosyaYolu != null &&
                        x.AnlikFotografDosyaYolu != "")
                    .GroupBy(x => x.AlarmLogId!.Value)
                    .Select(grup => grup.Max(x => x.Id))
                    .ToListAsync(cancellationToken);

            if (sonFotografKayitIdleri.Count == 0)
                return new List<MailEkBilgisi>();

            /*
             * Kamera 1 ve Kamera 1.1 için son fotoğraf bilgileri
             * ayrı ayrı döner.
             */
            return await sharedDbContext.YanginAlgilamaLogs
                .AsNoTracking()
                .Where(x => sonFotografKayitIdleri.Contains(x.Id) && x.AnlikFotografDosyaYolu != null)
                .Select(x => new MailEkBilgisi
                {
                    AlarmLogId = x.AlarmLogId,

                    GoreliDosyaYolu = x.AnlikFotografDosyaYolu!,
                    DosyaAdi = x.AnlikFotografDosyaAdi,
                    ContentType = x.AnlikFotografContentType,
                    DosyaBoyutu = x.AnlikFotografBoyutu
                })
                .ToListAsync(cancellationToken);
        }
        private static List<SharedAlarmBildirimGrubu> SharedAlarmBildirimGruplariniOlustur(IEnumerable<SharedApp.Domain.Models.AlarmLog> sharedAlarmLogs, IReadOnlyDictionary<int, vwAlarmlar> alarmlar, IReadOnlyDictionary<int, string> fizikselLokasyonlar)
        {
            var alarmLogBilgileri = sharedAlarmLogs
                .Where(log => log.AlarmId.HasValue)
                .Select(log =>
                {
                    alarmlar.TryGetValue(log.AlarmId!.Value, out var alarm);

                    return new
                    {
                        Log = log,
                        Alarm = alarm
                    };
                })
                .Where(x => x.Alarm != null)
                .Select(x => new
                {
                    x.Log,
                    Alarm = x.Alarm!,
                    Anahtar = SharedAlarmBildirimAnahtariOlustur(
                        x.Log,
                        x.Alarm!)
                })
                .ToList();

            return alarmLogBilgileri
                .GroupBy(x => x.Anahtar)
                .Select(grup =>
                {
                    /*
                     * Kamera 1 ve Kamera 1.1 birlikteyse Kamera 1'i
                     * mesaj oluşturmak için temsilci seçiyoruz.
                     */
                    var temsilci = grup
                        .OrderBy(x =>
                            x.Alarm.AlarmEslemeSozcugu?.Contains('-') == true
                                ? 1
                                : 0)
                        .ThenBy(x => x.Log.KayitZamani)
                        .First();

                    var fizikselLokasyonId =
                        temsilci.Alarm.FizikselLokasyonId;

                    string? fizikselLokasyonAdi = null;

                    if (fizikselLokasyonId.HasValue)
                    {
                        fizikselLokasyonlar.TryGetValue(
                            fizikselLokasyonId.Value,
                            out fizikselLokasyonAdi);
                    }

                    return new SharedAlarmBildirimGrubu
                    {
                        TemsilciLog = temsilci.Log,
                        TemsilciAlarm = temsilci.Alarm,

                        AlarmLoglari = grup
                            .Select(x => x.Log)
                            .ToList(),

                        FizikselLokasyonId = fizikselLokasyonId,
                        FizikselLokasyonAdi = fizikselLokasyonAdi
                    };
                })
                .ToList();
        }
        private static string FizikselLokasyonBilgisiEkle(string mesaj, string? fizikselLokasyonAdi)
        {
            if (string.IsNullOrWhiteSpace(fizikselLokasyonAdi))
                return mesaj;

            return $"{fizikselLokasyonAdi}. {mesaj}";
        }
        private static string[] EsAlarmEslemeSozcukleriniGetir(string? alarmEslemeSozcugu)
        {
            var eslemeSozcugu = alarmEslemeSozcugu?.Trim().ToUpperInvariant();

            return eslemeSozcugu switch
            {
                "DUMAN_1" or "DUMAN_1-1" =>
                    new[] { "DUMAN_1", "DUMAN_1-1" },

                "YANGIN_1" or "YANGIN_1-1" =>
                    new[] { "YANGIN_1", "YANGIN_1-1" },

                _ when !string.IsNullOrWhiteSpace(eslemeSozcugu) =>
                    new[] { eslemeSozcugu },

                _ => Array.Empty<string>()
            };
        }
        private static async Task<List<int>> AktifEsAlarmLogIdsGetirAsync(SharedDbContext sharedDbContext, vwAlarmlar alarm)
        {
            var result = new List<int>();

            if (!alarm.FizikselLokasyonId.HasValue)
                return result;

            var eslemeSozcukleri = EsAlarmEslemeSozcukleriniGetir(
                alarm.AlarmEslemeSozcugu);

            if (eslemeSozcukleri.Length == 0)
                return result;

            // Aynı fiziksel lokasyondaki Kamera 1 ve Kamera 1.1 alarm tanımları
            var esAlarmIds = await sharedDbContext.vwAlarmlar
                .AsNoTracking()
                .Where(x =>
                    x.FizikselLokasyonId == alarm.FizikselLokasyonId &&
                    eslemeSozcukleri.Contains(x.AlarmEslemeSozcugu) &&
                    x.Silindi != true)
                .Select(x => x.Id)
                .ToListAsync();

            if (esAlarmIds.Count == 0)
                return result;

            // Eş alarmlara ait açık alarm logları
            result = await sharedDbContext.AlarmLog
                .AsNoTracking()
                .Where(x =>
                    x.AlarmId.HasValue &&
                    esAlarmIds.Contains(x.AlarmId.Value) &&
                    x.Aktif == true &&
                    x.Silindi != true)
                .Select(x => x.Id)
                .ToListAsync();

            return result;
        }
        public async Task SharedAlarmGonderAsync(List<SharedApp.Domain.Models.AlarmLog> sharedAlarmLogs)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

                var alarmIds = sharedAlarmLogs.Select(x => x.AlarmId.GetValueOrDefault()).Distinct().ToList();

                var alarmGrupEslemeleri = await sharedDbContext.AlarmGrupAlarmEsleme
                    .Where(x => alarmIds.Contains(x.AlarmId.GetValueOrDefault()) && x.Silindi != true)
                    .ToListAsync();

                var alarmlar = await sharedDbContext.vwAlarmlar
                    .Where(x => alarmIds.Contains(x.Id) && x.Silindi != true)
                    .ToDictionaryAsync(x => x.Id);

                var fizikselLokasyonIds = alarmlar.Values.Where(x => x.FizikselLokasyonId.HasValue).Select(x => x.FizikselLokasyonId!.Value).Distinct().ToList();
                var fizikselLokasyonlar = await sharedDbContext.Lokasyon.AsNoTracking().Where(x => fizikselLokasyonIds.Contains(x.Id) && x.Silindi != true).ToDictionaryAsync(x => x.Id, x => x.Name);
                var bildirimGruplari = SharedAlarmBildirimGruplariniOlustur(sharedAlarmLogs, alarmlar, fizikselLokasyonlar);

                var alarmGrupIds = alarmGrupEslemeleri.Select(x => x.AlarmGrupId).Distinct().ToList();

                var kullaniciEslemeler = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.EPosta)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                var kullaniciEslemelerSesliMesaj = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.SesliMesaj)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                // Kullanıcı bazlı alarm listesi
                //var kullaniciAlarmMap = new Dictionary<string, (vwAlarmKullaniciEslemeForNotification kullanici, List<(vwAlarmlar, int, DateTime)> alarmlar)>();
                var kullaniciAlarmMap = new Dictionary<string, (vwAlarmKullaniciEslemeForNotification kullanici, List<(vwAlarmlar, int, DateTime)> alarmlar, HashSet<int> alarmLogIds)>();

                //foreach (var log in sharedAlarmLogs)
                //{
                foreach (var bildirimGrubu in bildirimGruplari)
                {
                    var log = bildirimGrubu.TemsilciLog;
                    var alarm = bildirimGrubu.TemsilciAlarm;
                    var bildirimGrubuAlarmLogIds = bildirimGrubu.AlarmLogIds;
                    //if (!alarmlar.TryGetValue(log.AlarmId ?? 0, out var alarm))
                    //    continue;

                    var ilgiliAlarmGrupIdList = alarmGrupEslemeleri
                        .Where(x => x.AlarmId == alarm.Id)
                        .Select(x => x.AlarmGrupId)
                        .Distinct()
                        .ToList();

                    var eslesenKullanicilar = kullaniciEslemeler
                        .Where(x => ilgiliAlarmGrupIdList.Contains(x.AlarmGrupId ?? 0))
                        .GroupBy(x => new { x.UserId, x.SeviyeNo })
                        .Select(g => g.First())
                        .OrderBy(x => x.SeviyeNo)
                        .ToList();

                    var seviyelereGoreKullaniciGruplari = eslesenKullanicilar
                        .GroupBy(x => x.SeviyeNo)
                        .OrderBy(x => x.Key);

                    DateTime? oncekiSeviyeGonderimZamani = null;
                    foreach (var seviyeGrubu in seviyelereGoreKullaniciGruplari)
                    {
                        var seviyeNo = seviyeGrubu.Key.GetValueOrDefault();
                        var kullanicilar = seviyeGrubu.ToList();

                        if (oncekiSeviyeGonderimZamani.HasValue &&
                            DateTime.Now < oncekiSeviyeGonderimZamani.Value)
                        {
                            continue;
                        }

                        foreach (var kullanici in kullanicilar)
                        {
                            //string match = $",{log.Id},";
                            // Mail zaten gönderildi mi kontrolü                     
                            var takipTipi = BildirimYontemi.EPosta;
                            var kaynakTuru = AlarmKaynakTuru.Shared;
                            var mailZatenGonderildiMi = await (from m in sharedDbContext.MailGonderimTakip.AsNoTracking()
                                                               join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                                                                 on m.Id equals j.TakipId
                                                               where m.UserId == kullanici.UserId
                                                                  && m.SeviyeNo == seviyeNo
                                                                  && j.TakipTipi == takipTipi
                                                                  && j.AlarmKaynakTuru == kaynakTuru
                                                                  //&& j.AlarmLogId == log.Id
                                                                  && bildirimGrubuAlarmLogIds.Contains(j.AlarmLogId)
                                                                  && ((m.GonderimBasarili ?? false) == true || !(m.KalanDenemeSayisi == 0 && (m.GonderimBasarili ?? false) == false))
                                                               select 1).AnyAsync();

                            if (mailZatenGonderildiMi)
                                continue;

                            // Zaman kontrolü
                            var bildirimZamani = log.KayitZamani?.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi) ?? DateTime.Now;
                            //if (DateTime.Now >= bildirimZamani)
                            //{
                            //    // Kullanıcıyı anahtar olarak kullan (UserId bazlı)
                            //    if (!kullaniciAlarmMap.ContainsKey(kullanici.UserId))
                            //    {
                            //        kullaniciAlarmMap[kullanici.UserId] = (kullanici, new List<(vwAlarmlar, int, DateTime)>());
                            //    }

                            //    kullaniciAlarmMap[kullanici.UserId].alarmlar.Add((alarm, log.Id, log.KayitZamani ?? DateTime.Now));

                            //    oncekiSeviyeGonderimZamani = DateTime.Now.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi);
                            //}
                            if (DateTime.Now >= bildirimZamani)
                            {
                                if (!kullaniciAlarmMap.ContainsKey(kullanici.UserId))
                                {
                                    kullaniciAlarmMap[kullanici.UserId] =
                                        (
                                            kullanici,
                                            new List<(vwAlarmlar, int, DateTime)>(),
                                            new HashSet<int>()
                                        );
                                }

                                kullaniciAlarmMap[kullanici.UserId].alarmlar.Add((alarm, log.Id, log.KayitZamani ?? DateTime.Now));

                                /*
                                 * Temsilci log dışında aynı bildirim grubundaki
                                 * diğer kamera AlarmLog ID'lerini de saklıyoruz.
                                 */
                                foreach (var alarmLogId in bildirimGrubuAlarmLogIds)
                                {
                                    kullaniciAlarmMap[kullanici.UserId].alarmLogIds.Add(alarmLogId);
                                }

                                oncekiSeviyeGonderimZamani = DateTime.Now.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi);
                            }
                        }
                    }
                }

                // Tüm kullanıcılar için e-postaları gönder
                //foreach (var (_, (kullanici, alarmlarList)) in kullaniciAlarmMap)
                //{
                //    await BildirimGonderAsync(kullanici, alarmlarList, AlarmKaynakTuru.Shared);
                //}
                foreach (var (_, deger) in kullaniciAlarmMap)
                {
                    await BildirimGonderAsync(
                        deger.kullanici,
                        deger.alarmlar,
                        AlarmKaynakTuru.Shared,
                        deger.alarmLogIds.ToList());
                }

                // ==========================
                // 2) Sesli Mesaj Bildirimleri (AlarmGrup bazlı)
                // ==========================
                foreach (var kullaniciSesliMesaj in kullaniciEslemelerSesliMesaj)
                {
                    // 1) Bu kullanıcıya ait alarm grubu
                    var ilgiliAlarmGrupId = kullaniciSesliMesaj.AlarmGrupId ?? 0;

                    // 2) Bu gruba bağlı alarmId’ler
                    var grupAlarmIds = alarmGrupEslemeleri
                        .Where(x => x.AlarmGrupId == ilgiliAlarmGrupId)
                        .Select(x => x.AlarmId.GetValueOrDefault())
                        .ToList();

                    //// 3) Loglardan bu gruba girenler
                    //var ilgiliAlarmLoglar = sharedAlarmLogs
                    //    .Where(x => x.AlarmId != null && grupAlarmIds.Contains(x.AlarmId.Value))
                    //    .ToList();

                    //if (!ilgiliAlarmLoglar.Any())
                    //    continue;

                    //var gonderilebilirLoglar = ilgiliAlarmLoglar.Where(log => { var bildirimZamani = log.KayitZamani?.Add(kullaniciSesliMesaj.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSesliMesaj.BildirimSuresi) ?? DateTime.Now; return DateTime.Now >= bildirimZamani; }).ToList();

                    //if (!gonderilebilirLoglar.Any())
                    //    continue;
                    //// 4) Sesli mesaj içeriğini oluştur
                    //var mesajlar = SesliMesajMetinleriOlusturShared(gonderilebilirLoglar, alarmlar);

                    //foreach (var mesaj in mesajlar)
                    //{
                    //    // Bu mesajın log ID’leri
                    //    var mesajAlarmLogIds = mesaj.AlarmLogIds;

                    //    // 5) Daha önce gönderilmiş mi kontrolü                     
                    //    var seviyeNo = kullaniciSesliMesaj.SeviyeNo ?? 0;
                    //    var alarmLogIdList = mesajAlarmLogIds.Distinct().ToList();

                    //    var sonGonderim = await (
                    //        from t in sharedDbContext.MesajGonderimTakip.AsNoTracking()
                    //        join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                    //            on t.Id equals j.TakipId
                    //        where t.UserId == kullaniciSesliMesaj.UserId
                    //           && t.SeviyeNo == seviyeNo
                    //           && j.TakipTipi == BildirimYontemi.SesliMesaj
                    //           && j.AlarmKaynakTuru == AlarmKaynakTuru.Shared
                    //           && alarmLogIdList.Contains(j.AlarmLogId)
                    //        orderby t.KayitZamani descending
                    //        select t
                    //    ).FirstOrDefaultAsync();

                    //    bool gonderilsinMi = false;
                    //    if (sonGonderim == null)
                    //    {
                    //        gonderilsinMi = true;
                    //    }
                    //    else if ((kullaniciSesliMesaj.TekrarlansinMi ?? false) &&
                    //             kullaniciSesliMesaj.TekrarlamaSuresi != null &&
                    //             DateTime.Now - sonGonderim.KayitZamani >= kullaniciSesliMesaj.TekrarlamaSuresi)
                    //    {
                    //        gonderilsinMi = true;
                    //    }

                    //    // 6) Gönderim yapılacaksa                     
                    //    if (gonderilsinMi && !string.IsNullOrEmpty(mesaj.Mesaj))
                    //    {
                    //        await using var tx = await sharedDbContext.Database.BeginTransactionAsync();
                    //        try
                    //        {
                    //            var takip = new MesajGonderimTakip
                    //            {
                    //                KayitZamani = DateTime.Now,
                    //                SmsIcerik = mesaj.Mesaj,
                    //                TelefonNo = kullaniciSesliMesaj.PhoneNumber,
                    //                UserId = kullaniciSesliMesaj.UserId,
                    //                SeviyeNo = kullaniciSesliMesaj.SeviyeNo ?? 0,
                    //                KalanDenemeSayisi = 10,
                    //                SesDosyaYolu = null,
                    //                AramaSure = null,
                    //                CevaplandiMi = null,
                    //                GonderimBasarili = false,
                    //                //SuHatAlarmLogIds = string.Join(",", mesaj.AlarmLogIds)
                    //            };
                    //            sharedDbContext.MesajGonderimTakip.Add(takip);
                    //            await sharedDbContext.SaveChangesAsync();

                    //            var takipId = takip.Id;
                    //            var takipTipi = BildirimYontemi.SesliMesaj;
                    //            var kaynakTuru = AlarmKaynakTuru.Shared;

                    //            var joinRows = mesaj.AlarmLogIds.Distinct().Select(alarmLogId => new MesajGonderimTakipAlarmLog
                    //            {
                    //                TakipId = takipId,
                    //                TakipTipi = takipTipi,
                    //                AlarmKaynakTuru = kaynakTuru,
                    //                AlarmLogId = alarmLogId
                    //            }).ToList();

                    //            sharedDbContext.MesajGonderimTakipAlarmLog.AddRange(joinRows);
                    //            await sharedDbContext.SaveChangesAsync();
                    //            await tx.CommitAsync();
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            await tx.RollbackAsync();
                    //        }
                    //    }
                    //}
                    var ilgiliBildirimGruplari = bildirimGruplari.Where(grup => grup.AlarmLoglari.Any(log => log.AlarmId.HasValue && grupAlarmIds.Contains(log.AlarmId.Value))).ToList();

                    if (!ilgiliBildirimGruplari.Any())
                        continue;

                    var gonderilebilirBildirimGruplari = ilgiliBildirimGruplari
                        .Where(grup =>
                        {
                            var bildirimZamani = grup.KayitZamani?.Add(
                                kullaniciSesliMesaj.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSesliMesaj.BildirimSuresi) ?? DateTime.Now;
                            return DateTime.Now >= bildirimZamani;
                        })
                        .ToList();

                    if (!gonderilebilirBildirimGruplari.Any())
                        continue;

                    foreach (var bildirimGrubu in gonderilebilirBildirimGruplari)
                    {
                        var mesajlar = SesliMesajMetinleriOlusturShared(
                            new List<SharedApp.Domain.Models.AlarmLog>
                            {
                              bildirimGrubu.TemsilciLog
                            },
                            alarmlar);
                        foreach (var mesaj in mesajlar)
                        {
                            var mesajMetni = FizikselLokasyonBilgisiEkle(
                                mesaj.Mesaj,
                                bildirimGrubu.FizikselLokasyonAdi);


                            /*
                             * Burada yalnızca temsilci log ID'si değil,
                             * Kamera 1 ve Kamera 1.1 log ID'leri birlikte kullanılır.
                             */
                            //var alarmLogIdList = bildirimGrubu.AlarmLogIds;
                            var aktifEsAlarmLogIds = await AktifEsAlarmLogIdsGetirAsync(sharedDbContext, bildirimGrubu.TemsilciAlarm);

                            var alarmLogIdList = aktifEsAlarmLogIds.Concat(bildirimGrubu.AlarmLogIds).Distinct().ToList();

                            if (alarmLogIdList.Count == 0)
                                continue;

                            var seviyeNo = kullaniciSesliMesaj.SeviyeNo ?? 0;

                            var sonGonderim = await (
                                from t in sharedDbContext.MesajGonderimTakip.AsNoTracking()
                                join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                                    on t.Id equals j.TakipId
                                where t.UserId == kullaniciSesliMesaj.UserId
                                   && t.SeviyeNo == seviyeNo
                                   && j.TakipTipi == BildirimYontemi.SesliMesaj
                                   && j.AlarmKaynakTuru == AlarmKaynakTuru.Shared
                                   && alarmLogIdList.Contains(j.AlarmLogId)
                                orderby t.KayitZamani descending
                                select t
                            ).FirstOrDefaultAsync();

                            var gonderilsinMi = sonGonderim == null;

                            if (!gonderilsinMi && (kullaniciSesliMesaj.TekrarlansinMi ?? false) && kullaniciSesliMesaj.TekrarlamaSuresi.HasValue && DateTime.Now - sonGonderim!.KayitZamani >= kullaniciSesliMesaj.TekrarlamaSuresi.Value)
                            {
                                gonderilsinMi = true;
                            }

                            if (!gonderilsinMi || string.IsNullOrWhiteSpace(mesajMetni))
                            {
                                continue;
                            }

                            await using var tx = await sharedDbContext.Database.BeginTransactionAsync();

                            try
                            {
                                var takip = new MesajGonderimTakip
                                {
                                    KayitZamani = DateTime.Now,
                                    SmsIcerik = mesajMetni,
                                    TelefonNo = kullaniciSesliMesaj.PhoneNumber,
                                    UserId = kullaniciSesliMesaj.UserId,
                                    SeviyeNo = seviyeNo,
                                    KalanDenemeSayisi = 10,
                                    SesDosyaYolu = null,
                                    AramaSure = null,
                                    CevaplandiMi = null,
                                    GonderimBasarili = false
                                };

                                sharedDbContext.MesajGonderimTakip.Add(takip);
                                await sharedDbContext.SaveChangesAsync();

                                /*
                                 * Tek arama kaydı oluşur.
                                 * Kamera 1 ve Kamera 1.1 logları aynı takip kaydına bağlanır.
                                 */
                                var joinRows = alarmLogIdList
                                    .Distinct()
                                    .Select(alarmLogId =>
                                        new MesajGonderimTakipAlarmLog
                                        {
                                            TakipId = takip.Id,
                                            TakipTipi = BildirimYontemi.SesliMesaj,
                                            AlarmKaynakTuru = AlarmKaynakTuru.Shared,
                                            AlarmLogId = alarmLogId
                                        })
                                    .ToList();

                                sharedDbContext.MesajGonderimTakipAlarmLog.AddRange(joinRows);

                                await sharedDbContext.SaveChangesAsync();
                                await tx.CommitAsync();
                            }
                            catch
                            {
                                await tx.RollbackAsync();
                                throw;
                            }
                        }
                    }
                }

                // ==========================
                // 3) SMS Bildirimleri (AlarmGrup bazlı)
                // ==========================
                var kullaniciEslemelerSms = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    .Where(x =>
                        alarmGrupIds.Contains(x.AlarmGrupId ?? 0) &&
                        (x.SeviyeAktif ?? false) &&
                        (x.Aktif ?? false) &&
                        (x.Silindi ?? false) != true &&
                        x.BildirimYontemi == BildirimYontemi.SMS)
                    .OrderBy(x => x.SeviyeNo)
                    .ToListAsync();

                foreach (var kullaniciSms in kullaniciEslemelerSms)
                {
                    var ilgiliAlarmGrupId = kullaniciSms.AlarmGrupId ?? 0;

                    var grupAlarmIds = alarmGrupEslemeleri
                        .Where(x => x.AlarmGrupId == ilgiliAlarmGrupId)
                        .Select(x => x.AlarmId.GetValueOrDefault())
                        .ToList();

                    //var ilgiliAlarmLoglar = sharedAlarmLogs.Where(x => x.AlarmId != null && grupAlarmIds.Contains(x.AlarmId.Value)).ToList();

                    //if (!ilgiliAlarmLoglar.Any())
                    //    continue;

                    //var gonderilebilirLoglar = ilgiliAlarmLoglar
                    //    .Where(log =>
                    //    {
                    //        var bildirimZamani = log.KayitZamani?.Add(
                    //            kullaniciSms.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSms.BildirimSuresi) ?? DateTime.Now;
                    //        return DateTime.Now >= bildirimZamani;
                    //    }).ToList();

                    //if (!gonderilebilirLoglar.Any())
                    //    continue;

                    //var mesajlar = SmsMetinleriOlusturShared(gonderilebilirLoglar, alarmlar);

                    //foreach (var mesaj in mesajlar)
                    //{
                    //    if (mesaj.AlarmLogIds == null || mesaj.AlarmLogIds.Count == 0)
                    //        continue;

                    //    await SmsGonderAsync(
                    //        sharedDbContext,
                    //        kullaniciSms.UserId,
                    //        kullaniciSms.SeviyeNo ?? 0,
                    //        kullaniciSms.PhoneNumber,
                    //        mesaj.Mesaj,
                    //        AlarmKaynakTuru.Shared,
                    //        mesaj.AlarmLogIds);
                    //}
                    var ilgiliBildirimGruplari = bildirimGruplari.Where(grup => grup.AlarmLoglari.Any(log => log.AlarmId.HasValue && grupAlarmIds.Contains(log.AlarmId.Value))).ToList();

                    if (!ilgiliBildirimGruplari.Any())
                        continue;

                    var gonderilebilirBildirimGruplari = ilgiliBildirimGruplari
                        .Where(grup =>
                        {
                            var bildirimZamani = grup.KayitZamani?.Add(kullaniciSms.BildirimSuresi == null ? TimeSpan.Zero : kullaniciSms.BildirimSuresi) ?? DateTime.Now;

                            return DateTime.Now >= bildirimZamani;
                        })
                        .ToList();

                    if (!gonderilebilirBildirimGruplari.Any())
                        continue;

                    foreach (var bildirimGrubu in gonderilebilirBildirimGruplari)
                    {
                        var mesaj = SmsMetinleriOlusturShared(
                                new List<SharedApp.Domain.Models.AlarmLog>
                                {
                                    bildirimGrubu.TemsilciLog
                                },
                                alarmlar)
                            .FirstOrDefault();

                        if (string.IsNullOrWhiteSpace(mesaj.Mesaj))
                            continue;

                        var smsMesaji = FizikselLokasyonBilgisiEkle(mesaj.Mesaj, bildirimGrubu.FizikselLokasyonAdi);

                        //var alarmLogIds = bildirimGrubu.AlarmLogIds;

                        //if (alarmLogIds.Count == 0)
                        //    continue;
                        var aktifEsAlarmLogIds = await AktifEsAlarmLogIdsGetirAsync(sharedDbContext, bildirimGrubu.TemsilciAlarm);

                        var alarmLogIds = aktifEsAlarmLogIds.Concat(bildirimGrubu.AlarmLogIds).Distinct().ToList();

                        if (alarmLogIds.Count == 0)
                            continue;

                        await SmsGonderAsync(sharedDbContext, kullaniciSms.UserId, kullaniciSms.SeviyeNo ?? 0, kullaniciSms.PhoneNumber, smsMesaji, AlarmKaynakTuru.Shared, alarmLogIds);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task BildirimGonderAsync(vwAlarmKullaniciEslemeForNotification kullanici, List<(vwAlarmlar alarm, int alarmLogId, DateTime kayitZamani)> alarmlar, string kaynakTuru, List<int>? alarmLogIds = null)
        //public async Task BildirimGonderAsync(vwAlarmKullaniciEslemeForNotification kullanici, List<(vwAlarmlar alarm, int alarmLogId, DateTime alarmBaslangicZamani)> alarmlar, string kaynakTuru)
        {
            var distinctAlarmlar = alarmlar.DistinctBy(x => x.alarmLogId).ToList();
            string mesaj = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; color: #333; }}
                        .container {{ padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                        .header {{ font-size: 18px; font-weight: bold; margin-bottom: 10px; }}
                        .alarm-text {{ margin-top: 15px; font-size: 16px; font-weight: bold; font-style: italic; color: #b30000; margin-bottom: 20px; }}
                        .footer {{ margin-top: 30px; font-size: 12px; color: #888; }}
                        a {{
                            margin-top: 20px;
                            padding: 10px 20px;
                            font-size: 14px;
                            text-decoration: underline;
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>Sayın {kullanici.UserFullName},</div>
                        <p>Sorumluluğunuz kapsamında aşağıdaki alarmlar oluşmuştur. Lütfen en kısa sürede inceleyiniz.</p>";

            foreach (var (alarm, alarmLogId, alarmBaslangicZamani) in distinctAlarmlar)
            {
                if (alarm.KumesId != null)
                {
                    mesaj += $"<div class='alarm-text'>- Alarm Adı: {alarm.AlarmMetni}, Başlangıç Zamanı: {alarmBaslangicZamani:dd.MM.yyyy HH:mm}, Kümes: {alarm.Yer}, Lokasyon: {alarm.LokasyonName2}</div>";
                }
                else if (alarm.LokasyonId != null)
                {
                    mesaj += $"<div class='alarm-text'>- Alarm Adı: {alarm.AlarmMetni}, Başlangıç Zamanı: {alarmBaslangicZamani:dd.MM.yyyy HH:mm}, Lokasyon: {alarm.LokasyonName2}</div>";
                }
                else if (alarm.CihazId != null)
                {
                    mesaj += $"<div class='alarm-text'>- Alarm Adı: {alarm.AlarmMetni}, Başlangıç Zamanı: {alarmBaslangicZamani:dd.MM.yyyy HH:mm}, Cihaz: {alarm.CihazName}, Bölüm: {alarm.BolumName}, Lokasyon: {alarm.LokasyonName2}</div>";
                }
                else if (alarm.KonveyorId != null)
                {
                    mesaj += $"<div class='alarm-text'>- Alarm Adı: {alarm.AlarmMetni}, Başlangıç Zamanı: {alarmBaslangicZamani:dd.MM.yyyy HH:mm}, Şirket: {alarm.SirketName}, Lokasyon: {alarm.LokasyonName2}</div>";
                }
                else
                {
                    mesaj += $"<div class='alarm-text'>- Alarm Adı: {alarm.AlarmMetni}, Başlangıç Zamanı: {alarmBaslangicZamani:dd.MM.yyyy HH:mm}</div>";
                }
            }
            mesaj += $@"
                        <div class='footer'>
                            <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlı Otomasyon Portalına Git</a><br/>
                            © {DateTime.Now.Year} Matlı Otomasyon
                        </div>
                    </div>
                </body>
                </html>";

            using var scope = _serviceProvider.CreateScope();
            var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

            await using var tx = await sharedDbContext.Database.BeginTransactionAsync();
            try
            {
                var mailAlarmLogIds = alarmLogIds is { Count: > 0 } ? alarmLogIds.Distinct().ToList() : distinctAlarmlar.Select(x => x.alarmLogId).Distinct().ToList();

                var fotografEkleri = await SonAlarmFotografBilgileriniGetirAsync(sharedDbContext, mailAlarmLogIds);
                var mailTakip = new MailGonderimTakip
                {
                    KayitZamani = DateTime.Now,
                    MailBasligi = $"Alarm Bildirimi",
                    MailIcerik = mesaj,
                    GonderilecekTo = kullanici.Email,
                    //KumesAlarmLogIds = kumesMi ? string.Join(",", distinctAlarmlar.Select(x => x.alarmLogId).ToList()) : null, // örnek bir logId, tümü tek mailde
                    //SuHatAlarmLogIds = kumesMi ? null : string.Join(",", distinctAlarmlar.Select(x => x.alarmLogId).ToList()), // örnek bir logId, tümü tek mailde
                    SeviyeNo = kullanici.SeviyeNo ?? 0,
                    UserId = kullanici.UserId,
                    KalanDenemeSayisi = 10,
                    IsBodyHtml = true,
                    EkYolu = null,
                    EkYoluKlasorMu = false,
                    EkBilgileriJson = fotografEkleri.Count > 0 ? JsonSerializer.Serialize(fotografEkleri) : null
                };
                sharedDbContext.MailGonderimTakip.Add(mailTakip);
                await sharedDbContext.SaveChangesAsync();

                //var alarmLogIdList = distinctAlarmlar.Select(x => x.alarmLogId).Distinct().ToList();
                var joinRows = mailAlarmLogIds
                              .Distinct()
                              .Select(alarmLogId =>
                                  new MesajGonderimTakipAlarmLog
                                  {
                                      TakipTipi = BildirimYontemi.EPosta,
                                      TakipId = mailTakip.Id,
                                      AlarmKaynakTuru = kaynakTuru,
                                      AlarmLogId = alarmLogId
                                  })
                              .ToList();

                sharedDbContext.MesajGonderimTakipAlarmLog.AddRange(joinRows);
                await sharedDbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
            }
        }

        private async Task SmsGonderAsync(SharedDbContext sharedDbContext, string userId, int seviyeNo, string telefonNo, string smsIcerik, string kaynakTuru, List<int> alarmLogIds)
        {
            if (string.IsNullOrEmpty(telefonNo) || string.IsNullOrEmpty(smsIcerik))
                return;

            var alarmLogIdList = alarmLogIds.Distinct().ToList();

            // Daha önce gönderildi mi kontrolü
            var zatenGonderildiMi = await (
                from t in sharedDbContext.GonderilenSms.AsNoTracking()
                join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                    on t.Id equals j.TakipId
                where t.UserId == userId
                   && t.SeviyeNo == seviyeNo
                   && j.TakipTipi == BildirimYontemi.SMS
                   && j.AlarmKaynakTuru == kaynakTuru
                   && alarmLogIdList.Contains(j.AlarmLogId)
                select 1
            ).AnyAsync();

            if (zatenGonderildiMi)
                return;

            await using var tx = await sharedDbContext.Database.BeginTransactionAsync();
            try
            {
                var smsTakip = new GonderilenSms
                {
                    KayitZamani = DateTime.Now,
                    Mesaj = smsIcerik,
                    CepTelefonu = telefonNo,
                    SeviyeNo = seviyeNo,
                    UserId = userId
                };
                sharedDbContext.GonderilenSms.Add(smsTakip);
                await sharedDbContext.SaveChangesAsync();

                var joinRows = alarmLogIdList.Select(alarmLogId => new MesajGonderimTakipAlarmLog
                {
                    TakipId = smsTakip.Id,
                    TakipTipi = BildirimYontemi.SMS,
                    AlarmKaynakTuru = kaynakTuru,
                    AlarmLogId = alarmLogId,
                }).ToList();

                sharedDbContext.MesajGonderimTakipAlarmLog.AddRange(joinRows);
                await sharedDbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
            }
        }

        private static string GetSesliMesajMetniForKumes(PoultryHouseScada.Domain.Models.AlarmLog log, Dictionary<int, vwAlarmlar> alarmlar)
        {
            vwAlarmlar? vw = null;

            if (log.AlarmId.HasValue)
                alarmlar.TryGetValue(log.AlarmId.Value, out vw);

            // Öncelik: vw.SesliMesajMetni -> vw.AlarmMetni -> log.Mesaj
            var text = (vw?.SesliMesajMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (vw?.AlarmMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (log.Mesaj ?? "").Trim();

            return text;
        }

        private static string GetSesliMesajMetniForSuHat(WaterLineScada.Domain.Models.AlarmLog log, Dictionary<int, vwAlarmlar> alarmlar)
        {
            vwAlarmlar? vw = null;
            if (log.AlarmId.HasValue)
                alarmlar.TryGetValue(log.AlarmId.Value, out vw);

            // Öncelik: vw.SesliMesajMetni -> vw.AlarmMetni -> log.Mesaj
            var text = (vw?.SesliMesajMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (vw?.AlarmMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (log.Mesaj ?? "").Trim();
            return text;
        }

        private static string GetSesliMesajMetniForEnerjiIzleme(EnergyMonitoring.Domain.Models.AlarmLog log, Dictionary<int, vwAlarmlar> alarmlar)
        {
            vwAlarmlar? vw = null;
            if (log.AlarmId.HasValue)
                alarmlar.TryGetValue(log.AlarmId.Value, out vw);

            // Öncelik: vw.SesliMesajMetni -> vw.AlarmMetni -> log.Mesaj
            var text = (vw?.SesliMesajMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (vw?.AlarmMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (log.Mesaj ?? "").Trim();
            return text;
        }

        private static string GetSesliMesajMetniForShared(SharedApp.Domain.Models.AlarmLog log, Dictionary<int, vwAlarmlar> alarmlar)
        {
            vwAlarmlar? vw = null;
            if (log.AlarmId.HasValue)
                alarmlar.TryGetValue(log.AlarmId.Value, out vw);

            // Öncelik: vw.SesliMesajMetni -> vw.AlarmMetni -> log.Mesaj
            var text = (vw?.SesliMesajMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (vw?.AlarmMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (log.Mesaj ?? "").Trim();
            return text;
        }

        private List<(string Mesaj, List<int> AlarmLogIds)> SesliMesajMetinleriOlustur(List<PoultryHouseScada.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
        {
            var mesajListesi = new List<(string Mesaj, List<int> AlarmLogIds)>();

            var oluSayisiGirMesajlar = logs.Where(x => x.Mesaj.Contains("Günlük ölü")).ToList();
            var haftasonuOluSayisiGirMesajlar = logs.Where(x => x.Mesaj.Contains("Hafta sonu ölü")).ToList();
            var digerMesajlar = logs.Where(x => !(x.Mesaj.Contains("Günlük ölü") || x.Mesaj.Contains("Hafta sonu ölü"))).ToList();

            if (oluSayisiGirMesajlar.Count() > 0)
            {
                var lokasyonBazindaGrupla = oluSayisiGirMesajlar
                    .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
                    .Where(l => alarmlar[l.AlarmId.Value].LokasyonName2 != null)
                    .GroupBy(l => new
                    {
                        Lokasyon = alarmlar[l.AlarmId.Value].LokasyonName2
                    });
                foreach (var lokasyonGrubu in lokasyonBazindaGrupla)
                {
                    var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                    var firstLog = lokasyonGrubu.First();
                    var alarmText = GetSesliMesajMetniForKumes(firstLog, alarmlar);

                    var mesaj = $"Matlı Otomasyon. Kümes Otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.Lokasyon} lokasyonunda, {alarmText}, lütfen ilgilenin.";
                    mesajListesi.Add((mesaj, alarmLogIds));
                }
            }
            if (haftasonuOluSayisiGirMesajlar.Count() > 0)
            {
                var lokasyonBazindaGrupla = haftasonuOluSayisiGirMesajlar
                    .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
                    .Where(l => alarmlar[l.AlarmId.Value].LokasyonName2 != null)
                    .GroupBy(l => new
                    {
                        Lokasyon = alarmlar[l.AlarmId.Value].LokasyonName2
                    });
                foreach (var lokasyonGrubu in lokasyonBazindaGrupla)
                {
                    var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                    var firstLog = lokasyonGrubu.First();
                    var alarmText = GetSesliMesajMetniForKumes(firstLog, alarmlar);

                    var mesaj = $"Matlı Otomasyon. Kümes Otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.Lokasyon} lokasyonunda, {alarmText}, lütfen ilgilenin.";
                    mesajListesi.Add((mesaj, alarmLogIds));
                }
            }
            // kümes bazında grupla
            var kumesBazindaGrupla = digerMesajlar
                .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
                .Where(l => alarmlar[l.AlarmId.Value].KumesId != null)
                .GroupBy(l => new
                {
                    KumesId = alarmlar[l.AlarmId.Value].KumesId,
                    Yer = alarmlar[l.AlarmId.Value].Yer,
                    Lokasyon = alarmlar[l.AlarmId.Value].LokasyonName2
                });

            foreach (var kumesGrubu in kumesBazindaGrupla)
            {
                var ilkMesaj = kumesGrubu.FirstOrDefault()?.Mesaj;
                if (ilkMesaj != null)
                {
                    var digerAlarmSayisi = Math.Max(0, kumesGrubu.Count() - 1);
                    if (digerAlarmSayisi > 0)
                    {
                        var firstLog = kumesGrubu.First();
                        var alarmText = GetSesliMesajMetniForKumes(firstLog, alarmlar);
                        var mesaj = $"Matlı Otomasyon. Kümes Otomasyonu projesi kapsamındaki {kumesGrubu.Key.Lokasyon} lokasyonunda,  {kumesGrubu.Key.Yer} için {alarmText}. Ayrıca {digerAlarmSayisi} adet başka alarm vardır, lütfen ilgilenin.";
                        var alarmLogIds = kumesGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                    else
                    {
                        var firstLog = kumesGrubu.First();
                        var alarmText = GetSesliMesajMetniForKumes(firstLog, alarmlar);
                        var mesaj = $"Matlı Otomasyon. Kümes Otomasyonu projesi kapsamındaki {kumesGrubu.Key.Lokasyon} lokasyonunda,  {kumesGrubu.Key.Yer} için {alarmText}, lütfen ilgilenin.";
                        var alarmLogIds = kumesGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                }
            }
            return mesajListesi;
        }
        private List<(string Mesaj, List<int> AlarmLogIds)> SesliMesajMetinleriOlusturSuHat(List<WaterLineScada.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
        {
            var mesajListesi = new List<(string Mesaj, List<int> AlarmLogIds)>();

            // lokasyon bazında grupla
            var lokasyonBazindaGrupla = logs
            .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
            .Where(l => alarmlar[l.AlarmId.Value].LokasyonId != null)
            .GroupBy(l => new
            {
                LokasyonId = alarmlar[l.AlarmId.Value].LokasyonId,
                LokasyonName2 = alarmlar[l.AlarmId.Value].LokasyonName2
            });

            foreach (var lokasyonGrubu in lokasyonBazindaGrupla)
            {
                var ilkMesaj = lokasyonGrubu.FirstOrDefault()?.Mesaj;
                if (ilkMesaj != null)
                {
                    var digerAlarmSayisi = Math.Max(0, lokasyonGrubu.Count() - 1);
                    if (digerAlarmSayisi > 0)
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSesliMesajMetniForSuHat(firstLog, alarmlar);
                        var mesaj = $"Matlı Otomasyon. Su izleme otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {alarmText}. Ayrıca {digerAlarmSayisi} adet başka alarm vardır, lütfen ilgilenin.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                    else
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSesliMesajMetniForSuHat(firstLog, alarmlar);
                        var mesaj = $"Matlı Otomasyon. Su izleme otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {alarmText}, lütfen ilgilenin.";
                        //var mesaj = $"Bu arama Matlı Otomasyon tarafından gerçekleştirilmektedir. Konu: Su İzleme Otomasyonu. Lokasyon: {lokasyonGrubu.Key.LokasyonName2}. Alarm: {ilkMesaj}. Lütfen ilgilenin.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                }
            }
            return mesajListesi;
        }
        private List<(string Mesaj, List<int> AlarmLogIds)> SesliMesajMetinleriOlusturYTK(List<PoultryHouseScada.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
        {
            var mesajListesi = new List<(string Mesaj, List<int> AlarmLogIds)>();

            // lokasyon bazında grupla
            var lokasyonBazindaGrupla = logs
            .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
            .Where(l => alarmlar[l.AlarmId.Value].KonveyorId != null)
            .GroupBy(l => new
            {
                SirketName = alarmlar[l.AlarmId.Value].SirketName,
                LokasyonId = alarmlar[l.AlarmId.Value].LokasyonId,
                LokasyonName2 = alarmlar[l.AlarmId.Value].LokasyonName2
            });

            foreach (var lokasyonGrubu in lokasyonBazindaGrupla)
            {
                var ilkMesaj = lokasyonGrubu.FirstOrDefault()?.Mesaj;
                if (ilkMesaj != null)
                {
                    var digerAlarmSayisi = Math.Max(0, lokasyonGrubu.Count() - 1);
                    if (digerAlarmSayisi > 0)
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSesliMesajMetniForKumes(firstLog, alarmlar);
                        var mesaj = $"Matlı Otomasyon. Yumurta Toplama Konveyörü otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.SirketName}, {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {alarmText}. Ayrıca {digerAlarmSayisi} adet başka alarm vardır, lütfen ilgilenin.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                    else
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSesliMesajMetniForKumes(firstLog, alarmlar);
                        var mesaj = $"Matlı Otomasyon. Yumurta Toplama Konveyörü otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.SirketName}, {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {alarmText}, lütfen ilgilenin.";
                        //var mesaj = $"Bu arama Matlı Otomasyon tarafından gerçekleştirilmektedir. Konu: Su İzleme Otomasyonu. Lokasyon: {lokasyonGrubu.Key.LokasyonName2}. Alarm: {ilkMesaj}. Lütfen ilgilenin.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                }
            }
            return mesajListesi;
        }
        private List<(string Mesaj, List<int> AlarmLogIds)> SesliMesajMetinleriOlusturEnerjiIzleme(List<EnergyMonitoring.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
        {
            var mesajListesi = new List<(string Mesaj, List<int> AlarmLogIds)>();

            var cihazBazindaGrupla = logs
            .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
            .Where(l => alarmlar[l.AlarmId.Value].CihazId != null)
            .GroupBy(l => new
            {
                CihazId = alarmlar[l.AlarmId.Value].CihazId,
                BolumName = alarmlar[l.AlarmId.Value].BolumName,
                LokasyonName2 = alarmlar[l.AlarmId.Value].LokasyonName2
            });

            foreach (var lokasyonGrubu in cihazBazindaGrupla)
            {
                var ilkMesaj = lokasyonGrubu.FirstOrDefault()?.Mesaj;
                if (ilkMesaj != null)
                {
                    var digerAlarmSayisi = Math.Max(0, lokasyonGrubu.Count() - 1);
                    if (digerAlarmSayisi > 0)
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSesliMesajMetniForEnerjiIzleme(firstLog, alarmlar);
                        var mesaj = $"Matlı Otomasyon. Enerji izleme otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {lokasyonGrubu.Key.BolumName} bölümünde, {alarmText}. Ayrıca {digerAlarmSayisi} adet başka alarm vardır, lütfen ilgilenin.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                    else
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSesliMesajMetniForEnerjiIzleme(firstLog, alarmlar);
                        var mesaj = $"Matlı Otomasyon. Enerji izleme otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {lokasyonGrubu.Key.BolumName} bölümünde, {alarmText}, lütfen ilgilenin.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                }
            }
            return mesajListesi;
        }
        private List<(string Mesaj, List<int> AlarmLogIds)> SesliMesajMetinleriOlusturShared(List<SharedApp.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
        {
            var mesajListesi = new List<(string Mesaj, List<int> AlarmLogIds)>();

            var validLogs = logs.Where(x => x.AlarmId.HasValue && alarmlar.ContainsKey(x.AlarmId.Value)).ToList();

            if (!validLogs.Any())
                return mesajListesi;

            var firstLog = validLogs.First();
            var alarmText = GetSesliMesajMetniForShared(firstLog, alarmlar);
            var digerAlarmSayisi = validLogs.Count - 1;
            string mesaj;

            if (digerAlarmSayisi > 0)
            {
                mesaj = $"Matlı Otomasyon. {alarmText}. Ayrıca {digerAlarmSayisi} adet başka alarm vardır, lütfen ilgilenin.";
            }
            else
            {
                mesaj = $"Matlı Otomasyon. {alarmText}, lütfen ilgilenin.";
            }

            mesajListesi.Add((
                mesaj,
                validLogs.Select(x => x.Id).ToList()
            ));
            return mesajListesi;
        }

        public async Task BasarisizAramalaraSmsGonderAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

                var zamanSiniri = DateTime.Now.AddMinutes(-5);

                // Son 5 dakika içinde Durum 2 (ulaşılamadı) veya 7 (meşgul) olan sesli arama kayıtları
                var basarisizAramalar = await sharedDbContext.MesajGonderimTakip
                    .Where(x => (x.Durum == 2 || x.Durum == 7)
                             && x.KayitZamani >= zamanSiniri)
                    .ToListAsync(cancellationToken);

                if (!basarisizAramalar.Any())
                    return;

                var takipIds = basarisizAramalar.Select(x => x.Id).ToList();

                // Bu aramalara ait alarm log eşlemeleri
                var takipAlarmLoglar = await sharedDbContext.MesajGonderimTakipAlarmLog
                    .Where(x => takipIds.Contains(x.TakipId) && x.TakipTipi == BildirimYontemi.SesliMesaj).ToListAsync(cancellationToken);

                foreach (var arama in basarisizAramalar)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var ilgiliLoglar = takipAlarmLoglar
                        .Where(x => x.TakipId == arama.Id)
                        .ToList();

                    if (!ilgiliLoglar.Any())
                        continue;

                    // Bu arama için daha önce SMS gönderildi mi?
                    //var smsZatenGonderildiMi = await (
                    //    from t in sharedDbContext.MesajGonderimTakip.AsNoTracking()
                    //    join j in sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking()
                    //        on t.Id equals j.TakipId
                    //    where t.UserId == arama.UserId
                    //       && t.SeviyeNo == arama.SeviyeNo
                    //       && j.TakipTipi == BildirimYontemi.SMS
                    //       && ilgiliLoglar.Select(x => x.AlarmKaynakTuru).Contains(j.AlarmKaynakTuru)
                    //       && ilgiliLoglar.Select(x => x.AlarmLogId).Contains(j.AlarmLogId)
                    //    select 1
                    //).AnyAsync();
                    var smsZatenGonderildiMi = await sharedDbContext.MesajGonderimTakipAlarmLog.AsNoTracking().AnyAsync(x => x.MesajGonderimTakipId == arama.Id && x.TakipTipi == BildirimYontemi.SMS, cancellationToken);

                    if (smsZatenGonderildiMi)
                        continue;

                    // Kullanıcı telefon numarasını bul
                    //var kullanici = await sharedDbContext.vwAlarmKullaniciEslemeForNotification
                    //    .Where(x => x.UserId == arama.UserId
                    //             && x.SeviyeNo == arama.SeviyeNo
                    //             && x.BildirimYontemi == BildirimYontemi.SMS
                    //             && (x.Aktif ?? false)
                    //             && (x.Silindi ?? false) != true)
                    //    .FirstOrDefaultAsync();

                    //// SMS için telefon numarası: önce Sms tanımına bak, yoksa aramanın numarasını kullan
                    //var telefonNo = kullanici?.PhoneNumber ?? arama.TelefonNo;
                    var telefonNo = arama.TelefonNo;

                    if (string.IsNullOrEmpty(telefonNo))
                        continue;

                    var smsIcerik = arama.SmsIcerik;
                    if (string.IsNullOrEmpty(smsIcerik))
                        continue;

                    await using var tx = await sharedDbContext.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        var text = smsIcerik.Replace("Matlı Otomasyon. ", "", StringComparison.OrdinalIgnoreCase).Replace("Piyelsi", "PLC", StringComparison.OrdinalIgnoreCase);
                        var smsTakip = new GonderilenSms
                        {
                            KayitZamani = DateTime.Now,
                            Mesaj = text,
                            CepTelefonu = telefonNo,
                            //UserId = arama.UserId,
                            //SeviyeNo = arama.SeviyeNo,
                            //KalanDenemeSayisi = 10,
                            //SesDosyaYolu = null,
                            //AramaSure = null,
                            //CevaplandiMi = null,
                            //GonderimBasarili = false,
                        };
                        sharedDbContext.GonderilenSms.Add(smsTakip);
                        await sharedDbContext.SaveChangesAsync(cancellationToken);

                        // Aynı alarm log eşlemelerini SMS takibine de bağla
                        var joinRows = ilgiliLoglar.Select(x => new MesajGonderimTakipAlarmLog
                        {
                            TakipId = smsTakip.Id,
                            TakipTipi = BildirimYontemi.SMS,
                            AlarmKaynakTuru = x.AlarmKaynakTuru,
                            AlarmLogId = x.AlarmLogId,
                            MesajGonderimTakipId = arama.Id
                        }).ToList();

                        sharedDbContext.MesajGonderimTakipAlarmLog.AddRange(joinRows);
                        await sharedDbContext.SaveChangesAsync(cancellationToken);
                        await tx.CommitAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        Console.WriteLine(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private List<(string Mesaj, List<int> AlarmLogIds)> SmsMetinleriOlusturForKumes(List<PoultryHouseScada.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
        {
            var mesajListesi = new List<(string Mesaj, List<int> AlarmLogIds)>();

            var oluSayisiGirMesajlar = logs.Where(x => x.Mesaj.Contains("Günlük ölü")).ToList();
            var haftasonuOluSayisiGirMesajlar = logs.Where(x => x.Mesaj.Contains("Hafta sonu ölü")).ToList();
            var digerMesajlar = logs.Where(x => !(x.Mesaj.Contains("Günlük ölü") || x.Mesaj.Contains("Hafta sonu ölü"))).ToList();

            if (oluSayisiGirMesajlar.Any())
            {
                var lokasyonBazindaGrupla = oluSayisiGirMesajlar
                    .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
                    .Where(l => alarmlar[l.AlarmId.Value].LokasyonName2 != null)
                    .GroupBy(l => new { Lokasyon = alarmlar[l.AlarmId.Value].LokasyonName2 });

                foreach (var lokasyonGrubu in lokasyonBazindaGrupla)
                {
                    var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                    var firstLog = lokasyonGrubu.First();
                    var alarmText = GetSmsMetniForKumes(firstLog, alarmlar);
                    var mesaj = $"Kümes Otomasyonu: {lokasyonGrubu.Key.Lokasyon} lokasyonunda, {alarmText}, lütfen ilgilenin. Başlangıç Zamanı: {oluSayisiGirMesajlar.First().KayitZamani:dd.MM.yyyy HH:mm}.";
                    mesajListesi.Add((mesaj, alarmLogIds));
                }
            }

            if (haftasonuOluSayisiGirMesajlar.Any())
            {
                var lokasyonBazindaGrupla = haftasonuOluSayisiGirMesajlar
                    .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
                    .Where(l => alarmlar[l.AlarmId.Value].LokasyonName2 != null)
                    .GroupBy(l => new { Lokasyon = alarmlar[l.AlarmId.Value].LokasyonName2 });

                foreach (var lokasyonGrubu in lokasyonBazindaGrupla)
                {
                    var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                    var firstLog = lokasyonGrubu.First();
                    var alarmText = GetSmsMetniForKumes(firstLog, alarmlar);
                    var mesaj = $"Kümes Otomasyonu: {lokasyonGrubu.Key.Lokasyon} lokasyonunda, {alarmText}, lütfen ilgilenin. Başlangıç Zamanı: {haftasonuOluSayisiGirMesajlar.First().KayitZamani:dd.MM.yyyy HH:mm}.";
                    mesajListesi.Add((mesaj, alarmLogIds));
                }
            }

            var kumesBazindaGrupla = digerMesajlar
                .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
                .Where(l => alarmlar[l.AlarmId.Value].KumesId != null)
                .GroupBy(l => new
                {
                    KumesId = alarmlar[l.AlarmId.Value].KumesId,
                    Yer = alarmlar[l.AlarmId.Value].Yer,
                    Lokasyon = alarmlar[l.AlarmId.Value].LokasyonName2
                });

            foreach (var kumesGrubu in kumesBazindaGrupla)
            {
                var ilkMesaj = kumesGrubu.FirstOrDefault()?.Mesaj;
                if (ilkMesaj == null) continue;

                var firstLog = kumesGrubu.First();
                var alarmText = GetSmsMetniForKumes(firstLog, alarmlar);
                var digerAlarmSayisi = Math.Max(0, kumesGrubu.Count() - 1);
                var alarmLogIds = kumesGrubu.Select(x => x.Id).ToList();

                var mesaj = digerAlarmSayisi > 0
                    ? $"Kümes Otomasyonu: {kumesGrubu.Key.Lokasyon} lokasyonunda, {kumesGrubu.Key.Yer} için {alarmText}. Ayrıca {digerAlarmSayisi} adet başka alarm vardır, lütfen ilgilenin. Başlangıç Zamanı: {firstLog.KayitZamani:dd.MM.yyyy HH:mm}."
                    : $"Kümes Otomasyonu: {kumesGrubu.Key.Lokasyon} lokasyonunda, {kumesGrubu.Key.Yer} için {alarmText}, lütfen ilgilenin. Başlangıç Zamanı: {firstLog.KayitZamani:dd.MM.yyyy HH:mm}.";

                mesajListesi.Add((mesaj, alarmLogIds));
            }

            return mesajListesi;
        }
        private List<(string Mesaj, List<int> AlarmLogIds)> SmsMetinleriOlusturSuHat(List<WaterLineScada.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
        {
            var mesajListesi = new List<(string Mesaj, List<int> AlarmLogIds)>();

            // lokasyon bazında grupla
            var lokasyonBazindaGrupla = logs
            .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
            .Where(l => alarmlar[l.AlarmId.Value].LokasyonId != null)
            .GroupBy(l => new
            {
                LokasyonId = alarmlar[l.AlarmId.Value].LokasyonId,
                LokasyonName2 = alarmlar[l.AlarmId.Value].LokasyonName2
            });

            foreach (var lokasyonGrubu in lokasyonBazindaGrupla)
            {
                var ilkMesaj = lokasyonGrubu.FirstOrDefault()?.Mesaj;
                if (ilkMesaj != null)
                {
                    var digerAlarmSayisi = Math.Max(0, lokasyonGrubu.Count() - 1);
                    if (digerAlarmSayisi > 0)
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSmsMetniForSuHat(firstLog, alarmlar);
                        var mesaj = $"Su izleme otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {alarmText}. Ayrıca {digerAlarmSayisi} adet başka alarm vardır, lütfen ilgilenin. Başlangıç Zamanı: {firstLog.KayitZamani:dd.MM.yyyy HH:mm}.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                    else
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSmsMetniForSuHat(firstLog, alarmlar);
                        var mesaj = $"Su izleme otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {alarmText}, lütfen ilgilenin. Başlangıç Zamanı: {firstLog.KayitZamani:dd.MM.yyyy HH:mm}.";
                        //var mesaj = $"Bu arama Matlı Otomasyon tarafından gerçekleştirilmektedir. Konu: Su İzleme Otomasyonu. Lokasyon: {lokasyonGrubu.Key.LokasyonName2}. Alarm: {ilkMesaj}. Lütfen ilgilenin.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                }
            }
            return mesajListesi;
        }
        private List<(string Mesaj, List<int> AlarmLogIds)> SmsMetinleriOlusturYTK(List<PoultryHouseScada.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
        {
            var mesajListesi = new List<(string Mesaj, List<int> AlarmLogIds)>();

            // lokasyon bazında grupla
            var lokasyonBazindaGrupla = logs
            .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
            .Where(l => alarmlar[l.AlarmId.Value].KonveyorId != null)
            .GroupBy(l => new
            {
                SirketName = alarmlar[l.AlarmId.Value].SirketName,
                LokasyonId = alarmlar[l.AlarmId.Value].LokasyonId,
                LokasyonName2 = alarmlar[l.AlarmId.Value].LokasyonName2
            });

            foreach (var lokasyonGrubu in lokasyonBazindaGrupla)
            {
                var ilkMesaj = lokasyonGrubu.FirstOrDefault()?.Mesaj;
                if (ilkMesaj != null)
                {
                    var digerAlarmSayisi = Math.Max(0, lokasyonGrubu.Count() - 1);
                    if (digerAlarmSayisi > 0)
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSmsMetniForKumes(firstLog, alarmlar);
                        var mesaj = $"Yumurta Toplama Konveyörü otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.SirketName}, {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {alarmText}. Ayrıca {digerAlarmSayisi} adet başka alarm vardır, lütfen ilgilenin. Başlangıç Zamanı: {firstLog.KayitZamani:dd.MM.yyyy HH:mm}.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                    else
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSmsMetniForKumes(firstLog, alarmlar);
                        var mesaj = $"Yumurta Toplama Konveyörü otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.SirketName}, {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {alarmText}, lütfen ilgilenin. Başlangıç Zamanı: {firstLog.KayitZamani:dd.MM.yyyy HH:mm}.";
                        //var mesaj = $"Bu arama Matlı Otomasyon tarafından gerçekleştirilmektedir. Konu: Su İzleme Otomasyonu. Lokasyon: {lokasyonGrubu.Key.LokasyonName2}. Alarm: {ilkMesaj}. Lütfen ilgilenin.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                }
            }
            return mesajListesi;
        }
        private List<(string Mesaj, List<int> AlarmLogIds)> SmsMetinleriOlusturEnerjiIzleme(List<EnergyMonitoring.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
        {
            var mesajListesi = new List<(string Mesaj, List<int> AlarmLogIds)>();

            var cihazBazindaGrupla = logs
            .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
            .Where(l => alarmlar[l.AlarmId.Value].CihazId != null)
            .GroupBy(l => new
            {
                CihazId = alarmlar[l.AlarmId.Value].CihazId,
                BolumName = alarmlar[l.AlarmId.Value].BolumName,
                LokasyonName2 = alarmlar[l.AlarmId.Value].LokasyonName2
            });

            foreach (var lokasyonGrubu in cihazBazindaGrupla)
            {
                var ilkMesaj = lokasyonGrubu.FirstOrDefault()?.Mesaj;
                if (ilkMesaj != null)
                {
                    var digerAlarmSayisi = Math.Max(0, lokasyonGrubu.Count() - 1);
                    if (digerAlarmSayisi > 0)
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSmsMetniForEnerjiIzleme(firstLog, alarmlar);
                        var mesaj = $"Enerji izleme otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {lokasyonGrubu.Key.BolumName} bölümünde, {alarmText}. Ayrıca {digerAlarmSayisi} adet başka alarm vardır, lütfen ilgilenin. Başlangıç Zamanı: {firstLog.KayitZamani:dd.MM.yyyy HH:mm}.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                    else
                    {
                        var firstLog = lokasyonGrubu.First();
                        var alarmText = GetSmsMetniForEnerjiIzleme(firstLog, alarmlar);
                        var mesaj = $"Enerji izleme otomasyonu projesi kapsamındaki {lokasyonGrubu.Key.LokasyonName2} lokasyonunda, {lokasyonGrubu.Key.BolumName} bölümünde, {alarmText}, lütfen ilgilenin. Başlangıç Zamanı: {firstLog.KayitZamani:dd.MM.yyyy HH:mm}.";
                        var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                }
            }
            return mesajListesi;
        }
        private List<(string Mesaj, List<int> AlarmLogIds)> SmsMetinleriOlusturShared(List<SharedApp.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
        {
            var mesajListesi = new List<(string Mesaj, List<int> AlarmLogIds)>();

            var validLogs = logs.Where(x => x.AlarmId.HasValue && alarmlar.ContainsKey(x.AlarmId.Value)).OrderBy(x => x.KayitZamani).ToList();

            if (!validLogs.Any())
                return mesajListesi;

            var firstLog = validLogs.First();
            var alarm = alarmlar[firstLog.AlarmId!.Value];
            var digerAlarmSayisi = validLogs.Count - 1;
            string mesaj;

            if (digerAlarmSayisi > 0)
            {
                mesaj =
                    $"{alarm.AlarmMetni}. Ayrıca {digerAlarmSayisi} adet başka alarm vardır, lütfen ilgilenin. " +
                    $"Başlangıç Zamanı: {firstLog.KayitZamani:dd.MM.yyyy HH:mm}.";
            }
            else
            {
                mesaj =
                    $"{alarm.AlarmMetni}, lütfen ilgilenin. " +
                    $"Başlangıç Zamanı: {firstLog.KayitZamani:dd.MM.yyyy HH:mm}.";
            }
            mesajListesi.Add((mesaj, validLogs.Select(x => x.Id).ToList()));
            return mesajListesi;
        }
        private static string GetSmsMetniForKumes(PoultryHouseScada.Domain.Models.AlarmLog log, Dictionary<int, vwAlarmlar> alarmlar)
        {
            vwAlarmlar? vw = null;

            if (log.AlarmId.HasValue)
                alarmlar.TryGetValue(log.AlarmId.Value, out vw);

            // Öncelik: vw.SesliMesajMetni -> vw.AlarmMetni -> log.Mesaj
            var text = (vw?.SesliMesajMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (vw?.AlarmMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (log.Mesaj ?? "").Trim();
            text = System.Text.RegularExpressions.Regex.Replace(text, "piyelsi", "PLC", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return text;
        }
        private static string GetSmsMetniForSuHat(WaterLineScada.Domain.Models.AlarmLog log, Dictionary<int, vwAlarmlar> alarmlar)
        {
            vwAlarmlar? vw = null;
            if (log.AlarmId.HasValue)
                alarmlar.TryGetValue(log.AlarmId.Value, out vw);

            // Öncelik: AlarmMetni  -> log.Mesaj -> SesliMesajMetni
            var text = (vw?.AlarmMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (log.Mesaj ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (vw?.SesliMesajMetni ?? "").Trim();
            text = System.Text.RegularExpressions.Regex.Replace(text, "piyelsi", "PLC", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return text;
        }
        private static string GetSmsMetniForEnerjiIzleme(EnergyMonitoring.Domain.Models.AlarmLog log, Dictionary<int, vwAlarmlar> alarmlar)
        {
            vwAlarmlar? vw = null;
            if (log.AlarmId.HasValue)
                alarmlar.TryGetValue(log.AlarmId.Value, out vw);

            // Öncelik: AlarmMetni  -> log.Mesaj -> SesliMesajMetni
            var text = (vw?.AlarmMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (log.Mesaj ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (vw?.SesliMesajMetni ?? "").Trim();
            text = System.Text.RegularExpressions.Regex.Replace(text, "piyelsi", "PLC", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return text;
        }
        private static string GetSmsMetniForShared(SharedApp.Domain.Models.AlarmLog log, Dictionary<int, vwAlarmlar> alarmlar)
        {
            vwAlarmlar? vw = null;
            if (log.AlarmId.HasValue)
                alarmlar.TryGetValue(log.AlarmId.Value, out vw);

            // Öncelik: AlarmMetni  -> log.Mesaj -> SesliMesajMetni
            var text = (vw?.AlarmMetni ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (log.Mesaj ?? "").Trim();

            if (string.IsNullOrWhiteSpace(text))
                text = (vw?.SesliMesajMetni ?? "").Trim();
            text = System.Text.RegularExpressions.Regex.Replace(text, "piyelsi", "PLC", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return text;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("AlarmBilgilendirmeService stop: {time}", DateTimeOffset.Now);
            //StopAsync metodu, veritabanı bağlantılarını düzgün bir şekilde kapatmak,
            //açılmış bağlantıları serbest bırakmak, dış hizmetlerle olan bağlantıları kapatmak veya
            //işlem tamamlanmadan önce yapılması gereken bitiş işlemleri için kullanılır.
            try
            {
                //_logger.LogInformation("AlarmBilgilendirmeServis stop: {time}", DateTimeOffset.Now);
                await UpdateServisDurumAsync("AlarmBilgilendirmeService", 3, "Servis durduruldu");
                await base.StopAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await UpdateServisDurumAsync("AlarmBilgilendirmeService", 0, ex.Message);
            }
        }

        public async Task UpdateServisDurumAsync(string servisName, byte durum, string mesaj, bool basariliMi = false)
        {
            try
            {
                mesaj = mesaj.Length > 500 ? mesaj.Substring(0, 500) : mesaj;
                using var scope = _serviceProvider.CreateScope();
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();
                var servis = await sharedDbContext.ServisKontrol.FirstOrDefaultAsync(x => x.ServisName == servisName);

                if (servis == null)
                {
                    servis = new ServisKontrol
                    {
                        ServisName = servisName
                    };

                    sharedDbContext.ServisKontrol.Add(servis);
                }

                servis.Durum = durum;
                servis.Mesaj = mesaj;
                servis.SonAktifZaman = DateTime.Now;

                if (basariliMi)
                {
                    servis.SonBasariliZaman = DateTime.Now;
                }

                await sharedDbContext.SaveChangesAsync();
            }
            catch (Exception) { }
        }

        private async Task SuresiDolanAlarmlariPasifeCekAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

                var simdi = DateTime.Now;
                var zamanAsimiSiniri =
                    simdi.AddMinutes(-15);

                /*
                 * Yalnızca AlarmEslemeSozcugu bulunan kamera sistemi
                 * alarmları pasife çekilir.
                 *
                 * Böylece diğer modüllerin AlarmLog kayıtları bu
                 * 15 dakikalık kurala dahil edilmez.
                 */
                var pasifeCekilenAlarmSayisi =
                    await sharedDbContext.AlarmLog
                        .Where(alarmLog =>
                            alarmLog.Aktif == true &&
                            alarmLog.Silindi != true &&
                            alarmLog.AlarmId.HasValue &&

                            sharedDbContext.Alarmlar.Any(alarm =>
                                alarm.Id == alarmLog.AlarmId.Value &&
                                alarm.Silindi != true &&
                                alarm.AlarmEslemeSozcugu != null &&
                                (
                                    alarm.AlarmEslemeSozcugu
                                        .StartsWith("YANGIN_") ||

                                     alarm.AlarmEslemeSozcugu
                                        .StartsWith("DUMAN_") ||

                                    alarm.AlarmEslemeSozcugu
                                        .StartsWith("VIDEOLOSS_") ||

                                    alarm.AlarmEslemeSozcugu ==
                                        "RECORDERR" ||

                                    alarm.AlarmEslemeSozcugu ==
                                        "SYSERR"
                                )) &&

                            (
                                (
                                    alarmLog.GuncellemeZamani.HasValue &&
                                    alarmLog.GuncellemeZamani.Value
                                        <= zamanAsimiSiniri
                                )
                                ||
                                (
                                    !alarmLog.GuncellemeZamani.HasValue &&
                                    alarmLog.KayitZamani.HasValue &&
                                    alarmLog.KayitZamani.Value
                                        <= zamanAsimiSiniri
                                )
                            ))
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(
                                    alarmLog => alarmLog.Aktif,
                                    false)
                                .SetProperty(
                                    alarmLog => alarmLog.PasifZamani,
                                    simdi),
                            cancellationToken);

                if (pasifeCekilenAlarmSayisi > 0)
                {
                    //_logger.LogInformation("{AlarmSayisi} adet kamera sistemi alarmı pasife çekildi.", pasifeCekilenAlarmSayisi);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Servis kapanırken normal durum.
            }
            catch (Exception exception)
            {
                //_logger.LogError(exception, "Kamera sistemi alarmları pasife çekilirken hata oluştu.");
            }
        }
    }
}