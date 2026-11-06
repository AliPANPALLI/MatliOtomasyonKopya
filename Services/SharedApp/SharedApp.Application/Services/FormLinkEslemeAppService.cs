using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class FormLinkEslemeAppService : AppService<FormLinkEsleme, FormLinkEslemeViewModel>, IFormLinkEslemeAppService
    {
        private readonly IFormLinkEslemeRepository _formLinkEslemeRepository;
        private readonly IFormRepository _formRepository;
        private readonly IModulRepository _modulRepository;
        private readonly ISirketRepository _sirketRepository;

        public FormLinkEslemeAppService(IMapper mapper, IFormLinkEslemeRepository formLinkEslemeRepository, IFormRepository formRepository, IModulRepository modulRepository, ISirketRepository sirketRepository) : base(mapper, formLinkEslemeRepository)
        {
            _formLinkEslemeRepository = formLinkEslemeRepository;
            _formRepository = formRepository;
            _modulRepository = modulRepository;
            _sirketRepository = sirketRepository;
        }

        public async Task<List<FormLinkEslemeViewModel>> GetAllEnriched()
        {
            var rows = await _formLinkEslemeRepository.GetAllListAsync();
            var forms = await _formRepository.GetAllListAsync();
            var moduls = await _modulRepository.GetAllListAsync();
            var sirketler = await _sirketRepository.GetAllListAsync();

            // Formu veya modülü silinmiş satırlar listeden tamamen çıkarılır
            // — admin ekranında artık var olmayan bir Form'a ait eşleme
            // gösterilmemeli.
            return rows.Where(x => x.Silindi != true).Select(x =>
            {
                var form = forms.FirstOrDefault(f => f.Id == x.FormId && f.Silindi != true);
                var modul = form != null ? moduls.FirstOrDefault(m => m.Id == form.ModulId && m.Silindi != true) : null;
                var sirket = x.SirketId.HasValue ? sirketler.FirstOrDefault(s => s.Id == x.SirketId && s.Silindi != true) : null;

                return (Row: x, Form: form, Modul: modul, Sirket: sirket);
            })
            .Where(x => x.Form != null && x.Modul != null)
            .Select(x => new FormLinkEslemeViewModel
            {
                Id = x.Row.Id,
                FormId = x.Row.FormId,
                FormName = x.Form!.Name,
                ModulName = x.Modul!.Name,
                Link = x.Row.Link,
                Ad = x.Row.Ad,
                SirketId = x.Row.SirketId,
                SirketName = x.Sirket?.Name,
                SidebarGoster = x.Row.SidebarGoster,
                Sira = x.Row.Sira,
                Silindi = x.Row.Silindi,
            }).ToList();
        }

        public async Task<List<FormLinkEslemeViewModel>> GetAllByFormId(int formId)
        {
            var result = await _formLinkEslemeRepository.GetAllByFormId(formId);
            return _mapper.Map<List<FormLinkEslemeViewModel>>(result);
        }

        public async Task<List<FormLinkEslemeViewModel>> GetAllByModulId(int modulId)
        {
            var result = await _formLinkEslemeRepository.GetAllByModulId(modulId);
            return _mapper.Map<List<FormLinkEslemeViewModel>>(result);
        }
    }
}
