using Authentication.Application.ViewModels;
using Authentication.Domain.DbViewModels;
using Authentication.Domain.Models;
using Authentication.Domain.Models.ReportModels;
using Mapster;

namespace Authentication.Application.Mapster
{
    public sealed class AuthMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            #region Db View Model to UI View Model
            config.NewConfig<vwPageVisitMain, PageVisitReportModel>();
            #endregion Db View Model to UI View Model

            #region Model to ViewModel Mappings           
            config.NewConfig<User, UserViewModel>();
            config.NewConfig<User, UserOtpViewModel>();
            #endregion Model to ViewModel Mappings

            #region ViewModel to Model
            config.NewConfig<UserViewModel, User>();
            #endregion ViewModel to Model        
        }
    }
}