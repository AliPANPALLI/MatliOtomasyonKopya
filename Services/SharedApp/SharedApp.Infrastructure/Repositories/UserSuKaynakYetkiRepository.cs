using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class UserSuKaynakYetkiRepository : Repository<UserSuKaynakYetki>, IUserSuKaynakYetkiRepository
    {
        private readonly SharedDbContext _context;
        public UserSuKaynakYetkiRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<vwSuHatCizimSirketForYetki>> GetvwSuHatCizimSirketForYetkiByCizimIds(List<int> cizimIds)
        {
            var result = await _context.vwSuHatCizimSirketForYetki.Where(x => cizimIds.Contains(x.CizimId.GetValueOrDefault(0))).ToListAsync();
            return result;
        }

        public async Task<List<vwSuHatCizimSirketForYetki>> GetAllvwSuHatCizimSirket()
        {
            var result = await _context.vwSuHatCizimSirketForYetki.ToListAsync();
            return result;
        }

        public async Task<List<vwUserSuKaynakYetki>> GetAllvwUserSuKaynakYetki()
        {
            return await _context.vwUserSuKaynakYetki.ToListAsync();
        }
    }
}