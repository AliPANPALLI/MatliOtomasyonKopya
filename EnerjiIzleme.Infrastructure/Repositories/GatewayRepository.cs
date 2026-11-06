using EnerjiIzleme.Domain.Interfaces;
using EnerjiIzleme.Domain.Models;
using EnerjiIzleme.Infrastructure.Repositories.Core;
using Microsoft.EntityFrameworkCore;

namespace EnerjiIzleme.Infrastructure.Repositories
{
    public class GatewayRepository : Repository<Gateway>, IGatewayRepository
    {
        private EnerjiIzlemeDbContext _context;

        public GatewayRepository(EnerjiIzlemeDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task AddGateway(Gateway gateway)
        {
            await _context.Gateways.AddAsync(gateway);
            await _context.SaveChangesAsync();
        }

        public async Task SetSilindi(int id)
        {
            var gateway = await _context.Gateways.Where(x => x.Id == id).FirstOrDefaultAsync();
            gateway.Aktif = false;
            await _context.SaveChangesAsync();
        }
    }
}