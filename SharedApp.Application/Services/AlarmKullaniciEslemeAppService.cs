using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;
using SharedApp.Domain.Models.ReportModels;
using static SharedKernel.ApplicationConstants;

namespace SharedApp.Application.Services
{
    public class AlarmKullaniciEslemeAppService : AppService<AlarmKullaniciEsleme, AlarmKullaniciEslemeViewModel>, IAlarmKullaniciEslemeAppService
    {
        private readonly IAlarmKullaniciEslemeRepository _alarmKullaniciEslemeRepository;

        public AlarmKullaniciEslemeAppService(IMapper mapper, IAlarmKullaniciEslemeRepository alarmKullaniciEslemeRepository) : base(mapper, alarmKullaniciEslemeRepository)
        {
            _alarmKullaniciEslemeRepository = alarmKullaniciEslemeRepository;
        }

        public async Task AddAlarmKullaniciEsleme2(List<string> secilenKullanicilar, List<int> secilenAlarmGruplar, List<int> secilenAlarmlar, bool silindi, bool aktif, bool seviyeAktif, int seviyeNo, string bildirimSuresi, string bildirimYontemi, bool tekrarlansinMi, string tekrarlamaSuresi)
        {
            var alarmKullaniciEslemeler = await _alarmKullaniciEslemeRepository.GetAllListAsync();
            var yetkiKayitlari = new List<AlarmKullaniciEsleme>();
            foreach (var userId in secilenKullanicilar)
            {
                var alarmKullaniciEslemeKayitlar = alarmKullaniciEslemeler.Where(x => x.UserId == userId).ToList();

                foreach (var alarmGrupId in secilenAlarmGruplar)
                {
                    var kayitVarMi = alarmKullaniciEslemeKayitlar.Where(x => x.UserId == userId && x.AlarmGrupId == alarmGrupId && x.SeviyeNo == seviyeNo).FirstOrDefault();
                    if (kayitVarMi == null)
                    {
                        yetkiKayitlari.Add(new AlarmKullaniciEsleme
                        {
                            UserId = userId,
                            AlarmId = 0,
                            AlarmGrupId = alarmGrupId,
                            Silindi = silindi,
                            SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null,
                            Aktif = aktif,
                            SeviyeAktif = seviyeAktif,
                            SeviyeNo = seviyeNo,
                            BildirimSuresi = TimeSpan.Parse(bildirimSuresi),
                            BildirimYontemi = bildirimYontemi,
                            TekrarlansinMi = bildirimYontemi == BildirimYontemi.SesliMesaj ? true : false,
                            TekrarlamaSuresi = bildirimYontemi == BildirimYontemi.SesliMesaj ? tekrarlamaSuresi != "0" ? TimeSpan.Parse(tekrarlamaSuresi) : null : null,
                        });
                    }
                    else
                    {
                        kayitVarMi.AlarmId = 0;
                        kayitVarMi.AlarmGrupId = alarmGrupId;
                        kayitVarMi.Silindi = silindi;
                        kayitVarMi.SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null;
                        kayitVarMi.Aktif = aktif;
                        kayitVarMi.SeviyeAktif = seviyeAktif;
                        kayitVarMi.SeviyeNo = seviyeNo;
                        kayitVarMi.BildirimSuresi = TimeSpan.Parse(bildirimSuresi);
                        kayitVarMi.BildirimYontemi = bildirimYontemi;
                        kayitVarMi.TekrarlansinMi = bildirimYontemi == BildirimYontemi.SesliMesaj ? true : false;
                        kayitVarMi.TekrarlamaSuresi = bildirimYontemi == BildirimYontemi.SesliMesaj ? tekrarlamaSuresi != "0" ? TimeSpan.Parse(tekrarlamaSuresi) : null : null;
                    }
                }

                foreach (var alarmId in secilenAlarmlar)
                {
                    var kayitVarMi = alarmKullaniciEslemeKayitlar.Where(x => x.UserId == userId && x.AlarmId == alarmId && x.SeviyeNo == seviyeNo).FirstOrDefault();
                    if (kayitVarMi == null)
                    {
                        yetkiKayitlari.Add(new AlarmKullaniciEsleme
                        {
                            UserId = userId,
                            AlarmId = alarmId,
                            AlarmGrupId = 0,
                            Silindi = silindi,
                            SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null,
                            Aktif = aktif,
                            SeviyeAktif = seviyeAktif,
                            SeviyeNo = seviyeNo,
                            BildirimSuresi = TimeSpan.Parse(bildirimSuresi),
                            BildirimYontemi = bildirimYontemi,
                            TekrarlansinMi = bildirimYontemi == BildirimYontemi.SesliMesaj ? true : false,
                            TekrarlamaSuresi = bildirimYontemi == BildirimYontemi.SesliMesaj ? tekrarlamaSuresi != "0" ? TimeSpan.Parse(tekrarlamaSuresi) : null : null,
                        });
                    }
                    else
                    {
                        kayitVarMi.UserId = userId;
                        kayitVarMi.AlarmId = alarmId;
                        kayitVarMi.AlarmGrupId = 0;
                        kayitVarMi.Silindi = silindi;
                        kayitVarMi.SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null;
                        kayitVarMi.Aktif = aktif;
                        kayitVarMi.SeviyeAktif = seviyeAktif;
                        kayitVarMi.SeviyeNo = seviyeNo;
                        kayitVarMi.BildirimSuresi = TimeSpan.Parse(bildirimSuresi);
                        kayitVarMi.BildirimYontemi = bildirimYontemi;
                        kayitVarMi.TekrarlansinMi = bildirimYontemi == BildirimYontemi.SesliMesaj ? true : false;
                        kayitVarMi.TekrarlamaSuresi = bildirimYontemi == BildirimYontemi.SesliMesaj ? tekrarlamaSuresi != "0" ? TimeSpan.Parse(tekrarlamaSuresi) : null : null;
                    }
                }
            }
            await _alarmKullaniciEslemeRepository.AddRangeAsync(yetkiKayitlari);
            await _alarmKullaniciEslemeRepository.SaveChangesAsync();
        }

        public async Task SetSilindi(int id)
        {
            var alarmKullaniciEsleme = await _alarmKullaniciEslemeRepository.GetAsyncByPredicate(x => x.Id == id);
            alarmKullaniciEsleme.Silindi = true;
            await _alarmKullaniciEslemeRepository.SaveChangesAsync();
        }

        public async Task<List<vwAlarmKullaniciEsleme>> GetAllvwAlarmKullaniciEsleme()
        {
            var vwAlarmKullaniciEslemeler = await _alarmKullaniciEslemeRepository.GetAllvwAlarmKullaniciEsleme();
            return vwAlarmKullaniciEslemeler;
        }

        public async Task EditAlarmKullaniciEsleme2(int alarmKullaniciEslemeId, bool silindi, bool aktif, bool seviyeAktif, int seviyeNo, string bildirimSuresi, string bildirimYontemi, bool tekrarlansinMi, string tekrarlamaSuresi)
        {
            var alarmKullaniciEsleme = await _alarmKullaniciEslemeRepository.GetAsyncByPredicate(x => x.Id == alarmKullaniciEslemeId);
            alarmKullaniciEsleme.Silindi = silindi;
            alarmKullaniciEsleme.SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null;
            alarmKullaniciEsleme.Aktif = aktif;
            alarmKullaniciEsleme.SeviyeAktif = seviyeAktif;
            alarmKullaniciEsleme.SeviyeNo = seviyeNo;
            alarmKullaniciEsleme.BildirimYontemi = bildirimYontemi;
            alarmKullaniciEsleme.BildirimSuresi = TimeSpan.Parse(bildirimSuresi);
            alarmKullaniciEsleme.TekrarlansinMi = bildirimYontemi == BildirimYontemi.SesliMesaj ? true : false;
            alarmKullaniciEsleme.TekrarlamaSuresi = bildirimYontemi == BildirimYontemi.SesliMesaj ? tekrarlamaSuresi != "0" ? TimeSpan.Parse(tekrarlamaSuresi) : null : null;
            await _alarmKullaniciEslemeRepository.SaveChangesAsync();
        }

        public async Task AddEditAlarmKullaniciEsleme(int alarmGrupId, List<string> secilenKullanicilarSeviye1, List<string> secilenKullanicilarSeviye2, List<string> secilenKullanicilarSeviye3, string bildirimYontemi, bool aktif, string bildirimSuresiSeviye1,
            string bildirimSuresiSeviye2, string bildirimSuresiSeviye3, bool tekrarlansinMi, bool silindi, bool seviyeAktif1, bool seviyeAktif2, bool seviyeAktif3)
        {
            var alarmKullaniciEslemeler = await _alarmKullaniciEslemeRepository.GetAllAsyncByPredicate(x => x.AlarmGrupId == alarmGrupId && x.BildirimYontemi == bildirimYontemi).ToListAsync();
            var eklenecekModelList = new List<AlarmKullaniciEsleme>();

            //Seviye 1 için kullanıcıları ekle             
            foreach (var secilenSeviye1UserId in secilenKullanicilarSeviye1)
            {
                var kayitVarMi = alarmKullaniciEslemeler.Where(x => x.UserId == secilenSeviye1UserId && x.SeviyeNo == 1).FirstOrDefault();
                if (kayitVarMi == null)
                {
                    eklenecekModelList.Add(new AlarmKullaniciEsleme
                    {
                        UserId = secilenSeviye1UserId,
                        AlarmGrupId = alarmGrupId,
                        Silindi = silindi,
                        SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null,
                        Aktif = aktif,
                        SeviyeAktif = seviyeAktif1,
                        SeviyeNo = 1,
                        BildirimYontemi = bildirimYontemi,
                        BildirimSuresi = TimeSpan.Parse(bildirimSuresiSeviye1),
                        TekrarlansinMi = tekrarlansinMi,
                    });
                }
                else
                {
                    kayitVarMi.UserId = secilenSeviye1UserId;
                    kayitVarMi.AlarmGrupId = alarmGrupId;
                    kayitVarMi.Silindi = silindi;
                    kayitVarMi.SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null;
                    kayitVarMi.Aktif = aktif;
                    kayitVarMi.SeviyeAktif = seviyeAktif1;
                    kayitVarMi.SeviyeNo = 1;
                    kayitVarMi.BildirimYontemi = bildirimYontemi;
                    kayitVarMi.BildirimSuresi = TimeSpan.Parse(bildirimSuresiSeviye1);
                    kayitVarMi.TekrarlansinMi = tekrarlansinMi;
                }
            }
            var silinecekKayitlar = alarmKullaniciEslemeler.Where(x => x.SeviyeNo == 1 && !secilenKullanicilarSeviye1.Contains(x.UserId)).ToList();
            foreach (var kayit in silinecekKayitlar)
            {
                kayit.Silindi = true;
                kayit.SilinmeZamani = DateTime.Now;
            }
            //Seviye 2 için kullanıcıları ekle
            foreach (var secilenSeviye2UserId in secilenKullanicilarSeviye2)
            {
                var kayitVarMi = alarmKullaniciEslemeler.Where(x => x.UserId == secilenSeviye2UserId && x.SeviyeNo == 2).FirstOrDefault();
                if (kayitVarMi == null)
                {
                    eklenecekModelList.Add(new AlarmKullaniciEsleme
                    {
                        UserId = secilenSeviye2UserId,
                        AlarmGrupId = alarmGrupId,
                        Silindi = silindi,
                        SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null,
                        Aktif = aktif,
                        SeviyeAktif = seviyeAktif2,
                        SeviyeNo = 2,
                        BildirimYontemi = bildirimYontemi,
                        BildirimSuresi = TimeSpan.Parse(bildirimSuresiSeviye2),
                        TekrarlansinMi = tekrarlansinMi,
                    });
                }
                else
                {
                    kayitVarMi.UserId = secilenSeviye2UserId;
                    kayitVarMi.AlarmGrupId = alarmGrupId;
                    kayitVarMi.Silindi = silindi;
                    kayitVarMi.SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null;
                    kayitVarMi.Aktif = aktif;
                    kayitVarMi.SeviyeAktif = seviyeAktif2;
                    kayitVarMi.SeviyeNo = 2;
                    kayitVarMi.BildirimYontemi = bildirimYontemi;
                    kayitVarMi.BildirimSuresi = TimeSpan.Parse(bildirimSuresiSeviye2);
                    kayitVarMi.TekrarlansinMi = tekrarlansinMi;
                }
            }
            silinecekKayitlar = alarmKullaniciEslemeler.Where(x => x.SeviyeNo == 2 && !secilenKullanicilarSeviye2.Contains(x.UserId)).ToList();
            foreach (var kayit in silinecekKayitlar)
            {
                kayit.Silindi = true;
                kayit.SilinmeZamani = DateTime.Now;
            }
            //Seviye 3 için kullanıcıları ekle
            foreach (var secilenSeviye3UserId in secilenKullanicilarSeviye3)
            {
                var kayitVarMi = alarmKullaniciEslemeler.Where(x => x.UserId == secilenSeviye3UserId && x.SeviyeNo == 3).FirstOrDefault();
                if (kayitVarMi == null)
                {
                    eklenecekModelList.Add(new AlarmKullaniciEsleme
                    {
                        UserId = secilenSeviye3UserId,
                        AlarmGrupId = alarmGrupId,
                        Silindi = silindi,
                        SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null,
                        Aktif = aktif,
                        SeviyeAktif = seviyeAktif3,
                        SeviyeNo = 3,
                        BildirimYontemi = bildirimYontemi,
                        BildirimSuresi = TimeSpan.Parse(bildirimSuresiSeviye3),
                        TekrarlansinMi = tekrarlansinMi,
                    });
                }
                else
                {
                    kayitVarMi.UserId = secilenSeviye3UserId;
                    kayitVarMi.AlarmGrupId = alarmGrupId;
                    kayitVarMi.Silindi = silindi;
                    kayitVarMi.SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null;
                    kayitVarMi.Aktif = aktif;
                    kayitVarMi.SeviyeAktif = seviyeAktif3;
                    kayitVarMi.SeviyeNo = 3;
                    kayitVarMi.BildirimYontemi = bildirimYontemi;
                    kayitVarMi.BildirimSuresi = TimeSpan.Parse(bildirimSuresiSeviye3);
                    kayitVarMi.TekrarlansinMi = tekrarlansinMi;
                }
            }
            silinecekKayitlar = alarmKullaniciEslemeler.Where(x => x.SeviyeNo == 3 && !secilenKullanicilarSeviye3.Contains(x.UserId)).ToList();
            foreach (var kayit in silinecekKayitlar)
            {
                kayit.Silindi = true;
                kayit.SilinmeZamani = DateTime.Now;
            }
            await _alarmKullaniciEslemeRepository.AddRangeAsync(eklenecekModelList);
            await _alarmKullaniciEslemeRepository.SaveChangesAsync();
        }

        public async Task EditAlarmKullaniciEsleme(int alarmKullaniciEslemeId, string bildirimYontemi, bool aktif, string bildirimSuresi, bool tekrarlansinMi, bool silindi)
        {
            var alarmKullaniciEsleme = await _alarmKullaniciEslemeRepository.GetAsyncByPredicate(x => x.Id == alarmKullaniciEslemeId);
            alarmKullaniciEsleme.Silindi = silindi;
            alarmKullaniciEsleme.SilinmeZamani = silindi ? DateTime.Now : (DateTime?)null;
            alarmKullaniciEsleme.Aktif = aktif;
            alarmKullaniciEsleme.SeviyeAktif = true;
            alarmKullaniciEsleme.BildirimYontemi = bildirimYontemi;
            alarmKullaniciEsleme.BildirimSuresi = TimeSpan.Parse(bildirimSuresi);
            alarmKullaniciEsleme.TekrarlansinMi = tekrarlansinMi;
            await _alarmKullaniciEslemeRepository.SaveChangesAsync();
        }

        public async Task<AlarmKullaniciEslemeFormModel> GetAlarmKullaniciEslemelerForEdit(int alarmKullaniciEslemeId)
        {
            var result = new AlarmKullaniciEslemeFormModel();
            var alarmKullaniciEsleme = await _alarmKullaniciEslemeRepository.GetAsyncByPredicate(x => x.Id == alarmKullaniciEslemeId);
            var alarmKullaniciEslemelerByAlarmGrupId = await _alarmKullaniciEslemeRepository.GetAllAsyncByPredicate(x => x.AlarmGrupId == alarmKullaniciEsleme.AlarmGrupId && x.BildirimYontemi == alarmKullaniciEsleme.BildirimYontemi && x.Silindi != true).ToListAsync();
            var grupAlarmKullaniciEslemelerBySeviyeNo = alarmKullaniciEslemelerByAlarmGrupId.GroupBy(x => x.SeviyeNo).ToList();

            foreach (var seviyeAlarmKullaniciEsleme in grupAlarmKullaniciEslemelerBySeviyeNo)
            {
                switch (seviyeAlarmKullaniciEsleme.FirstOrDefault()?.SeviyeNo)
                {
                    case 1:
                        result.BildirimSuresiSeviye1 = seviyeAlarmKullaniciEsleme.FirstOrDefault()?.BildirimSuresi;
                        result.SecilenKullanicilarSeviye1 = seviyeAlarmKullaniciEsleme.Select(x => x.UserId).ToList();
                        result.SeviyeAktif1 = seviyeAlarmKullaniciEsleme.FirstOrDefault()?.SeviyeAktif;
                        break;
                    case 2:
                        result.BildirimSuresiSeviye2 = seviyeAlarmKullaniciEsleme.FirstOrDefault()?.BildirimSuresi;
                        result.SecilenKullanicilarSeviye2 = seviyeAlarmKullaniciEsleme.Select(x => x.UserId).ToList();
                        result.SeviyeAktif2 = seviyeAlarmKullaniciEsleme.FirstOrDefault()?.SeviyeAktif;
                        break;
                    case 3:
                        result.BildirimSuresiSeviye3 = seviyeAlarmKullaniciEsleme.FirstOrDefault()?.BildirimSuresi;
                        result.SecilenKullanicilarSeviye3 = seviyeAlarmKullaniciEsleme.Select(x => x.UserId).ToList();
                        result.SeviyeAktif3 = seviyeAlarmKullaniciEsleme.FirstOrDefault()?.SeviyeAktif;
                        break;
                    default:
                        break;
                }
                result.AlarmGrupId = seviyeAlarmKullaniciEsleme.FirstOrDefault()?.AlarmGrupId;
                result.BildirimYontemi = seviyeAlarmKullaniciEsleme.FirstOrDefault()?.BildirimYontemi;
                result.Aktif = seviyeAlarmKullaniciEsleme.FirstOrDefault()?.Aktif;
                result.TekrarlansinMi = seviyeAlarmKullaniciEsleme.FirstOrDefault()?.TekrarlansinMi;
                result.Silindi = seviyeAlarmKullaniciEsleme.FirstOrDefault()?.Silindi;
            }
            return result;
        }

        public async Task<AlarmKullaniciEslemeFormModel?> GetAlarmKullaniciEslemeVarMiKontrolEt(int alarmGrupId, string bildirimYontemi)
        {
            var alarmKullaniciEsleme = await _alarmKullaniciEslemeRepository.GetAsyncByPredicate(x => x.AlarmGrupId == alarmGrupId && x.BildirimYontemi == bildirimYontemi && x.Silindi != true);
            if (alarmKullaniciEsleme != null)
            {
                var result = await GetAlarmKullaniciEslemelerForEdit(alarmKullaniciEsleme.Id);
                return result;
            }
            else
            {
                return null;
            }
        }
    }
}