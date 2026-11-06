using KumesScada.Application.ViewModels;
using KumesScada.Domain.Models;
using SharedKernel.Core;

namespace KumesScada.Application.Interfaces
{
    public interface ICizimAppService : IAppService<CizimViewModel>
    {
        Task<List<SekilJson>> GetCizimJsonByNameWithValue(string cizimName);
        Task<List<SekilJson>> GetCizimJsonById(int cizimId);
        Task<List<SekilJson>> GetCizimJsonByKumesId(int kumesId);
        Task EditSekil(SekilJson sekil);
        Task<List<ShapeEditorEtiketViewModel>> GetShapeEditorEtiketler();
        Task<List<ShapeEditorRuleViewModel>> GetShapeEditorKurallar(string shapeJsonId);
        Task<ShapeEditorRuleViewModel> SaveShapeEditorRule(SaveShapeEditorRuleViewModel model);
        Task DeleteShapeEditorRule(int id);

    }
}
