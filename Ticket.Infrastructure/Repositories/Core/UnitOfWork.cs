namespace Ticket.Infrastructure.Repositories.Core
{
    public class UnitOfWork
    {
        private readonly TicketDbContext _context;

        public UnitOfWork(TicketDbContext appDbContext)
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
