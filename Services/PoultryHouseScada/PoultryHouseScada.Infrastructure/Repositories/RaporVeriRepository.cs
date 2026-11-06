using PoultryHouseScada.Domain.Interfaces;
using PoultryHouseScada.Domain.Models;
using PoultryHouseScada.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;

namespace PoultryHouseScada.Infrastructure.Repositories
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
