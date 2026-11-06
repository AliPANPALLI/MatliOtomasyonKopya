using Microsoft.EntityFrameworkCore;
using ObjectMonitoring.Domain.DbViewModels;
using ObjectMonitoring.Domain.Entities;
using ObjectMonitoring.Domain.Interfaces;
using ObjectMonitoring.Domain.Models.ReportModels;
using System.Text.Json;

namespace ObjectMonitoring.Infrastructure.Repositories
{
    public class DebimetreRepository : IDebimetreRepository
    {
        private readonly NesneIzlemeDbContext _context;
        public DebimetreRepository(NesneIzlemeDbContext context)
        {
            _context = context;
        }

        public async Task OzetGuncelleAsync(byte periyotTipi, DateTime baslangicZamani, DateTime bitisZamani, CancellationToken cancellationToken = default)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"
            EXEC dbo.sp_DebimetreOzetGuncelle
                @PeriyotTipi = {periyotTipi},
                @BaslangicZamani = {baslangicZamani},
                @BitisZamani = {bitisZamani}",
                    cancellationToken);
        }

        public async Task<List<DebimetreRaporVeriModel>> GetDebimetreRaporVerileriAsync(List<int> lokasyonIds, DateTime baslangicZamani, DateTime bitisZamani, string raporlamaTip, CancellationToken cancellationToken = default)
        {
            if (lokasyonIds == null || lokasyonIds.Count == 0)
                return new List<DebimetreRaporVeriModel>();

            var lokasyonIdsJson = JsonSerializer.Serialize(lokasyonIds.Distinct());

            return await _context.Set<DebimetreRaporVeriModel>().FromSqlInterpolated($@"
            EXEC dbo.sp_DebimetreRaporGetir
                @LokasyonIdsJson = {lokasyonIdsJson},
                @BaslangicZamani = {baslangicZamani},
                @BitisZamani = {bitisZamani},
                @RaporlamaTip = {raporlamaTip}")
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Debimetre>> GetAktifDebimetrelerByLokasyonIdsAsync(List<int> lokasyonIds, CancellationToken cancellationToken = default)
        {
            return await _context.Debimetre
                .AsNoTracking()
                .Where(x =>
                    lokasyonIds.Contains(x.LokasyonId) &&
                    x.Aktif &&
                    !x.Silindi)
                .OrderBy(x => x.LokasyonId)
                .ThenBy(x => x.RaporSira)
                .ThenBy(x => x.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<vwLokasyon>> GetLokasyonlarFromLokasyonIds(List<int> lokasyonIds)
        {
            return await _context.vwLokasyon.Where(x => lokasyonIds.Contains(x.Id)).ToListAsync();
        }
    }
}