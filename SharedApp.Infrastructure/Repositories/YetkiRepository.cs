using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using static SharedKernel.ApplicationConstants;

namespace SharedApp.Infrastructure.Repositories
{
    public class YetkiRepository : IYetkiRepository
    {
        private readonly SharedDbContext _context;
        public YetkiRepository(SharedDbContext context)
        {
            _context = context;
        }
        public async Task<List<vwUserFormYetki>> GetUserFormYetkiListByUserId(string userId)
        {
            var userFormYetkiList = await _context.vwUserFormYetki.Where(x => x.UserId == userId && x.Silindi != true && x.FormSilindi != true && x.ModulSilindi != true).ToListAsync();
            return userFormYetkiList;
        }

        public async Task<vwUserFormYetki> GetUserFormYetkiByUserIdAndFormLink(string userId, string formLink)
        {
            var userFormYetkiList = await _context.vwUserFormYetki.Where(x => x.UserId == userId && formLink.Contains(x.ModulLink + x.FormLink) && x.Silindi != true && x.FormSilindi != true && x.ModulSilindi != true).FirstOrDefaultAsync();
            return userFormYetkiList;
        }

        public async Task<List<vwSuHatCizimSirketForYetki>> GetvwSuHatCizimSirketForYetki()
        {
            var cizimler = await _context.vwSuHatCizimSirketForYetki.AsNoTracking().OrderBy(x => x.SidebarSira).ToListAsync();
            return cizimler;
        }

        public async Task<List<Tuple<List<int>, string>>> GetPanoKimlikYetkiModelForLokasyonIds(string userId)
        {
            List<Tuple<List<int>, string>> returnModel = new List<Tuple<List<int>, string>>();
            var userMakineYetkiler = await _context.UserMakineYetki.Where(x => x.UserId.Equals(userId) && (x.Aktif ?? true) && x.Silindi != true).ToListAsync();
            var makineler = userMakineYetkiler.Where(x => x.MakineId > 0).Select(x => x.MakineId).ToList();
            var bolumler = userMakineYetkiler.Where(x => x.BolumId > 0).Select(x => x.BolumId).ToList();
            var lokasyonlar = userMakineYetkiler.Where(x => x.LokasyonId > 0).Select(x => x.LokasyonId).ToList();
            var sirketler = userMakineYetkiler.Where(x => x.SirketId > 0).Select(x => x.SirketId).ToList();

            if (makineler.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(makineler, TreeViewNodeTypes.Makine));
            if (bolumler.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(bolumler, TreeViewNodeTypes.Bolum));
            if (lokasyonlar.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(lokasyonlar, TreeViewNodeTypes.Lokasyon));
            if (sirketler.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(sirketler, TreeViewNodeTypes.Sirket));

            return returnModel;
        }

        public async Task<List<Tuple<List<int>, string>>> GetKumesScadaYetkiModelForLokasyonIds(string userId)
        {
            List<Tuple<List<int>, string>> returnModel = new List<Tuple<List<int>, string>>();
            var userKumesYetkiler = await _context.UserKumesYetki.Where(x => x.UserId.Equals(userId) && (x.Aktif ?? true) && x.Silindi != true).ToListAsync();
            var kumesler = userKumesYetkiler.Where(x => x.KumesId > 0).Select(x => x.KumesId).ToList();
            var lokasyonlar = userKumesYetkiler.Where(x => x.LokasyonId > 0).Select(x => x.LokasyonId).ToList();
            var sirketler = userKumesYetkiler.Where(x => x.SirketId > 0).Select(x => x.SirketId).ToList();


            if (kumesler.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(kumesler, KumesHiyerarsi.Kumes));
            if (lokasyonlar.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(lokasyonlar, KumesHiyerarsi.Lokasyon));
            if (sirketler.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(sirketler, KumesHiyerarsi.Sirket));

            return returnModel;
        }

        public async Task<List<Tuple<List<int>, string>>> GetSuHatScadaYetkiModelForLokasyonIds(string userId)
        {
            List<Tuple<List<int>, string>> returnModel = new List<Tuple<List<int>, string>>();
            var userSuKaynakYetkiler = await _context.UserSuKaynakYetki.Where(x => x.UserId.Equals(userId) && (x.Aktif ?? true) && x.Silindi != true).ToListAsync();
            var suKaynaklar = userSuKaynakYetkiler.Where(x => x.SuKaynakId > 0).Select(x => x.SuKaynakId).ToList();
            var lokasyonlar = userSuKaynakYetkiler.Where(x => x.LokasyonId > 0).Select(x => x.LokasyonId).ToList();
            var cizimler = userSuKaynakYetkiler.Where(x => x.CizimId > 0).Select(x => x.CizimId).ToList();
            var sirketler = userSuKaynakYetkiler.Where(x => x.SirketId > 0).Select(x => x.SirketId).ToList();

            if (suKaynaklar.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(suKaynaklar, SuKaynakHiyerarsi.SuKaynak));
            if (lokasyonlar.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(lokasyonlar, SuKaynakHiyerarsi.Lokasyon));
            if (cizimler.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(cizimler, SuKaynakHiyerarsi.Cizim));
            if (sirketler.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(sirketler, SuKaynakHiyerarsi.Sirket));

            return returnModel;
        }

        public async Task<List<Tuple<List<int>, string>>> GetEnerjiIzlemeYetkiModelForLokasyonIds(string userId)
        {
            List<Tuple<List<int>, string>> returnModel = new List<Tuple<List<int>, string>>();
            var userEnerjiIzlemeYetkiler = await _context.UserEnerjiIzlemeYetki.Where(x => x.UserId.Equals(userId) && (x.Aktif ?? true) && x.Silindi != true).ToListAsync();
            var lokasyonlar = userEnerjiIzlemeYetkiler.Where(x => x.LokasyonId > 0).Select(x => x.LokasyonId).ToList();
            var sirketler = userEnerjiIzlemeYetkiler.Where(x => x.SirketId > 0).Select(x => x.SirketId).ToList();

            if (lokasyonlar.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(lokasyonlar, EnerjiIzlemeHiyerarsi.Lokasyon));
            if (sirketler.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(sirketler, EnerjiIzlemeHiyerarsi.Sirket));

            return returnModel;
        }

        public async Task<List<Tuple<List<int>, string>>> GetSunucuScadaYetkiModelForLokasyonIds(string userId)
        {
            List<Tuple<List<int>, string>> returnModel = new List<Tuple<List<int>, string>>();
            var userEenrjiIzlemeYetkiler = await _context.UserEnerjiIzlemeYetki.Where(x => x.UserId.Equals(userId) && (x.Aktif ?? true) && x.Silindi != true).ToListAsync();
            var lokasyonlar = userEenrjiIzlemeYetkiler.Where(x => x.LokasyonId > 0).Select(x => x.LokasyonId).ToList();
            var sirketler = userEenrjiIzlemeYetkiler.Where(x => x.SirketId > 0).Select(x => x.SirketId).ToList();

            if (lokasyonlar.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(lokasyonlar, EnerjiIzlemeHiyerarsi.Lokasyon));
            if (sirketler.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(sirketler, EnerjiIzlemeHiyerarsi.Sirket));

            return returnModel;
        }

        public async Task<List<UserLinkYetki>> GetUserLinkYetkiByUserId(string userId)
        {
            return await _context.Set<UserLinkYetki>().AsNoTracking().Where(x => x.UserId == userId && x.Silindi != true && x.Aktif != false).ToListAsync();
        }

        public async Task<List<vwUserKumesYetki>> GetUserKumesYetkiForSendingMail()
        {
            return await _context.vwUserKumesYetki.Where(x => (x.Aktif ?? true) && x.Silindi != true && x.UserSilindi != true).ToListAsync();
        }

        public async Task<List<vwUserSuKaynakYetki>> GetUserSuKaynakYetkiForSendingMail()
        {
            return await _context.vwUserSuKaynakYetki.Where(x => (x.Aktif ?? true) && x.Silindi != true && x.UserSilindi != true).ToListAsync();
        }

        public async Task<List<vwUserMakineYetki>> GetUserMakineYetkiForSendingMail()
        {
            return await _context.vwUserMakineYetki.Where(x => (x.Aktif ?? true) && x.Silindi != true && x.UserSilindi != true).ToListAsync();
        }

        public async Task<List<Tuple<List<int>, string>>> GetEnerjiIzlemeYetkiModelOnaylayabilirForLokasyonIds(string userId)
        {
            List<Tuple<List<int>, string>> returnModel = new List<Tuple<List<int>, string>>();
            var userEnerjiIzlemeYetkiler = await _context.UserEnerjiIzlemeYetki.Where(x => x.UserId.Equals(userId) && (x.Aktif ?? true) && x.Onaylayabilir == true && x.Silindi != true).ToListAsync();
            var lokasyonlar = userEnerjiIzlemeYetkiler.Where(x => x.LokasyonId > 0).Select(x => x.LokasyonId).ToList();
            var sirketler = userEnerjiIzlemeYetkiler.Where(x => x.SirketId > 0).Select(x => x.SirketId).ToList();

            if (lokasyonlar.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(lokasyonlar, EnerjiIzlemeHiyerarsi.Lokasyon));
            if (sirketler.Count() > 0)
                returnModel.Add(new Tuple<List<int>, string>(sirketler, EnerjiIzlemeHiyerarsi.Sirket));

            return returnModel;
        }

        public async Task<UserLinkYetki?> GetUserLinkYetkiByUserIdAndLink(string userId, string link)
        {
            var userLinkYetki = await _context.Set<UserLinkYetki>().AsNoTracking().Where(x => x.UserId == userId && x.Link == link && x.Silindi != true && x.Aktif != false).FirstOrDefaultAsync();
            return userLinkYetki;
        }
    }
}