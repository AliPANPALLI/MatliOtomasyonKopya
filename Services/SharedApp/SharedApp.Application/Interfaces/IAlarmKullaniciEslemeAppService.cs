using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Models.ReportModels;
using SharedKernel.Core;

namespace SharedApp.Application.Interfaces
{
    public interface IAlarmKullaniciEslemeAppService : IAppService<AlarmKullaniciEslemeViewModel>
    {
        Task AddAlarmKullaniciEsleme2(List<string> secilenKullanicilar, List<int> secilenAlarmGruplar, List<int> secilenAlarmlar, bool silindi, bool aktif, bool seviyeAktif, int seviyeNo, string bildirimSuresi, string bildirimYontemi, bool tekrarlansinMi, string tekrarlamaSuresi);
        Task EditAlarmKullaniciEsleme(int alarmKullaniciEslemeId, string bildirimYontemi, bool aktif, string bildirimSuresi, bool tekrarlansinMi, bool silindi);
        Task<AlarmKullaniciEslemeFormModel> GetAlarmKullaniciEslemelerForEdit(int alarmKullaniciEslemeId);
        Task<AlarmKullaniciEslemeFormModel?> GetAlarmKullaniciEslemeVarMiKontrolEt(int alarmGrupId, string bildirimYontemi);
        Task EditAlarmKullaniciEsleme2(int alarmKullaniciEslemeId, bool silindi, bool aktif, bool seviyeAktif, int seviyeNo, string bildirimSuresi, string bildirimYontemi, bool tekrarlansinMi, string tekrarlamaSuresi);
        Task AddEditAlarmKullaniciEsleme(int alarmGrupId, List<string> secilenKullanicilarSeviye1, List<string> secilenKullanicilarSeviye2, List<string> secilenKullanicilarSeviye3, string bildirimYontemi, bool aktif, string bildirimSuresiSeviye1,
            string bildirimSuresiSeviye2, string bildirimSuresiSeviye3, bool tekrarlansinMi, bool silindi, bool seviyeAktif1, bool seviyeAktif2, bool seviyeAktif3);
        Task SetSilindi(int id);
        Task<List<vwAlarmKullaniciEsleme>> GetAllvwAlarmKullaniciEsleme();
    }
}
