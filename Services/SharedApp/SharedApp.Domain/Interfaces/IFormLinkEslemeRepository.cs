using SharedApp.Domain.Models;
using MatliOtomasyon.Shared.Infrastructure.Persistence;

namespace SharedApp.Domain.Interfaces
{
    public interface IFormLinkEslemeRepository : IRepository<FormLinkEsleme>
    {
        Task<List<FormLinkEsleme>> GetAllByFormId(int formId);

        // Modül altındaki TÜM formların link kataloğu — admin ekranında
        // "bu modül için hangi linkler tanımlı" görünümü için.
        Task<List<FormLinkEsleme>> GetAllByModulId(int modulId);
    }
}
