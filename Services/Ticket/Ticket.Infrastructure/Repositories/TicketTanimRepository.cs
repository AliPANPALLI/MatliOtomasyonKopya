using Microsoft.EntityFrameworkCore;
using Ticket.Domain.DbViewModels;
using Ticket.Domain.Interfaces;
using Ticket.Domain.Models;
using Ticket.Infrastructure.Repositories.Core;

namespace Ticket.Infrastructure.Repositories
{
    public class TicketTanimRepository : Repository<TicketTanim>, ITicketTanimRepository
    {
        private readonly TicketDbContext _context;
        public TicketTanimRepository(TicketDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<List<vwTicketAciklamaEsleme>> GetAllvwTicketAciklamaEsleme()
        {
            return await _context.vwTicketAciklamaEsleme.ToListAsync();
        }
        public async Task<List<ParaBirimi>> GetAllParaBirimis()
        {
            return await _context.ParaBirimi.ToListAsync();
        }
        //public async Task<List<vwTicketDurumLog>> GetAllvwTicketDurumLogs()
        //{
        //    return await _context.vwTicketDurumLog.ToListAsync();
        //}
        public async Task<List<vwTicketDurumLog>> GetAllvwTicketDurumLogsByTicketId(int ticketId)
        {
            return await _context.vwTicketDurumLog.Where(x => x.TicketId == ticketId).ToListAsync();
        }

        public async Task<List<vwTicketTeklifFiyatLog>> GetAllvwTicketTeklifFiyatLogs(int ticketId)
        {
            return await _context.vwTicketTeklifFiyatLog.Where(x => x.TicketId == ticketId).ToListAsync();
        }

        public async Task<List<vwTedarikciUser>> GetAllvwTedarikciUser()
        {
            return await _context.vwTedarikciUser.Where(x => x.Silindi != true).ToListAsync();
        }
        public async Task<List<vwTicketTanim>> GetAllvwTicketTanim()
        {
            return await _context.vwTicketTanim.Where(x => x.Silindi != true).AsNoTracking().ToListAsync();
        }
        public async Task<List<vwTicketTanim>> GetAllFilteredvwTicketTanimByTedarikciId(int tedarikciId)
        {
            return await _context.vwTicketTanim.Where(x => x.TedarikciId == tedarikciId && x.Silindi != true).AsNoTracking().ToListAsync();
        }
        public async Task<List<vwTicketTanim>> GetAllFilteredvwTicketTanim(DateTime baslangicZamani, DateTime bitisZamani, int? tedarikciId)
        {
            List<vwTicketTanim> list = new();
            List<int> ticketIds = new();
            var durumDegisenler = await _context.TicketDurumLog.AsNoTracking().Where(x => x.KayitZamani.Date >= baslangicZamani.Date && x.KayitZamani.Date <= bitisZamani.Date).Select(x => x.TicketId).ToListAsync();
            var aciklamaEklenenler = await _context.TicketAciklamaEsleme.AsNoTracking().Where(x => x.KayitZamani.Value.Date >= baslangicZamani && x.KayitZamani.Value.Date <= bitisZamani).Select(x => x.TicketId).ToListAsync();
            ticketIds.AddRange(durumDegisenler);
            ticketIds.AddRange(aciklamaEklenenler);
            if (tedarikciId == null)
            {
                var guncellenenler = await _context.vwTicketTanim.Where(x => (x.GuncellemeZamani.Value.Date >= baslangicZamani.Date && x.GuncellemeZamani.Value.Date <= bitisZamani.Date) || ticketIds.Contains(x.Id)).Where(x => x.Silindi != true).AsNoTracking().ToListAsync();
                list.AddRange(guncellenenler);
            }
            else
            {
                var guncellenenler = await _context.vwTicketTanim.Where(x => x.TedarikciId == tedarikciId.GetValueOrDefault(0) && ((x.GuncellemeZamani.Value.Date >= baslangicZamani.Date && x.GuncellemeZamani.Value.Date <= bitisZamani.Date) || ticketIds.Contains(x.Id))).Where(x => x.Silindi != true).AsNoTracking().ToListAsync();
                list.AddRange(guncellenenler);
            }
            return list;
        }

        public IQueryable<vwTicketTanim> GetvwTicketTanimQueryable(int? tedarikciId, DateTime? baslangicZamani, DateTime? bitisZamani)
        {
            var query = _context.vwTicketTanim.AsNoTracking().Where(x => x.Silindi != true);

            if (tedarikciId != null)
                query = query.Where(x => x.TedarikciId == tedarikciId.Value);

            if (baslangicZamani != null && bitisZamani != null)
            {
                var bas = baslangicZamani.Value.Date;
                var bit = bitisZamani.Value.Date;

                var durumDegisenler = _context.TicketDurumLog
                    .Where(x => x.KayitZamani.Date >= bas && x.KayitZamani.Date <= bit)
                    .Select(x => x.TicketId);

                var aciklamaEklenenler = _context.TicketAciklamaEsleme
                    .Where(x => x.KayitZamani.Value.Date >= bas && x.KayitZamani.Value.Date <= bit)
                    .Select(x => x.TicketId);

                var degisenTicketIds = durumDegisenler.Union(aciklamaEklenenler);

                query = query.Where(x =>
                    (x.GuncellemeZamani.Value.Date >= bas && x.GuncellemeZamani.Value.Date <= bit)
                    || degisenTicketIds.Contains(x.Id));
            }

            return query;
        }

        public async Task<vwTicketTanim> GetvwTicketTanimById(int ticketId)
        {
            return await _context.vwTicketTanim.Where(x => x.Id == ticketId).FirstOrDefaultAsync();
        }
        public async Task AddTicketDurumLog(int ticketId, int durumIdOncesi, int durumIdSonrasi, bool tedarikciMi, int? userId, string? matliUserId)
        {
            TicketDurumLog ticketDurumLog = new TicketDurumLog();
            ticketDurumLog.TicketId = ticketId;
            ticketDurumLog.UserId = userId;
            ticketDurumLog.KayitZamani = DateTime.Now;
            ticketDurumLog.MatliUserId = matliUserId;
            if (!tedarikciMi)
            {
                ticketDurumLog.DurumIdOncesi = durumIdOncesi;
                ticketDurumLog.DurumIdSonrasi = durumIdSonrasi;
            }
            else if (tedarikciMi)
            {
                ticketDurumLog.DurumIdTedarikciOncesi = durumIdOncesi;
                ticketDurumLog.DurumIdTedarikciSonrasi = durumIdSonrasi;
            }

            await _context.TicketDurumLog.AddAsync(ticketDurumLog);
            await _context.SaveChangesAsync();
        }

        public async Task AddTicketTeklifFiyatLog(int ticketId, decimal teklifFiyatOncesi, int teklifParaBirimiOncesi, decimal teklifFiyatSonrasi, int teklifParaBirimiSonrasi, int? userId, string? matliUserId)
        {
            TicketTeklifFiyatLog ticketTeklifFiyatLog = new TicketTeklifFiyatLog();
            ticketTeklifFiyatLog.TicketId = ticketId;
            ticketTeklifFiyatLog.UserId = userId;
            ticketTeklifFiyatLog.KayitZamani = DateTime.Now;
            ticketTeklifFiyatLog.MatliUserId = matliUserId;
            ticketTeklifFiyatLog.TeklifFiyatOncesi = teklifFiyatOncesi;
            ticketTeklifFiyatLog.TeklifParaBirimiIdOncesi = teklifParaBirimiOncesi;
            ticketTeklifFiyatLog.TeklifFiyatSonrasi = teklifFiyatSonrasi;
            ticketTeklifFiyatLog.TeklifParaBirimiIdSonrasi = teklifParaBirimiSonrasi;

            await _context.TicketTeklifFiyatLog.AddAsync(ticketTeklifFiyatLog);
            await _context.SaveChangesAsync();
        }

        public async Task AddTicketAciklamaEsleme(int ticketId, string aciklama, string loginUserId, int? userId, string? matliUserId)
        {
            TicketAciklamaEsleme ticketAciklamaEsleme = new TicketAciklamaEsleme();
            ticketAciklamaEsleme.TicketId = ticketId;
            ticketAciklamaEsleme.Aciklama = aciklama;
            ticketAciklamaEsleme.UserId = userId;
            ticketAciklamaEsleme.MatliUserId = matliUserId;
            ticketAciklamaEsleme.LoginUserId = loginUserId;
            ticketAciklamaEsleme.KayitZamani = DateTime.Now;
            await _context.TicketAciklamaEsleme.AddAsync(ticketAciklamaEsleme);
            await _context.SaveChangesAsync();
        }

        public async Task<List<vwTedarikciUser>> GetSorumlularForMailByTedarikciId(int tedarikciId)
        {
            return await _context.vwTedarikciUser.Where(x => x.TedarikciId == tedarikciId && x.Silindi != true).ToListAsync();
        }

        public async Task<List<vwTicketMatliTakipciEsleme>> GetTakipcilerForMailByTicketId(int ticketId)
        {
            return await _context.vwTicketMatliTakipciEsleme.Where(x => x.TicketId == ticketId && x.Silindi != true).ToListAsync();
        }

        public async Task<List<vwTicketAciklamaEsleme>> GetAllvwTicketAciklamaEslemeByTicketId(int ticketId)
        {
            return await _context.vwTicketAciklamaEsleme.Where(x => x.TicketId == ticketId && x.MatliUserId != null).ToListAsync();
        }

        public async Task<List<vwTicketAciklamaEsleme>> GetAllTakipciAciklamalarByTicketId(int ticketId)
        {
            return await _context.vwTicketAciklamaEsleme.Where(x => x.TicketId == ticketId && x.MatliUserId != null).ToListAsync();
        }

        public async Task<List<vwTicketAciklamaEsleme>> GetAllSorumluAciklamalarByTicketId(int ticketId)
        {
            return await _context.vwTicketAciklamaEsleme.Where(x => x.TicketId == ticketId && x.UserId != null).ToListAsync();
        }

        public async Task<List<vwTicketAciklamaEsleme>> GetAllvwTicketAciklamaEslemeByTicketIds(List<int> ticketIds)
        {
            return await _context.vwTicketAciklamaEsleme.Where(x => ticketIds.Contains(x.TicketId) && x.MatliUserId != null).ToListAsync();
        }

        public async Task<List<int>> GetTicketIdsWithAciklamaByTicketIds(List<int> ticketIds)
        {
            return await _context.vwTicketAciklamaEsleme.Where(x => ticketIds.Contains(x.TicketId)).Select(x => x.TicketId).Distinct().ToListAsync();
        }

        public async Task<vwTedarikciUser> GetUserSorumluMuByLoginUserId(string loginUserId)
        {
            return await _context.vwTedarikciUser.Where(x => x.Silindi != true && x.LoginUserId == loginUserId).FirstOrDefaultAsync();
        }

        public async Task<bool> GetTakipciUserKisitliYetki(string loginUserId)
        {
            var takipciUser = await _context.TakipciUser.Where(x => x.Silindi != true && x.LoginUserId == loginUserId).FirstOrDefaultAsync();
            return takipciUser == null ? false : takipciUser.SadeceAciklamaYapar.GetValueOrDefault(false);
        }
        public async Task<bool> GetTakipciUserYetkisiz(string loginUserId)
        {
            var takipciUser = await _context.TakipciUser.Where(x => x.Silindi != true && x.LoginUserId == loginUserId).FirstOrDefaultAsync();
            return takipciUser == null ? false : takipciUser.SadeceGoruntuler.GetValueOrDefault(false);
        }

        public async Task<vwTedarikciUser> GetvwTedarikciUserForMatliUser(string loginUserId, int tedarikciId)
        {
            return await _context.vwTedarikciUser.Where(x => x.TedarikciId == tedarikciId && x.Silindi != true && x.LoginUserId == loginUserId).FirstOrDefaultAsync();
        }
    }
}