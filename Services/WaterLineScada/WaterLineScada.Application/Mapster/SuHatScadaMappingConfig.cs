using Mapster;
using WaterLineScada.Application.ViewModels;
using WaterLineScada.Domain.DbViewModels;
using WaterLineScada.Domain.Models;

namespace WaterLineScada.Application.Mapster
{
    public sealed class SuHatScadaMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            #region Db View Model to UI View Model

            #endregion Db View Model to UI View Model


            #region Model to ViewModel Mappings 
            config.NewConfig<AlarmLog, AlarmViewModel>();
            config.NewConfig<Sayac, SayacViewModel>();
            config.NewConfig<SuKaynak, SuKaynakViewModel>();
            #endregion Model to ViewModel Mappings


            #region ViewModel to Model
            config.NewConfig<vwAlarmLog, AlarmViewModel>();
            #endregion ViewModel to Model
        }
    }
}