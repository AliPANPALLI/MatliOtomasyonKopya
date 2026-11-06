namespace PanoKimliklendirme.Infrastructure.Repositories.Core
{
    internal class UnitOfWork
    {
        private readonly PanoKimliklendirmeDbContext _context;

        public UnitOfWork(PanoKimliklendirmeDbContext appDbContext)
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
