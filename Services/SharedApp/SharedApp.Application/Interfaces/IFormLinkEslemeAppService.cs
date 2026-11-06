using SharedApp.Application.ViewModels;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IFormLinkEslemeAppService : IAppService<FormLinkEslemeViewModel>
    {
        Task<List<FormLinkEslemeViewModel>> GetAllByFormId(int formId);
        Task<List<FormLinkEslemeViewModel>> GetAllByModulId(int modulId);

        // Admin ekranının listesi için — her satıra Form/Modul adı eklenmiş
        // hâliyle TÜM FormLinkEsleme kayıtları (bkz. FormAppService.GetAllForm
        // ile aynı in-memory join deseni).
        Task<List<FormLinkEslemeViewModel>> GetAllEnriched();
    }
}
