using EnerjiIzleme.Application.ViewModels;
using EnerjiIzleme.Domain.DbViewModels;
using EnerjiIzleme.Domain.Models;
using Mapster;

namespace EnerjiIzleme.Application.Mapster
{
    public sealed class EnerjiIzlemeMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            #region Db View Model to UI View Model
            config.NewConfig<vwBolum, BolumViewModel>();
            config.NewConfig<vwCihaz_Onay, CihazViewModel>();
            config.NewConfig<vwMakine, MakineViewModel>();
            #endregion Db View Model to UI View Model

            #region Model to ViewModel Mappings           
            config.NewConfig<Bolum, BolumViewModel>();
            config.NewConfig<Cihaz, CihazViewModel>();
            config.NewConfig<CihazModel, CihazModelViewModel>();
            config.NewConfig<Gateway, GatewayViewModel>();
            config.NewConfig<Makine, MakineViewModel>();
            config.NewConfig<EnerjiMaliyetParametreler, EnerjiMaliyetParametrelerViewModel>();
            config.NewConfig<vwAlarmLog, vwAlarmLogViewModel>();
            #endregion Model to ViewModel Mappings

            #region ViewModel to Model
            config.NewConfig<vwAlarmLogViewModel, vwAlarmLog>();
            config.NewConfig<BolumViewModel, Bolum>();
            config.NewConfig<CihazViewModel, Cihaz>();
            config.NewConfig<CihazModelViewModel, CihazModel>();
            config.NewConfig<GatewayViewModel, Gateway>();
            config.NewConfig<MakineViewModel, Makine>();
            config.NewConfig<EnerjiMaliyetParametrelerViewModel, EnerjiMaliyetParametreler>();
            #endregion ViewModel to Model       
        }
    }
}
