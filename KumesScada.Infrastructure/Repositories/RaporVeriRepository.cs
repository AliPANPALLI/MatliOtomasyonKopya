using KumesScada.Domain.Interfaces;
using KumesScada.Domain.Models;
using KumesScada.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;

namespace KumesScada.Infrastructure.Repositories
{
    public class RaporVeriRepository : Repository<RaporVeri>, IRaporVeriRepository
    {
        private readonly KumesScadaDbContext _context;

        public RaporVeriRepository(KumesScadaDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<RaporVeri>> GetByKategoriAdi(string kategoriAdi)
        {
            return await _context.RaporVerileri
                .Include(x => x.RaporKategori)
                .Where(x => x.RaporKategori.RaporKategoriAd == kategoriAdi)
                .OrderBy(x => x.HaftaSayisi)
                .ToListAsync();
        }
    }
}
