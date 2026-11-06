using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.DbViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class FormLokasyonEslemeAppService : AppService<FormLokasyonEsleme, FormLokasyonEslemeViewModel>, IFormLokasyonEslemeAppService
    {
        private readonly IFormLokasyonEslemeRepository _formLokasyonEslemeRepository;

        public FormLokasyonEslemeAppService(IMapper mapper, IFormLokasyonEslemeRepository formLokasyonEslemeRepository) : base(mapper, formLokasyonEslemeRepository)
        {
            _formLokasyonEslemeRepository = formLokasyonEslemeRepository;
        }

        public async Task<bool> AddFormLokasyonEsleme(List<int> secilenLokasyonlar, List<int> secilenFormlar, List<int> secilenCizimler)
        {
            try
            {
                List<FormLokasyonEsleme> addedList = new List<FormLokasyonEsleme>();
                var formLokasyonEslemeler = await _formLokasyonEslemeRepository.GetAllAsyncByPredicateWithAsNoTracking(x => secilenFormlar.Contains(x.FormId) && secilenLokasyonlar.Contains(x.LokasyonId));

                foreach (var formId in secilenFormlar)
                {
                    foreach (var lokasyonId in secilenLokasyonlar)
                    {
                        var formLokasyonEsleme = formLokasyonEslemeler.Where(x => x.FormId == formId && x.LokasyonId == lokasyonId).FirstOrDefault();
                        if (formLokasyonEsleme == null)
                        {
                            var addModel = new FormLokasyonEsleme { FormId = formId, LokasyonId = lokasyonId, Silindi = false };
                            addedList.Add(addModel);
                        }
                        else
                        {
                            if (formLokasyonEsleme.Silindi.GetValueOrDefault(false))
                            {
                                _formLokasyonEslemeRepository.Attach(formLokasyonEsleme);
                                formLokasyonEsleme.Silindi = false;
                                //_formLokasyonEslemeRepository.Detach(formLokasyonEsleme);
                            }
                        }
                    }

                    foreach (var cizimId in secilenCizimler)
                    {
                        var formLokasyonEsleme = formLokasyonEslemeler.Where(x => x.CizimId == cizimId && x.FormId == formId).FirstOrDefault();
                        if (formLokasyonEsleme == null)
                        {
                            var addModel = new FormLokasyonEsleme { CizimId = cizimId, FormId = formId, LokasyonId = 13/* 0 veya null verilemediğinden sabit verdim*/, Silindi = false };
                            addedList.Add(addModel);
                        }
                        else
                        {
                            if (formLokasyonEsleme.Silindi.GetValueOrDefault(false))
                            {
                                _formLokasyonEslemeRepository.Attach(formLokasyonEsleme);
                                formLokasyonEsleme.Silindi = false;
                            }
                        }
                    }
                }
                await _formLokasyonEslemeRepository.AddRangeAsync(addedList);
                await _formLokasyonEslemeRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> EditFormLokasyonEsleme(FormLokasyonEslemeViewModel formLokasyonEslemeModel)
        {
            try
            {
                var formLokasyonEsleme = await _formLokasyonEslemeRepository.GetAsyncByPredicate(x => x.FormId == formLokasyonEslemeModel.FormId && x.LokasyonId == formLokasyonEslemeModel.LokasyonId);
                if (formLokasyonEsleme == null)
                {
                    var addModel = new FormLokasyonEsleme { FormId = formLokasyonEslemeModel.FormId, LokasyonId = formLokasyonEslemeModel.LokasyonId, Silindi = false };
                    await _formLokasyonEslemeRepository.AddAsync(addModel);
                }
                else
                {
                    //eger kayit varsa sadece silindi durumu degistirilir
                    if (formLokasyonEslemeModel.Silindi != formLokasyonEsleme.Silindi.GetValueOrDefault(false))
                    {
                        formLokasyonEsleme.Silindi = formLokasyonEslemeModel.Silindi;
                    }
                }
                await _formLokasyonEslemeRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public IQueryable<vwFormLokasyonEsleme> GetAllFormLokasyonEslemeDx()
        {
            return _formLokasyonEslemeRepository.GetAllFormLokasyonEslemeDx();
        }

        public async Task SetSilindi(int id)
        {
            var formLokasyonEsleme = await _formLokasyonEslemeRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            formLokasyonEsleme.Silindi = true;
            await _formLokasyonEslemeRepository.SaveChangesAsync();
        }
    }
}