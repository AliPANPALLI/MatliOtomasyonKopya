using Microsoft.EntityFrameworkCore;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Domain.Models.ReportModels;
using SharedApp.Infrastructure.Repositories.Core;

namespace SharedApp.Infrastructure.Repositories
{
    public class LokasyonRepository : Repository<Lokasyon>, ILokasyonRepository
    {
        private readonly SharedDbContext _context;
        public LokasyonRepository(SharedDbContext context) : base(context)
        {
            _context = context;
        }
       
    }
}