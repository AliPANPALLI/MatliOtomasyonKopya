using SharedApp.Application.ViewModels;
using SharedApp.Application.ViewModels.ReportViewModels;
using SharedApp.Domain.Models;
using SharedApp.Domain.Models.ReportModels;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace SharedApp.Application.Interfaces
{
    public interface ISirketAppService : IAppService<SirketViewModel>
    {
        Task SetSilindi(int id);
        Task<List<Sirket>> GetAllSirketForYetki();
        Task<List<TreeModel>> GetTesisTreeView();
        Task AddLokasyon(LokasyonViewModel lokasyonViewModel);
        Task AddTesis(TesisViewModel tesisViewModel);
        Task AddBolum(BolumViewModel bolumViewModel);
        Task AddMakine(Tbl_MakineViewModel tbl_MakineViewModel);
        Task AddNesne(Tbl_NesneViewModel tbl_NesneViewModel);
    }
}
