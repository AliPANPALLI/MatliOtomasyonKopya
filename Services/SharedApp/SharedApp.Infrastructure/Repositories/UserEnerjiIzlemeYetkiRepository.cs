using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class UserEnerjiIzlemeYetkiRepository : Repository<UserEnerjiIzlemeYetki>, IUserEnerjiIzlemeYetkiRepository
    {
        private readonly SharedDbContext _context;
        public UserEnerjiIzlemeYetkiRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

    }
}
