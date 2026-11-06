using Microsoft.AspNetCore.Mvc;
using Ticket.Application.Interfaces;
using Ticket.Application.ViewModels;
using MatliOtomasyon.Shared.Application.Abstractions;

namespace Ticket.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TedarikciController : BaseApiController<TedarikciViewModel>
    {
        private readonly ITedarikciAppService _tedarikciAppService;
        private readonly IUserExtension _userExtension;

        public TedarikciController(ITedarikciAppService tedarikciAppService, IUserExtension userExtension) : base(tedarikciAppService)
        {
            _tedarikciAppService = tedarikciAppService;
            _userExtension = userExtension;
        }
    }
}