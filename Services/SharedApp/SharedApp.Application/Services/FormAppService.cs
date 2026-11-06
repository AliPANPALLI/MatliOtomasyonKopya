using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class FormAppService : AppService<Form, FormViewModel>, IFormAppService
    {
        private readonly IFormRepository _formRepository;
        private readonly IModulRepository _modulRepository;

        public FormAppService(IMapper mapper, IFormRepository formRepository, IModulRepository modulRepository) : base(mapper, formRepository)
        {
            _formRepository = formRepository;
            _modulRepository = modulRepository;
        }

        public async Task<List<FormViewModel>> GetAllForm()
        {           
            var forms = await _formRepository.GetAllListAsync();
            var moduls = await _modulRepository.GetAllListAsync();

            return forms.Select(uky => new FormViewModel
            {
                Id = uky.Id,
                Name = uky.Name,
                Aciklama = uky.Aciklama,
                Link = uky.Link,
                ModulId = uky.ModulId,
                ModulName = moduls.FirstOrDefault(m => m.Id == uky.ModulId)?.Name,
                TopbarNesnesi = uky.TopbarNesnesi,
                Sira = uky.Sira,
                Silindi = uky.Silindi,
            }).ToList();
        }

        public async Task SetSilindi(int id)
        {
            var form = await _formRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            form.Silindi = true;
            await _formRepository.SaveChangesAsync();
        }
    }
}
