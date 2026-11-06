namespace PanoKimliklendirme.Infrastructure.Repositories.Core
{
    public interface IUnitOfWork
    {
        Task CommitAsync();
        void Commit();
    }
}
