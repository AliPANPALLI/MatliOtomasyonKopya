using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.Services.Core;
using SharedApp.Application.ViewModels;
using SharedApp.Domain.Interfaces;
using SharedApp.Domain.Models;

namespace SharedApp.Application.Services
{
    public class LokasyonAppService : AppService<Lokasyon, LokasyonViewModel>, ILokasyonAppService
    {
        private readonly ILokasyonRepository _lokasyonRepository;

        public LokasyonAppService(IMapper mapper, ILokasyonRepository lokasyonRepository) : base(mapper, lokasyonRepository)
        {
            _lokasyonRepository = lokasyonRepository;
        }

        public async Task<List<Lokasyon>> GetAllLokasyonBySirketId(int sirketId)
        {
            var lokasyonlar = await _lokasyonRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.SirketId == sirketId && x.Silindi != true);
            return lokasyonlar;
        }

        public async Task SetSilindi(int id)
        {
            var lokasyon = await _lokasyonRepository.GetAsyncByPredicate(x => x.Id == id && x.Silindi != true);
            lokasyon.Silindi = true;
            await _lokasyonRepository.SaveChangesAsync();
        }

        public async Task<List<Lokasyon>> GetAllLokasyonForYetki()
        {
            var listLokasyon = new List<Lokasyon>();

            var makineler = await _lokasyonRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true);
            listLokasyon.AddRange(makineler);

            return listLokasyon;
        }

        public async Task<List<Lokasyon>> GetAllLokasyonForTicketTanim()
        {
            return await _lokasyonRepository.GetAllAsyncByPredicateWithAsNoTracking(x => x.Silindi != true);           
        }
    }
}
