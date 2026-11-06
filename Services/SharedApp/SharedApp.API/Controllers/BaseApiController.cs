using Microsoft.AspNetCore.Mvc;
using SharedKernel.Core;
using DevExtreme.AspNet.Mvc;
using DevExtreme.AspNet.Data;
using SharedKernel.DevExpressModel;

namespace SharedApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseApiController<TViewModel> : ControllerBase where TViewModel : class
    {
        private readonly IAppService<TViewModel> _baseAppService;

        protected BaseApiController(IAppService<TViewModel> baseTanimAppService)
        {
            _baseAppService = baseTanimAppService;
        }

        protected JsonResult Json(object value)
        {
            return new JsonResult(value);
        }

        #region Query Operations    

        [HttpGet("GetAllDx")]
        public async Task<IActionResult> GetAllDx(DataSourceLoadOptions loadOptions)
        {
            var result = await GetList();
            return Json(await DataSourceLoader.LoadAsync(result, loadOptions));
        }

        //[FunctionAuthorize]
        [HttpGet("GetAllWithoutPaging")]
        public async Task<IActionResult> GetAllWithoutPaging()
        {
            IQueryable<TViewModel> result = await GetList();

            return Json(result);
        }

        [HttpGet("GetAllList")]
        public async Task<IActionResult> GetAllList()
        {
            var result = await _baseAppService.GetAllListAsync();
            return Json(result);
        }

        private async Task<IQueryable<TViewModel>> GetList()
        {
            var result = await _baseAppService.GetAllAsync();

            return result;
        }

        [HttpGet("GetById/{Id}")]
        public async Task<IActionResult> GetById(int Id)
        {
            var result = await _baseAppService.GetByIdAsync(Id);

            return Json(result);
        }

        #endregion Query Operations

        #region CRUD operations

        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] TViewModel tEntity)
        {
            await _baseAppService.CreateAsync(tEntity);

            return Ok();
        }

        [HttpPut("Update")]
        public async Task<IActionResult> Update([FromBody] TViewModel tEntity)
        {
            await _baseAppService.UpdateAsync(tEntity);

            return Ok();
        }

        [HttpPut("UpdateDx")]
        public async Task<IActionResult> UpdateDx([FromBody] DxUpdateModel dxUpdateModel)
        {
            await _baseAppService.UpdateAsyncDx(dxUpdateModel.Key, dxUpdateModel.Values);
            return Ok();
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _baseAppService.RemoveAsync(id);

            return Ok();
        }

        #endregion CRUD operations
    }
}
