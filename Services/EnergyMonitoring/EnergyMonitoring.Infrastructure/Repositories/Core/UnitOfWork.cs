namespace EnergyMonitoring.Infrastructure.Repositories.Core
{
    public class UnitOfWork
    {
        private readonly EnerjiIzlemeDbContext _context;

        public UnitOfWork(EnerjiIzlemeDbContext appDbContext)
        {
            _context = appDbContext;
        }
        public void Commit()
        {
            _context.SaveChanges();
        }

        public async Task CommitAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
