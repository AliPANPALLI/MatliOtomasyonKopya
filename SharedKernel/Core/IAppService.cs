namespace SharedKernel.Core
{
    public interface IAppService<TViewModel> : IDisposable where TViewModel : class
    {
        #region Query Operations

        Task<IQueryable<TViewModel>> GetAllAsync();
        Task<List<TViewModel>> GetAllListAsync();
        Task<TViewModel> GetByIdAsync(int id);

        #endregion Query Operations

        #region CRUD Operations

        Task CreateAsync(TViewModel viewModel);

        Task UpdateAsync(TViewModel viewModel);

        Task UpdateAsyncDx(int key, string values);

        Task RemoveAsync(int id);

        #endregion CRUD Operations

        //#region Event Operations

        //Task<IQueryable<TViewModel>> GetAllEventAsync();
        //Task<IQueryable<TViewModel>> GetAllEventByRefKeyAsync(int refKey);
        //Task<IQueryable<TViewModel>> GetAllEventByObjectIdAsync(string mongoId);

        //#endregion
    }
}
