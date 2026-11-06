using KumesScada.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure;
using SharedApp.Infrastructure.Sender;
using SuHatScada.Infrastructure;
using System.Text.RegularExpressions;
using static SharedKernel.ApplicationConstants;

namespace AlarmWorker
{
    public class AlarmBilgilendirmeServis : BackgroundService
    {
        //private readonly ILogger<AlarmBilgilendirmeServis> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly EmailSender _emailSender;

        public AlarmBilgilendirmeServis(/*ILogger<AlarmBilgilendirmeServis> logger, */IServiceProvider serviceProvider, EmailSender emailSender)
        {
            //_logger = logger;
            _serviceProvider = serviceProvider;
            _emailSender = emailSender;
            _emailSender = emailSender;
        }
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("AlarmBilgilendirmeServis working: {time}", DateTimeOffset.Now);
            await base.StartAsync(cancellationToken);
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //_logger.LogInformation("AlarmBilgilendirmeServis running at: {time}", DateTimeOffset.Now);
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var kumesDbContext = scope.ServiceProvider.GetRequiredService<KumesScadaDbContext>();
                        //Kümes aktif deðilse alarm gelmesin diye kontrol eklemek gerekiyormuþ
                        var aktifKumesler = await kumesDbContext.Kumes.Where(x => x.Aktif != false && x.Silindi != true).ToListAsync(stoppingToken);
                        var aktifKumesAlarmlar = await kumesDbContext.AlarmLog.Where(x => aktifKumesler.Select(k => k.Id).ToList().Contains(x.KumesId.GetValueOrDefault(0)) && x.Aktif != false && x.Silindi != true).ToListAsync(stoppingToken);
                        await KumesScadaAlarmGonderAsync(aktifKumesAlarmlar);

                        var suHatDbContext = scope.ServiceProvider.GetRequiredService<SuHatScadaDbContext>();
                        var aktifSuIzlemeAlarmlar = await suHatDbContext.AlarmLog.Where(x => x.Aktif != false && x.Silindi != true).ToListAsync(stoppingToken);
                        await SuHatScadaAlarmGonderAsync(aktifSuIzlemeAlarmlar);
                    }
                }
                catch (Exception)
                { }
                finally
                {
                    //_logger.LogInformation("AlarmBilgilendirmeServis completed at: {time}", DateTimeOffset.Now);
                    await Task.Delay(10000, stoppingToken);
                }
            }
        }
        public async Task KumesScadaAlarmGonderAsync(List<KumesScada.Domain.Models.AlarmLog> kumesAlarmLogs)
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
                // Kullanýcý bazlý alarm listesi
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

                        foreach (var kullanici in kullanicilar)
                        {
                            string match = $",{log.Id},";
                            // Mail zaten gönderildi mi kontrolü
                            var mailZatenGonderildiMi = await sharedDbContext.MailGonderimTakip
                                .AsNoTracking()
                                .AnyAsync(x =>
                                    x.UserId == kullanici.UserId &&
                                    ("," + x.KumesAlarmLogIds + ",").Contains(match) &&
                                    x.SeviyeNo == seviyeNo &&
                                    ((x.GonderimBasarili ?? false) == true || !(x.KalanDenemeSayisi == 0 && (x.GonderimBasarili ?? false) == false)));

                            if (mailZatenGonderildiMi)
                                continue;

                            // Zaman kontrolü
                            var bildirimZamani = log.KayitZamani?.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi) ?? DateTime.Now;
                            if (DateTime.Now >= bildirimZamani)
                            {
                                // Kullanýcýyý anahtar olarak kullan (UserId bazlý)
                                if (!kullaniciAlarmMap.ContainsKey(kullanici.UserId))
                                {
                                    kullaniciAlarmMap[kullanici.UserId] = (kullanici, new List<(vwAlarmlar, int, DateTime)>());
                                }

                                kullaniciAlarmMap[kullanici.UserId].alarmlar.Add((alarm, log.Id, log.KayitZamani ?? DateTime.Now));
                            }
                        }
                    }
                }

                // Tüm kullanýcýlar için e-postalarý gönder
                foreach (var (_, (kullanici, alarmlarList)) in kullaniciAlarmMap)
                {
                    await BildirimGonderAsync(kullanici, alarmlarList, true);
                }

                // ==========================
                // 2) Sesli Mesaj Bildirimleri (AlarmGrup bazlý)
                // ==========================
                foreach (var kullaniciSesliMesaj in kullaniciEslemelerSesliMesaj)
                {
                    // 1) Bu kullanýcýya ait alarm grubu
                    var ilgiliAlarmGrupId = kullaniciSesliMesaj.AlarmGrupId ?? 0;

                    // 2) Bu gruba baðlý alarmId’ler
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

                    // 4) Sesli mesaj içeriðini oluþtur
                    var mesajlar = SesliMesajMetinleriOlustur(gonderilebilirLoglar, alarmlar);

                    foreach (var mesaj in mesajlar)
                    {
                        var mesajAlarmLogIds = mesaj.AlarmLogIds;
                        if (mesajAlarmLogIds == null || mesajAlarmLogIds.Count == 0)
                            continue;

                        // === DÜZELTME: ANY (OR) mantýðýyla "daha önce gönderilmiþ mi" kontrolü ===
                        var idStrList = mesajAlarmLogIds.Select(id => id.ToString()).ToList();

                        var sonGonderim = (await sharedDbContext.MesajGonderimTakip.AsNoTracking().Where(x => x.UserId == kullaniciSesliMesaj.UserId && x.SeviyeNo == (kullaniciSesliMesaj.SeviyeNo ?? 0) && x.KumesAlarmLogIds != null)
                            .OrderByDescending(x => x.KayitZamani).ToListAsync()).FirstOrDefault(x => idStrList.Any(id => ("," + x.KumesAlarmLogIds + ",").Contains($",{id},")));

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

                        // 6) Gönderim yapýlacaksa
                        if (gonderilsinMi && !string.IsNullOrEmpty(mesaj.Mesaj))
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
                                KumesAlarmLogIds = string.Join(",", mesaj.AlarmLogIds)
                            };

                            sharedDbContext.MesajGonderimTakip.Add(takip);
                            await sharedDbContext.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task SuHatScadaAlarmGonderAsync(List<SuHatScada.Domain.Models.AlarmLog> suHatAlarmLogs)
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

                // Kullanýcý bazlý alarm listesi
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
                            var mailZatenGonderildiMi = await sharedDbContext.MailGonderimTakip
                                .AsNoTracking()
                                .AnyAsync(x =>
                                    x.UserId == kullanici.UserId &&
                                    ("," + x.SuHatAlarmLogIds + ",").Contains(match) &&
                                    x.SeviyeNo == seviyeNo &&
                                    ((x.GonderimBasarili ?? false) == true || !(x.KalanDenemeSayisi == 0 && (x.GonderimBasarili ?? false) == false)));

                            if (mailZatenGonderildiMi)
                                continue;

                            // Zaman kontrolü
                            var bildirimZamani = log.KayitZamani?.Add(kullanici.BildirimSuresi == null ? TimeSpan.Zero : kullanici.BildirimSuresi) ?? DateTime.Now;
                            if (DateTime.Now >= bildirimZamani)
                            {
                                // Kullanýcýyý anahtar olarak kullan (UserId bazlý)
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

                // ==========================
                // 2) Sesli Mesaj Bildirimleri (AlarmGrup bazlý)
                // ==========================
                foreach (var kullaniciSesliMesaj in kullaniciEslemelerSesliMesaj)
                {
                    // 1) Bu kullanýcýya ait alarm grubu
                    var ilgiliAlarmGrupId = kullaniciSesliMesaj.AlarmGrupId ?? 0;

                    // 2) Bu gruba baðlý alarmId’ler
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
                    // 4) Sesli mesaj içeriðini oluþtur
                    var mesajlar = SesliMesajMetinleriOlusturSuHat(gonderilebilirLoglar, alarmlar);

                    foreach (var mesaj in mesajlar)
                    {
                        // Bu mesajýn log ID’leri
                        var mesajAlarmLogIds = mesaj.AlarmLogIds;

                        // 5) Daha önce gönderilmiþ mi kontrolü
                        //var adaylar = await sharedDbContext.MesajGonderimTakip.Where(x => x.UserId == kullaniciSesliMesaj.UserId && x.SeviyeNo == kullaniciSesliMesaj.SeviyeNo && x.KumesAlarmLogIds != null).OrderByDescending(x => x.KayitZamani).ToListAsync();

                        //var sonGonderim = adaylar.FirstOrDefault(x =>
                        //{
                        //    var ids = x.KumesAlarmLogIds
                        //        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        //        .Select(idStr => int.TryParse(idStr, out var id) ? id : -1)
                        //        .ToList();

                        //    return ids.Any(id => mesajAlarmLogIds.Contains(id));
                        //});

                        IQueryable<MesajGonderimTakip> query = sharedDbContext.MesajGonderimTakip.Where(x => x.UserId == kullaniciSesliMesaj.UserId && x.SeviyeNo == kullaniciSesliMesaj.SeviyeNo && x.SuHatAlarmLogIds != null);
                        foreach (var id in mesajAlarmLogIds)
                        {
                            var idStr = id.ToString();
                            query = query.Where(x =>
                                x.SuHatAlarmLogIds.Contains($",{idStr},") ||
                                x.SuHatAlarmLogIds.StartsWith($"{idStr},") ||
                                x.SuHatAlarmLogIds.EndsWith($",{idStr}") ||
                                x.SuHatAlarmLogIds == idStr);
                        }
                        var sonGonderim = await query.OrderByDescending(x => x.KayitZamani).FirstOrDefaultAsync();
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

                        // 6) Gönderim yapýlacaksa
                        if (gonderilsinMi && !string.IsNullOrEmpty(mesaj.Mesaj))
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
                                SuHatAlarmLogIds = string.Join(",", mesaj.AlarmLogIds)
                            };

                            sharedDbContext.MesajGonderimTakip.Add(takip);
                            await sharedDbContext.SaveChangesAsync();
                        }
                    }
                }

                // Tüm kullanýcýlar için e-postalarý gönder
                foreach (var (_, (kullanici, alarmlarList)) in kullaniciAlarmMap)
                {
                    await BildirimGonderAsync(kullanici, alarmlarList, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task BildirimGonderAsync(vwAlarmKullaniciEslemeForNotification kullanici, List<(vwAlarmlar alarm, int alarmLogId, DateTime alarmBaslangicZamani)> alarmlar, bool kumesMi)
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
                        <div class='header'>Sayýn {kullanici.UserFullName},</div>
                        <p>Sorumluluðunuz kapsamýnda aþaðýdaki alarmlar oluþmuþtur. Lütfen en kýsa sürede inceleyiniz.</p>";

            foreach (var (alarm, alarmLogId, alarmBaslangicZamani) in distinctAlarmlar)
            {
                if (alarm.KumesId != null)
                {
                    mesaj += $"<div class='alarm-text'>- Alarm Adý: {alarm.AlarmMetni}, Baþlangýç Zamaný: {alarmBaslangicZamani:dd.MM.yyyy HH:mm}, Kümes: {alarm.Yer}, Lokasyon: {alarm.LokasyonName2}</div>";
                }
                if (alarm.LokasyonId != null)
                {
                    mesaj += $"<div class='alarm-text'>- Alarm Adý: {alarm.AlarmMetni}, Baþlangýç Zamaný: {alarmBaslangicZamani:dd.MM.yyyy HH:mm}, Lokasyon: {alarm.LokasyonName2}</div>";
                }
            }
            mesaj += $@"
                        <div class='footer'>
                            <a href='https://otomasyon.matli.com.tr' target='_blank'>Matlý Otomasyon Portalýna Git</a><br/>
                            © {DateTime.Now.Year} Matlý Otomasyon
                        </div>
                    </div>
                </body>
                </html>";

            using var scope = _serviceProvider.CreateScope();
            var sharedDbContext = scope.ServiceProvider.GetRequiredService<SharedDbContext>();

            // Sadece bir adet takip kaydý oluþturuyoruz, tüm alarmlarý bu mailde gönderiyoruz
            var mailTakip = new MailGonderimTakip
            {
                KayitZamani = DateTime.Now,
                MailBasligi = $"Alarm Bildirimi",
                MailIcerik = mesaj,
                GonderilecekTo = kullanici.Email,
                KumesAlarmLogIds = kumesMi ? string.Join(",", distinctAlarmlar.Select(x => x.alarmLogId).ToList()) : null, // örnek bir logId, tümü tek mailde
                SuHatAlarmLogIds = kumesMi ? null : string.Join(",", distinctAlarmlar.Select(x => x.alarmLogId).ToList()), // örnek bir logId, tümü tek mailde
                SeviyeNo = kullanici.SeviyeNo ?? 0,
                UserId = kullanici.UserId,
                KalanDenemeSayisi = 10,
                IsBodyHtml = true
            };

            sharedDbContext.MailGonderimTakip.Add(mailTakip);
            await sharedDbContext.SaveChangesAsync();

            // Mail gönderimi burada yapýlabilir (tek seferde)
            // await _emailSender.SendAlarmMessage(mesaj, new List<string> { kullanici.Email });
        }

        private List<(string Mesaj, List<int> AlarmLogIds)> SesliMesajMetinleriOlustur(List<KumesScada.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
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
                    var mesaj = $"Bu arama Matlý Otomasyon tarafýndan gerçekleþtirilmektedir. Konu: Kümes Otomasyonu. Lokasyon: {lokasyonGrubu.Key.Lokasyon}. Alarm: {oluSayisiGirMesajlar.First().Mesaj}, lütfen ilgilenin.";
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
                    var mesaj = $"Bu arama Matlý Otomasyon tarafýndan gerçekleþtirilmektedir. Konu: Kümes Otomasyonu. Lokasyon: {lokasyonGrubu.Key.Lokasyon}. Alarm: {haftasonuOluSayisiGirMesajlar.First().Mesaj}, lütfen ilgilenin.";
                    mesajListesi.Add((mesaj, alarmLogIds));
                }
            }
            // kümes bazýnda grupla
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
                    var pattern = @"(?i)(^|\s)plc(?=\s|[.,;:!?]|$)";
                    if (Regex.IsMatch(ilkMesaj, pattern))
                    {
                        ilkMesaj = Regex.Replace(ilkMesaj, pattern, "$1pi yel si");
                    }
                    if (digerAlarmSayisi > 0)
                    {
                        var mesaj = $"Bu arama Matlý Otomasyon tarafýndan gerçekleþtirilmektedir. Konu: Kümes Otomasyonu. Lokasyon: {kumesGrubu.Key.Lokasyon}. {kumesGrubu.Key.Yer}. Alarm: {ilkMesaj}. Ayrýca {digerAlarmSayisi} adet baþka alarm vardýr, lütfen ilgilenin.";
                        var alarmLogIds = kumesGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                    else
                    {
                        var mesaj = $"Bu arama Matlý Otomasyon tarafýndan gerçekleþtirilmektedir. Konu: Kümes Otomasyonu. Lokasyon: {kumesGrubu.Key.Lokasyon}. {kumesGrubu.Key.Yer}. Alarm: {ilkMesaj}. Lütfen ilgilenin.";
                        var alarmLogIds = kumesGrubu.Select(x => x.Id).ToList();
                        mesajListesi.Add((mesaj, alarmLogIds));
                    }
                }
            }
            return mesajListesi;
        }

        private List<(string Mesaj, List<int> AlarmLogIds)> SesliMesajMetinleriOlusturSuHat(List<SuHatScada.Domain.Models.AlarmLog> logs, Dictionary<int, vwAlarmlar> alarmlar)
        {
            var mesajListesi = new List<(string Mesaj, List<int> AlarmLogIds)>();

            //var tekMetinMi = logs.Select(a => a.Mesaj).Distinct().Count() == 1;           
            //var lokasyonlar = logs
            //    .Where(l => l.AlarmId.HasValue && alarmlar.ContainsKey(l.AlarmId.Value))
            //    .Select(l => alarmlar[l.AlarmId.Value].LokasyonName2)
            //    .Distinct()
            //    .ToList();
            //var tekLokasyonMu = lokasyonlar.Count == 1;

            //if (tekMetinMi && tekLokasyonMu)
            //{
            //    var alarmMetni = logs.First().Mesaj;
            //    var lokasyon = lokasyonlar.First();
            //    var mesaj = $"Bu arama Matlý Otomasyon tarafýndan gerçekleþtirilmektedir. Konu: Su Ýzleme Otomasyonu. Lokasyon: {lokasyon}. Alarm: {alarmMetni}, lütfen ilgilenin.";
            //    mesajListesi.Add((mesaj, logs.DistinctBy(x => x.Id).Select(x => x.Id).ToList()));
            //}
            //else
            {
                // lokasyon bazýnda grupla
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
                        var pattern = @"(?i)(^|\s)plc(?=\s|[.,;:!?]|$)";
                        if (Regex.IsMatch(ilkMesaj, pattern))
                        {
                            ilkMesaj = Regex.Replace(ilkMesaj, pattern, "$1pi yel si");
                        }
                        if (digerAlarmSayisi > 0)
                        {
                            var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                            var mesaj = $"Bu arama Matlý Otomasyon tarafýndan gerçekleþtirilmektedir. Konu: Su Ýzleme Otomasyonu. Lokasyon: {lokasyonGrubu.Key.LokasyonName2} Alarm: {ilkMesaj}. Ayrýca {digerAlarmSayisi} adet baþka alarm vardýr, lütfen ilgilenin.";
                            mesajListesi.Add((mesaj, alarmLogIds));
                        }
                        else
                        {
                            var mesaj = $"Bu arama Matlý Otomasyon tarafýndan gerçekleþtirilmektedir. Konu: Su Ýzleme Otomasyonu. Lokasyon: {lokasyonGrubu.Key.LokasyonName2} Alarm: {ilkMesaj}. Lütfen ilgilenin.";
                            var alarmLogIds = lokasyonGrubu.Select(x => x.Id).ToList();
                            mesajListesi.Add((mesaj, alarmLogIds));
                        }
                    }
                }
            }
            return mesajListesi;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            //_logger.LogInformation("AlarmBilgilendirmeServis stop: {time}", DateTimeOffset.Now);
            //StopAsync metodu, veritabaný baðlantýlarýný düzgün bir þekilde kapatmak,
            //açýlmýþ baðlantýlarý serbest býrakmak, dýþ hizmetlerle olan baðlantýlarý kapatmak veya
            //iþlem tamamlanmadan önce yapýlmasý gereken bitiþ iþlemleri için kullanýlýr.
            await base.StopAsync(cancellationToken);
        }
    }
}