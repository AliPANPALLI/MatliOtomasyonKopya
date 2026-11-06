using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class UserFormYetkiRepository : Repository<UserFormYetki>, IUserFormYetkiRepository
    {
        private readonly SharedDbContext _context;
        public UserFormYetkiRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

    }
}
