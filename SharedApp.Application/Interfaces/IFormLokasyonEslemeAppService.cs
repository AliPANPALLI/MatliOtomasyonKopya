using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.DbViewModels;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IFormLokasyonEslemeAppService : IAppService<FormLokasyonEslemeViewModel>
    {
        Task SetSilindi(int id);
        Task<bool> AddFormLokasyonEsleme(List<int> secilenLokasyonlar, List<int> secilenFormlar, List<int> secilenCizimler);
        Task<bool> EditFormLokasyonEsleme(FormLokasyonEslemeViewModel formLokasyonEslemeModel);
        IQueryable<vwFormLokasyonEsleme> GetAllFormLokasyonEslemeDx();

    }
}
