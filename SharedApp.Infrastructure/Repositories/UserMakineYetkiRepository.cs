using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class UserMakineYetkiRepository : Repository<UserMakineYetki>, IUserMakineYetkiRepository
    {
        private readonly SharedDbContext _context;
        public UserMakineYetkiRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

    }
}
