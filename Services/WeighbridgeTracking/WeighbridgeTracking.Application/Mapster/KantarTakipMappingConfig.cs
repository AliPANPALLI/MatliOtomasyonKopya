using WeighbridgeTracking.Application.DTOs;
using WeighbridgeTracking.Application.ViewModels;
using WeighbridgeTracking.Domain.Models;
using Mapster;

namespace WeighbridgeTracking.Application.Mapster
{
    public sealed class KantarTakipMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            #region Model to Dto
            config.NewConfig<Firma, FirmaDto>();
            config.NewConfig<KaliteKontrolPersonel, KaliteKontrolPersonelDto>();
            config.NewConfig<KantarGuvenlik, KantarGuvenlikDto>();
            config.NewConfig<KantarKalite, KantarKaliteDto>();
            #endregion Model to Dto

            #region ViewModel to Model
            config.NewConfig<FirmaViewModel, Firma>();
            config.NewConfig<KaliteKontrolPersonelViewModel, KaliteKontrolPersonel>();
            // Tarih (nullable -> zorunlu) serviste doğrulanıp elle atanır.
            config.NewConfig<KantarGuvenlikViewModel, KantarGuvenlik>()
                .Ignore(d => d.Tarih);
            config.NewConfig<KantarKaliteViewModel, KantarKalite>();
            #endregion ViewModel to Model
        }
    }
}
