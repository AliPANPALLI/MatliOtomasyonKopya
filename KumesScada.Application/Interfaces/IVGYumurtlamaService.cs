using KumesScada.Application.RequestModels;
using KumesScada.Application.ViewModels;

namespace KumesScada.Application.Interfaces
{
    public interface IVGYumurtlamaService
    {
        VGYumurtlamaViewModel Calculate(VGYumurtlamaRequest request);
    }
}