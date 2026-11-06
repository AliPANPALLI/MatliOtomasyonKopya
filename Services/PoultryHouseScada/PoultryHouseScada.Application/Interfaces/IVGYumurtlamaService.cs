using PoultryHouseScada.Application.RequestModels;
using PoultryHouseScada.Application.ViewModels;

namespace PoultryHouseScada.Application.Interfaces
{
    public interface IVGYumurtlamaService
    {
        VGYumurtlamaViewModel Calculate(VGYumurtlamaRequest request);
    }
}