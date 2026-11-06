using Microsoft.AspNetCore.Mvc;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenelParametrelerController : BaseApiController<GenelParametrelerViewModel>
    {
        private readonly IGenelParametrelerAppService _genelParametrelerAppService;

        public GenelParametrelerController(IGenelParametrelerAppService genelParametrelerAppService) : base(genelParametrelerAppService)
        {
            _genelParametrelerAppService = genelParametrelerAppService;
        }

        [HttpGet("GetGenelParametrelerDegerByName")]
        public async Task<IActionResult> GetGenelParametrelerDegerByName(string genelParametreName)
        {
            var result = await _genelParametrelerAppService.GetGenelParametrelerDegerByName(genelParametreName);
            return Ok(result);
        }

        //public async Task<List<UserMakineYetki>> GetUserMakineYetkilerByUserId(string userId)
        //{
        //    return await _sharedContext.UserMakineYetki.Where(x => x.UserId.Equals(userId) && (x.Aktif ?? true) && x.Silindi != true).ToListAsync();
        //}
    }
}
