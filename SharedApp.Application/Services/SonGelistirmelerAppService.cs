using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class SonGelistirmelerAppService : AppService<SonGelistirmeler, SonGelistirmelerViewModel>, ISonGelistirmelerAppService
    {
        private readonly ISonGelistirmelerRepository _sonGelistirmelerRepository;

        public SonGelistirmelerAppService(IMapper mapper, ISonGelistirmelerRepository sonGelistirmelerRepository) : base(mapper, sonGelistirmelerRepository)
        {
            _sonGelistirmelerRepository = sonGelistirmelerRepository;
        }

        public async Task<List<SonGelistirmeler>> GetSonGelistirmeler()
        {
            var result = (await _sonGelistirmelerRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Tarih >= DateTime.Now.AddMonths(-2))).OrderByDescending(x => x.Tarih).Take(10).ToList();
            return result;
        }
    }
}