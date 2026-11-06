using Microsoft.EntityFrameworkCore;
using MobilGuncelleme.Domain.Entities;

namespace MobilGuncelleme.Application.Interfaces
{
    /// <summary>
    /// Application katmanının ihtiyaç duyduğu tablolar. Gerçeği Infrastructure'daki
    /// MobilGuncellemeDbContext'tir; servis yalnızca bu arayüzü bilir.
    /// </summary>
    public interface IMobilGuncellemeDbContext
    {
        DbSet<UygulamaSurum> UygulamaSurum { get; }
        DbSet<Cihaz> Cihaz { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
