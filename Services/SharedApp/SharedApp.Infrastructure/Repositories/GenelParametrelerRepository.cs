using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class GenelParametrelerRepository : Repository<GenelParametreler>, IGenelParametrelerRepository
    {
        private readonly SharedDbContext _context;
        public GenelParametrelerRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<GenelParametreler?> GetGenelParametrelerDegerByName(string genelParametreName)
        {
            var genelParametre = await _context.GenelParametreler.Where(x => x.Name == genelParametreName).FirstOrDefaultAsync();
            return genelParametre;
        }
    }
}
