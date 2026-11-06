using MapsterMapper;
using Ticket.Application.Interfaces;
using Ticket.Application.Services.Core;
using Ticket.Application.ViewModels;
using Ticket.Domain.Interfaces;
using Ticket.Domain.Models;

namespace Ticket.Application.Services
{
    public class TedarikciAppService : AppService<Tedarikci, TedarikciViewModel>, ITedarikciAppService
    {
        private readonly ITedarikciRepository _tedarikciRepository;
        public TedarikciAppService(IMapper mapper, ITedarikciRepository tedarikciRepository) : base(mapper, tedarikciRepository)
        {
            _tedarikciRepository = tedarikciRepository;
        }
    }
}
