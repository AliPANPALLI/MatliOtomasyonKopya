using Mapster;
using PanoKimliklendirme.Application.ViewModels;
using PanoKimliklendirme.Domain.Models;


namespace PanoKimliklendirme.Application.Mapster
{
    public sealed class PanoKimliklendirmeMappingConfig :IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            #region Db View Model to UI View Model
            #endregion Db View Model to UI View Model

            #region Model to ViewModel Mappings           
            config.NewConfig<Bolum, BolumViewModel>();
            config.NewConfig<Cihaz, CihazViewModel>();
            config.NewConfig<Makine, MakineViewModel>();
            config.NewConfig<Pano, PanoViewModel>();
            #endregion Model to ViewModel Mappings

            #region ViewModel to Model
            config.NewConfig<BolumViewModel, Bolum>();
            config.NewConfig<CihazViewModel, Cihaz>();
            config.NewConfig<MakineViewModel, Makine>();
            config.NewConfig<PanoViewModel, Pano>();
            #endregion ViewModel to Model
        }
    }
}
