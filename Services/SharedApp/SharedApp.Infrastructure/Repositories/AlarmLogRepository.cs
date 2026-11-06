using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;
using static SharedKernel.ApplicationConstants;

namespace SharedApp.Infrastructure.Repositories
{
    public class AlarmLogRepository : Repository<AlarmLog>, IAlarmLogRepository
    {
        private readonly SharedDbContext _context;
        public AlarmLogRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<Alarmlar?> GetByAlarmEslemeSozcuguAsync(string alarmEslemeSozcugu, CancellationToken cancellationToken = default)
        {
            return await _context.Alarmlar.AsNoTracking().FirstOrDefaultAsync(x => x.AlarmEslemeSozcugu == alarmEslemeSozcugu && x.Silindi != true, cancellationToken);
        }

        public async Task<int> AddOrUpdateAlarmLogAsync(Alarmlar alarm, CancellationToken cancellationToken = default)
        {
            if (alarm == null)
                throw new ArgumentNullException(nameof(alarm));

            var simdi = DateTime.Now;

            var mesaj =
                alarm.AlarmMetni
                ?? alarm.AlarmAd
                ?? alarm.AlarmEslemeSozcugu
                ?? "Kamera alarmı oluştu.";

            /*
             * Önce mevcut aktif kaydın ID değerini buluyoruz.
             * Tracking yapılmasına gerek yok.
             */
            var aktifAlarmLogId = await _context.AlarmLog
                .AsNoTracking()
                .Where(x =>
                    x.AlarmId == alarm.Id &&
                    x.Aktif == true &&
                    x.Silindi != true)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (aktifAlarmLogId.HasValue)
            {
                /*
                 * Kayıt sorgudan sonra pasife çekilmiş olabilir.
                 * Bu yüzden update sırasında aktiflik şartını tekrar kontrol ediyoruz.
                 */
                var guncellenenKayitSayisi = await _context.AlarmLog
                    .Where(x =>
                        x.Id == aktifAlarmLogId.Value &&
                        x.Aktif == true &&
                        x.Silindi != true)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(
                                x => x.GuncellemeZamani,
                                simdi)
                            .SetProperty(
                                x => x.Mesaj,
                                mesaj)
                            .SetProperty(
                                x => x.PasifZamani,
                                (DateTime?)null),
                        cancellationToken);

                if (guncellenenKayitSayisi > 0)
                    return aktifAlarmLogId.Value;
            }

            /*
             * Aktif kayıt bulunamadıysa yeni AlarmLog oluşturulur.
             */
            var yeniAlarmLog = new AlarmLog
            {
                AlarmId = alarm.Id,
                Mesaj = mesaj,
                KayitZamani = simdi,
                GuncellemeZamani = simdi,
                Aktif = true,
                AlarmSeviye = AlarmSeviye.Kritik,
                PasifZamani = null,
                Silindi = false
            };

            await _context.AlarmLog.AddAsync(
                yeniAlarmLog,
                cancellationToken);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);

                /*
                 * SaveChanges sonrasında identity ID doldurulmuş olur.
                 */
                return yeniAlarmLog.Id;
            }
            catch (DbUpdateException exception)
                when (IsUniqueConstraintViolation(exception))
            {
                /*
                 * Aynı anda iki istek geldiyse diğer istek aktif kaydı
                 * oluşturmuş olabilir. Başarısız ekleme nesnesini context'ten
                 * çıkarıyoruz.
                 */
                _context.Entry(yeniAlarmLog).State =
                    EntityState.Detached;

                /*
                 * Diğer isteğin oluşturduğu aktif kaydı buluyoruz.
                 */
                var mevcutAlarmLogId = await _context.AlarmLog
                    .AsNoTracking()
                    .Where(x =>
                        x.AlarmId == alarm.Id &&
                        x.Aktif == true &&
                        x.Silindi != true)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (!mevcutAlarmLogId.HasValue)
                {
                    /*
                     * Unique constraint hatası oluştuğu hâlde aktif kayıt
                     * bulunamıyorsa beklenmeyen başka bir durum vardır.
                     */
                    throw;
                }

                var tekrarGuncellenenKayitSayisi =
                    await _context.AlarmLog
                        .Where(x =>
                            x.Id == mevcutAlarmLogId.Value &&
                            x.Aktif == true &&
                            x.Silindi != true)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(
                                    x => x.GuncellemeZamani,
                                    simdi)
                                .SetProperty(
                                    x => x.Mesaj,
                                    mesaj)
                                .SetProperty(
                                    x => x.PasifZamani,
                                    (DateTime?)null),
                            cancellationToken);

                if (tekrarGuncellenenKayitSayisi == 0)
                    throw;

                return mevcutAlarmLogId.Value;
            }
        }

        //public async Task AddOrUpdateAlarmLogAsync(Alarmlar alarm, CancellationToken cancellationToken = default)
        //{
        //    var simdi = DateTime.Now;

        //    var mesaj = alarm.AlarmMetni ?? alarm.AlarmAd ?? alarm.AlarmEslemeSozcugu ?? "Kamera alarmı oluştu.";

        //    /*
        //     * Aktif kayıt varsa yeni satır eklenmez.
        //     * Sadece son güncelleme zamanı yenilenir.
        //     */
        //    var guncellenenKayitSayisi = await _context.AlarmLog
        //        .Where(x =>
        //            x.AlarmId == alarm.Id &&
        //            x.Aktif == true &&
        //            x.Silindi != true)
        //        .ExecuteUpdateAsync(
        //            setters => setters
        //                .SetProperty(
        //                    x => x.GuncellemeZamani,
        //                    simdi)
        //                .SetProperty(
        //                    x => x.Mesaj,
        //                    mesaj)
        //                .SetProperty(
        //                    x => x.PasifZamani,
        //                    (DateTime?)null),
        //            cancellationToken);

        //    if (guncellenenKayitSayisi > 0)
        //        return;

        //    var alarmLog = new AlarmLog
        //    {
        //        AlarmId = alarm.Id,
        //        Mesaj = mesaj,
        //        KayitZamani = simdi,
        //        GuncellemeZamani = simdi,
        //        Aktif = true,
        //        AlarmSeviye = AlarmSeviye.Kritik,
        //        PasifZamani = null,
        //        Silindi = false
        //    };

        //    await _context.AlarmLog.AddAsync(alarmLog, cancellationToken);

        //    try
        //    {
        //        await _context.SaveChangesAsync(cancellationToken);
        //    }
        //    catch (DbUpdateException exception)
        //        when (IsUniqueConstraintViolation(exception))
        //    {
        //        /*
        //         * Aynı anda iki istek geldiyse ilk istek kayıt açmış olabilir.
        //         * İkinci istek unique indexe takılırsa mevcut aktif kayıt
        //         * güncellenir.
        //         */

        //        _context.Entry(alarmLog).State = EntityState.Detached;

        //        //_logger.LogWarning(
        //        //    exception,
        //        //    "Aynı alarm için eş zamanlı istek geldi. AlarmId: {AlarmId}",
        //        //    alarm.Id);

        //        var tekrarGuncellenen = await _context.AlarmLog
        //            .Where(x =>
        //                x.AlarmId == alarm.Id &&
        //                x.Aktif == true &&
        //                x.Silindi != true)
        //            .ExecuteUpdateAsync(
        //                setters => setters
        //                    .SetProperty(
        //                        x => x.GuncellemeZamani,
        //                        simdi)
        //                    .SetProperty(
        //                        x => x.Mesaj,
        //                        mesaj)
        //                    .SetProperty(
        //                        x => x.PasifZamani,
        //                        (DateTime?)null),
        //                cancellationToken);

        //        if (tekrarGuncellenen == 0)
        //            throw;
        //    }
        //}

        private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            if (exception.InnerException is not SqlException sqlException)
                return false;

            return sqlException.Number is 2601 or 2627;
        }

        public async Task AddYanginAlgilamaLogAsync(YanginAlgilamaLogs log, CancellationToken cancellationToken = default)
        {
            await _context.YanginAlgilamaLogs.AddAsync(log, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}