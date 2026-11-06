using Mapster;
using MapsterMapper;
using Newtonsoft.Json;
using SharedKernel.Core;
using System.Reflection;

namespace PanoKimliklendirme.Application.Services.Core
{
    public class AppService<TModel, TViewModel> : IAppService<TViewModel>
      where TModel : class where TViewModel : class
    {
        protected readonly IMapper _mapper;
        protected readonly IRepository<TModel> _repo;

        public AppService(IMapper mapper,
          IRepository<TModel> repo)
        {
            _mapper = mapper;
            _repo = repo;
        }

        #region Query Operation

        public virtual async Task<IQueryable<TViewModel>> GetAllByIsletmeRefkey()
        {
            var result = await _repo.GetAllAsync();

            //int isletmeRefKey = 0; // todo: _user.GetCurrentUserIsletmeRefKey();

            //bool hasIsletmeValue = isletmeRefKey != 0;
            //bool hasIsletmeProperty = result.FirstOrDefault()?.GetType().GetProperty("IsletmeRefKey") != null;

            //Expression<Func<TViewModel, bool>> predicate = (x) => x.IsletmeRefKey == isletmeRefKey;

            return result.ProjectToType<TViewModel>(_mapper.Config);
            //.WhereIf(hasIsletmeProperty && hasIsletmeValue, predicate);
        }

        public virtual async Task<IQueryable<TViewModel>> GetAllAsync()
        {
            var result = await _repo.GetAllAsync();

            return result.ProjectToType<TViewModel>(_mapper.Config);
        }

        public virtual async Task<List<TViewModel>> GetAllListAsync()
        {
            var result = await _repo.GetAllListAsync();

            return _mapper.Map<List<TViewModel>>(result);
        }

        public async Task<TViewModel> GetByIdAsync(int id)
        {
            var result = await _repo.GetByIdAsync(id);

            return _mapper.Map<TViewModel>(result);
        }

        #endregion Query Operation

        #region CRUD Operations

        public virtual async Task CreateAsync(TViewModel viewModel)
        {
            var model = _mapper.Map<TModel>(viewModel);
            await _repo.AddAsync(model);
            await _repo.SaveChangesAsync();

            //_cachingService.Remove(nameof(TModel));
        }

        public virtual async Task UpdateAsync(TViewModel viewModel)
        {
            var model = _mapper.Map<TModel>(viewModel);
            _repo.Update(model);
            await _repo.SaveChangesAsync();
        }

        public virtual async Task UpdateAsyncDx(int key, string values)
        {
            var model = await _repo.GetByIdAsync(key);
            JsonConvert.PopulateObject(values, model);
            _repo.Update(model);
            await _repo.SaveChangesAsync();
        }

        public virtual async Task RemoveAsync(int id)
        {
            var tEntity = await _repo.GetByIdAsync(id);
            PropertyInfo propInfo = tEntity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).FirstOrDefault(x => x.Name.Equals("Silindi", StringComparison.OrdinalIgnoreCase));
            if (propInfo != null)
            {
                propInfo.SetValue(tEntity, true);
            }
            else
            {
                await _repo.RemoveAsync(id);
            }
            await _repo.SaveChangesAsync();
        }

        #endregion CRUD Operations

        //#region Event Operations

        //public async Task<IQueryable<TViewModel>> GetAllEventAsync()
        //{
        //    var result = await _mongoEventRepository.GetList<TEventModel>(x => x.TableName == ConvertTableName());

        //    return result.ProjectTo<TViewModel>(_mapper.ConfigurationProvider);
        //}

        //public async Task<IQueryable<TViewModel>> GetAllEventByRefKeyAsync(int refKey)
        //{
        //    var result = await _mongoEventRepository.GetList<TEventModel>(x => x.TableName == ConvertTableName() && x.AggregateId == refKey);

        //    return result.ProjectTo<TViewModel>(_mapper.ConfigurationProvider);
        //}

        //public async Task<IQueryable<TViewModel>> GetAllEventByObjectIdAsync(string mongoId)
        //{
        //    var result = await _mongoEventRepository.GetList<TEventModel>(x => x.TableName == ConvertTableName() && x.MongoId == mongoId);

        //    return result.ProjectTo<TViewModel>(_mapper.ConfigurationProvider);
        //}

        //#endregion

        public string ConvertTableName()
        {
            string pureClassName = typeof(TViewModel).ToString().Split('.').Last();

            return pureClassName.Replace("ViewModel", "");
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
