using PoultryHouseScada.Application.ViewModels;
using PoultryHouseScada.Domain.Models;
using Mapster;

namespace PoultryHouseScada.Application.Mapster
{
    public sealed class KumesScadaMappingConfig:IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            #region Db View Model to UI View Model

            #endregion Db View Model to UI View Model

            #region Model to ViewModel Mappings           
            config.NewConfig<Cizim, CizimViewModel>();
            config.NewConfig<Konveyor, KonveyorViewModel>();
            config.NewConfig<Kumes, KumesViewModel>();
            config.NewConfig<AlarmLog, AlarmViewModel>();
            config.NewConfig<KumesHaftalikCatiKontrol, KumesHaftalikCatiKontrolViewModel>();
            config.NewConfig<KumesRaporRehber, KumesRaporRehberViewModel>();
            #endregion Model to ViewModel Mappings

            #region ViewModel to Model
            config.NewConfig<KumesHaftalikCatiKontrolViewModel, KumesHaftalikCatiKontrol>();
            config.NewConfig<KumesRaporRehberViewModel, KumesRaporRehber>();
            #endregion ViewModel to Model
        }
    }
}
