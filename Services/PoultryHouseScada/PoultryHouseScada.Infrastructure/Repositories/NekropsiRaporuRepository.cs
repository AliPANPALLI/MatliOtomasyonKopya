using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;

namespace PoultryHouseScada.Infrastructure.Repositories
{
    public class NekropsiRaporuRepository : Repository<NekropsiRaporu>, INekropsiRaporuRepository
    {
        private readonly KumesScadaDbContext _context;

        public NekropsiRaporuRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<NekropsiRaporu>> GetAllWithDetailsByKumesIdAsync(int kumesId)
        {
            return await _context.NekropsiRaporlari
                .AsNoTracking()
                .Where(x => x.KumesId == kumesId && x.Silindi != true)
                .Include(x => x.Bulgular)
                .Include(x => x.Fotograflar)
                .OrderByDescending(x => x.Tarih)
                .ThenByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<NekropsiRaporu?> GetWithDetailsByIdAsync(int id)
        {
            return await _context.NekropsiRaporlari
                .AsNoTracking()
                .Include(x => x.Bulgular)
                .Include(x => x.Fotograflar)
                .FirstOrDefaultAsync(x => x.Id == id && x.Silindi != true);
        }

        public async Task<NekropsiFotograf?> GetFotografByIdAsync(int fotografId)
        {
            return await _context.NekropsiFotograflari
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == fotografId);
        }

        // Rapor alanlarını ve bulguları TEK bir değişiklik olarak günceller —
        // bulgu listesi tamamen yeni gönderilen listeyle değiştirilir (eski
        // bulgu satırları silinip yenileri eklenir), fotoğraflara dokunulmaz.
        public async Task GuncelleAsync(NekropsiRaporu guncellenecekRapor, List<NekropsiBulgu> yeniBulgular)
        {
            var mevcut = await _context.NekropsiRaporlari
                .Include(x => x.Bulgular)
                .FirstOrDefaultAsync(x => x.Id == guncellenecekRapor.Id && x.Silindi != true);

            if (mevcut == null)
                throw new InvalidOperationException($"{guncellenecekRapor.Id} id'li nekropsi raporu bulunamadı.");

            mevcut.Tarih = guncellenecekRapor.Tarih;
            mevcut.Hafta = guncellenecekRapor.Hafta;
            mevcut.NekropsiSebebi = guncellenecekRapor.NekropsiSebebi;
            mevcut.Pcr = guncellenecekRapor.Pcr;

            _context.NekropsiBulgulari.RemoveRange(mevcut.Bulgular);

            foreach (var bulgu in yeniBulgular)
            {
                bulgu.NekropsiRaporuId = mevcut.Id;
                _context.NekropsiBulgulari.Add(bulgu);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<NekropsiFotograf?> FotografSilAsync(int fotografId)
        {
            var fotograf = await _context.NekropsiFotograflari.FirstOrDefaultAsync(x => x.Id == fotografId);

            if (fotograf == null)
                return null;

            _context.NekropsiFotograflari.Remove(fotograf);
            await _context.SaveChangesAsync();

            return fotograf;
        }
    }
}
