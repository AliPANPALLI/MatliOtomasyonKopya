using Mapster;
using SharedApp.Application.ViewModels;
using SharedApp.Application.ViewModels.ReportViewModels;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models;
using SharedApp.Domain.Models.ReportModels;

namespace SharedApp.Application.Mapster
{
    public sealed class SharedAppMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            #region Db View Model to UI View Model
            //CreateMap<vwUser, User>();
            config.NewConfig<vwFormLokasyonEslemeViewModel, vwFormLokasyonEsleme>();
            #endregion Db View Model to UI View Model

            #region Model to ViewModel Mappings    
            config.NewConfig<AlarmGrupAlarmEsleme, AlarmGrupAlarmEslemeViewModel>();
            config.NewConfig<AlarmGruplar, AlarmGruplarViewModel>();
            config.NewConfig<AlarmKullaniciEsleme, AlarmKullaniciEslemeViewModel>();
            config.NewConfig<vwAlarmlar, AlarmlarViewModel>();
            config.NewConfig<Alarmlar, AlarmlarViewModel>();
            config.NewConfig<AlarmLog, AlarmLogViewModel>();
            config.NewConfig<Bolum, BolumViewModel>();
            config.NewConfig<FormLokasyonEsleme, FormLokasyonEslemeViewModel>();
            config.NewConfig<Form, FormViewModel>();
            config.NewConfig<GenelParametreler, GenelParametrelerViewModel>();
            config.NewConfig<Lokasyon, LokasyonViewModel>();
            config.NewConfig<MailGonderimTakip, MailGonderimTakipViewModel>();
            config.NewConfig<Modul, ModulViewModel>();
            config.NewConfig<ProductionReport, ProductionReportViewModel>();
            config.NewConfig<Sirket, SirketViewModel>();
            config.NewConfig<SonGelistirmeler, SonGelistirmelerViewModel>();
            config.NewConfig<Tbl_Makine, Tbl_MakineViewModel>();
            config.NewConfig<Tbl_Nesne, Tbl_NesneViewModel>();
            config.NewConfig<Tesis, TesisViewModel>();
            config.NewConfig<UserFormYetki, UserFormYetkiViewModel>();
            config.NewConfig<UserEnerjiIzlemeYetki, UserEnerjiIzlemeYetkiViewModel>();
            config.NewConfig<UserKumesYetki, UserKumesYetkiViewModel>();
            config.NewConfig<UserLinkYetki, UserLinkYetkiViewModel>();
            config.NewConfig<UserMakineYetki, UserMakineYetkiViewModel>();
            config.NewConfig<UserSuKaynakYetki, UserMakineYetkiViewModel>();
            #endregion Model to ViewModel Mappings

            #region ViewModel to Model
            config.NewConfig<AlarmGrupAlarmEslemeViewModel, AlarmGrupAlarmEsleme>();
            config.NewConfig<AlarmGruplarViewModel, AlarmGruplar>();
            config.NewConfig<AlarmKullaniciEslemeViewModel, AlarmKullaniciEsleme>();
            config.NewConfig<AlarmlarViewModel, Alarmlar>();
            config.NewConfig<AlarmLogViewModel, AlarmLog>();
            config.NewConfig<BolumViewModel, Bolum>();
            config.NewConfig<FormLokasyonEslemeViewModel, FormLokasyonEsleme>();
            config.NewConfig<FormViewModel, Form>();
            config.NewConfig<LokasyonViewModel, Lokasyon>();
            config.NewConfig<MailGonderimTakipViewModel, MailGonderimTakip>();
            config.NewConfig<ModulViewModel, Modul>();
            config.NewConfig<Tbl_MakineViewModel, Tbl_Makine>();
            config.NewConfig<Tbl_NesneViewModel, Tbl_Nesne>();
            config.NewConfig<TesisViewModel, Tesis>();
            config.NewConfig<UserFormYetkiViewModel, UserFormYetki>();
            config.NewConfig<UserEnerjiIzlemeYetkiViewModel, UserEnerjiIzlemeYetki>();
            config.NewConfig<UserKumesYetkiViewModel, UserKumesYetki>();
            config.NewConfig<UserLinkYetkiViewModel, UserLinkYetki>();
            config.NewConfig<UserMakineYetkiViewModel, UserMakineYetki>();
            config.NewConfig<UserSuKaynakYetkiViewModel, UserSuKaynakYetki>();
            #endregion ViewModel to Model
        }
    }
}
