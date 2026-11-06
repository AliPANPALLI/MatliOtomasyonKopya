using EnergyMonitoring.Domain.DbViewModels;
using EnergyMonitoring.Domain.Interfaces;
using EnergyMonitoring.Domain.Models;
using EnergyMonitoring.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;
using static MatliOtomasyon.Shared.Application.Constants.ApplicationConstants;

namespace EnergyMonitoring.Infrastructure.Repositories
{
    public class CihazRepository : Repository<Cihaz>, ICihazRepository
    {
        private EnerjiIzlemeDbContext _context;

        public CihazRepository(EnerjiIzlemeDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<vwCihaz>> GetvwCihazlarByNodeIdAndType(int nodeId, string nodeType)
        {
            switch (nodeType.ToLowerInvariant())
            {
                case TreeViewNodeTypes.Lokasyon:
                    {
                        var cihazlar = await _context.vwCihaz.AsNoTracking().Where(x => x.Silindi != true && x.LokasyonId == nodeId && x.BolumSilindi != true && x.MakineSilindi != true).ToListAsync();
                        return cihazlar.DistinctBy(x => x.Id).ToList();
                    }
                case TreeViewNodeTypes.Bolum:
                    {
                        var cihazlar = await _context.vwCihaz.AsNoTracking().Where(x => x.Silindi != true && x.BolumId == nodeId && x.MakineSilindi != true).ToListAsync();
                        return cihazlar.DistinctBy(x => x.Id).ToList();
                    }
                case TreeViewNodeTypes.Makine:
                    {
                        var cihazlar = await _context.vwCihaz.AsNoTracking().Where(x => x.Silindi != true && x.MakineId == nodeId).ToListAsync();
                        return cihazlar.DistinctBy(x => x.Id).ToList();
                    }
                case TreeViewNodeTypes.Cihaz:
                    // Sadece tek bir cihazı getir
                    var cihaz = await _context.vwCihaz.Where(x => x.Id == nodeId && x.Silindi != true).FirstOrDefaultAsync();
                    return cihaz != null ? new List<vwCihaz> { cihaz } : new List<vwCihaz>();
                default:
                    // Tanımsız nodeType
                    return new List<vwCihaz>();
            }
        }

        public async Task<List<Cihaz>> GetAllCihazByNodeIdAndType(int nodeId, string nodeType)
        {
            switch (nodeType.ToLowerInvariant())
            {
                case TreeViewNodeTypes.Lokasyon:
                    {
                        var cihazlar = await _context.Cihaz.AsNoTracking().Where(x => x.Silindi != true && x.Makine.Bolum.LokasyonId == nodeId && x.Makine.Bolum.Silindi != true && x.Makine.Silindi != true).ToListAsync();
                        return GetRootCihazlar(cihazlar);
                    }
                case TreeViewNodeTypes.Bolum:
                    {
                        var cihazlar = await _context.Cihaz.AsNoTracking().Where(x => x.Silindi != true && x.Makine.BolumId == nodeId && x.Makine.Silindi != true).ToListAsync();
                        return GetRootCihazlar(cihazlar);
                    }
                case TreeViewNodeTypes.Makine:
                    {
                        var cihazlar = await _context.Cihaz.AsNoTracking().Where(x => x.Silindi != true && x.MakineId == nodeId).ToListAsync();
                        return GetRootCihazlar(cihazlar);
                    }
                case TreeViewNodeTypes.Cihaz:
                    // Sadece tek bir cihazı getir
                    var cihaz = await _context.Cihaz.Where(x => x.Id == nodeId && x.Silindi != true).FirstOrDefaultAsync();
                    return cihaz != null ? new List<Cihaz> { cihaz } : new List<Cihaz>();
                default:
                    // Tanımsız nodeType
                    return new List<Cihaz>();
            }
        }
        private List<Cihaz> GetRootCihazlar(List<Cihaz> cihazlar)
        {
            var cihazDict = cihazlar.ToDictionary(x => x.Id, x => x);
            var rootCihazlar = new HashSet<Cihaz>();
            foreach (var cihaz in cihazlar)
            {
                var current = cihaz;
                // zincirin en tepesine çık
                while (current.ParentCihazId > 0 && cihazDict.TryGetValue(current.ParentCihazId, out var parent))
                {
                    current = parent;
                }
                // artık current en üst parent oldu
                rootCihazlar.Add(current);
            }
            return rootCihazlar.ToList();
        }

        public async Task<List<Cihaz>> GetAllCihazByNodeIdAndType2(int nodeId, string nodeType)
        {
            switch (nodeType.ToLowerInvariant())
            {
                case TreeViewNodeTypes.Lokasyon:
                    var cihazlar = await _context.Cihaz.AsNoTracking().Where(x => x.Silindi != true && x.LokasyonEnerjiHesabindaKullan == true && x.Makine.Bolum.LokasyonId == nodeId && x.Makine.Bolum.Silindi != true && x.Makine.Silindi != true).ToListAsync();
                    return cihazlar;
                case TreeViewNodeTypes.Bolum:
                    // Bölüm altındaki makineleri getir
                    cihazlar = await _context.Cihaz.AsNoTracking().Where(x => x.Silindi != true && x.ParentCihazId == 0 && x.Makine != null && !x.Makine.Silindi != true && x.Makine.BolumId == nodeId).ToListAsync();
                    return cihazlar;
                case TreeViewNodeTypes.Makine:
                    // Bu makineye bağlı cihazları getir
                    cihazlar = await _context.Cihaz.AsNoTracking().Where(x => x.Silindi != true && x.ParentCihazId == 0 && x.MakineId == nodeId).ToListAsync();
                    return cihazlar;
                case TreeViewNodeTypes.Cihaz:
                    // Sadece tek bir cihazı getir
                    var cihaz = await _context.Cihaz.Where(x => x.Id == nodeId && x.Silindi != true).FirstOrDefaultAsync();
                    return cihaz != null ? new List<Cihaz> { cihaz } : new List<Cihaz>();
                default:
                    // Tanımsız nodeType
                    return new List<Cihaz>();
            }
        }
        public async Task<List<Cihaz>> GetAllCihazByCihazIds(List<int> cihazIds)
        {
            return await _context.Cihaz.Where(x => cihazIds.Contains(x.Id)).ToListAsync();
        }

        public async Task<List<vwCihaz>> GetAllvwCihazByCihazIds(List<int> cihazIds)
        {
            return await _context.vwCihaz.Where(x => cihazIds.Contains(x.Id)).ToListAsync();
        }
        public async Task<List<vwCihaz>> GetAllAktifvwCihazByCihazIds(List<int> cihazIds)
        {
            return await _context.vwCihaz.Where(x => cihazIds.Contains(x.Id) && x.Aktif != false && x.Silindi != true).ToListAsync();
        }

        public async Task<List<vwCihaz>> GetAllCihaz(int lokasyonId)
        {
            return await _context.vwCihaz.Where(x => x.LokasyonId == lokasyonId && x.Silindi != true).ToListAsync();
        }

        public async Task<List<vwCihaz>> GetAllAktifCihaz()
        {
            return await _context.vwCihaz.Where(x => x.Aktif == true && x.Silindi != true).ToListAsync();
        }

        public async Task<List<CihazModel>> GetAllCihazModel()
        {
            return await _context.CihazModel.Where(x => x.Silindi != true).ToListAsync();
        }

        public async Task<List<Gateway>> GetAllGateway()
        {
            return await _context.Gateways/*.Where(x => x.Aktif != false)*/.ToListAsync();
        }

        public async Task AddCihaz(Cihaz cihaz)
        {
            await _context.Cihaz.AddAsync(cihaz);
            await _context.SaveChangesAsync();
        }

        public async Task<vwCihaz_Onay> GetvwCihazOnayById(int id)
        {
            return await _context.vwCihaz_Onay.Where(x => x.Id == id).FirstAsync();
        }
    }
}