using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;
using SharedApp.Infrastructure.Sender;

namespace SharedApp.Infrastructure.Repositories
{
    public class MailGonderimTakipRepository : Repository<MailGonderimTakip>, IMailGonderimTakipRepository
    {
        private readonly SharedDbContext _context;
        private readonly EmailSender _emailSender;
        public MailGonderimTakipRepository(SharedDbContext context, EmailSender emailSender) : base(context)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task<bool> AddMailGonderimTakip(string? mailBasligi, string? mailIcerik, string? ekYolu, bool? ekYoluKlasorMu, string? hataMesaji, string? gonderilecekTo, string? gonderilecekCc, bool? isHtml)
        {
            try
            {
                if (string.IsNullOrEmpty(gonderilecekTo))
                {
                    return false;
                }
                if (gonderilecekTo.Replace(" ", "").Split(';').ToList().Count == 0)
                {
                    return false;
                }

                var dbModel = new MailGonderimTakip()
                {
                    EkYolu = ekYolu,
                    GonderimBasarili = false,
                    KalanDenemeSayisi = 3,
                    KayitZamani = DateTime.Now,
                    MailBasligi = mailBasligi,
                    MailIcerik = mailIcerik,
                    GonderilecekTo = gonderilecekTo,
                    GonderilecekCc = gonderilecekCc,
                    IsBodyHtml = isHtml,
                    EkYoluKlasorMu = ekYoluKlasorMu.GetValueOrDefault(false),
                };

                await _context.AddAsync(dbModel);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task MailKontrolEtVeGonder()
        {
            //todo basarisiz olani surekli denememek icin dakikada bir tekrar deneyecek sekilda kalan miktar eklenebilir.
            var gonderilecekler = GetAllAsyncByPredicate(p => p.GonderimBasarili == false && p.KalanDenemeSayisi > 0).ToList();

            foreach (var gonderilecek in gonderilecekler)
            {
                if (gonderilecek.SonDenemeZamani != null)
                {
                    var fark = DateTime.Now - gonderilecek.SonDenemeZamani.GetValueOrDefault();
                    if (fark.TotalSeconds < 65)
                    {
                        //aynı dk içinde mail gönderiminde hata olursa
                        continue;
                    }
                }

                try
                {
                    await _emailSender.SendEmailAsync(gonderilecek);
                    gonderilecek.GonderimBasarili = true;
                    gonderilecek.KalanDenemeSayisi = 0;
                }
                catch (System.Exception ex)
                {
                    gonderilecek.HataMesaji = ex.Message;
                    gonderilecek.GonderimBasarili = false;
                    gonderilecek.KalanDenemeSayisi -= 1;
                }
                finally
                {
                    gonderilecek.SonDenemeZamani = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
