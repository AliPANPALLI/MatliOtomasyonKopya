using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class GenelParametrelerAppService : AppService<GenelParametreler, GenelParametrelerViewModel>, IGenelParametrelerAppService
    {
        private readonly IGenelParametrelerRepository _genelParametrelerRepository;

        public GenelParametrelerAppService(IMapper mapper, IGenelParametrelerRepository genelParametrelerRepository) : base(mapper, genelParametrelerRepository)
        {
            _genelParametrelerRepository = genelParametrelerRepository;
        }
        public async Task<GenelParametreler?> GetGenelParametrelerDegerByName(string genelParemetreName)
        {
            return await _genelParametrelerRepository.GetGenelParametrelerDegerByName(genelParemetreName);
        }
    }
}
