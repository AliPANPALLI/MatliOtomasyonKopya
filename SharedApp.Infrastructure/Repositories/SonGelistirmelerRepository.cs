using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class SonGelistirmelerRepository : Repository<SonGelistirmeler>, ISonGelistirmelerRepository
    {
        private readonly SharedDbContext _context;
        public SonGelistirmelerRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

    }
}
