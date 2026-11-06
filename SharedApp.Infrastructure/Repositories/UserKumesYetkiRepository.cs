using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class UserKumesYetkiRepository : Repository<UserKumesYetki>, IUserKumesYetkiRepository
    {
        private readonly SharedDbContext _context;
        public UserKumesYetkiRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

    }
}
