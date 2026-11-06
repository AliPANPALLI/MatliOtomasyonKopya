using MapsterMapper;
using SharedApp.Application.Interfaces;
using SharedApp.Application.ViewModels.ReportViewModels;
using SharedApp.Application.ViewModels.RequestModel;
using SharedApp.Domain.Interfaces;

namespace SharedApp.Application.Services
{
    public class ProductionReportAppService : IProductionReportAppService
    {
        private readonly IMapper _mapper;
        private readonly IProductionReportRepository _productionReportRepository;

        public ProductionReportAppService(IMapper mapper, IProductionReportRepository productionReportRepository)
        {
            _mapper = mapper;
            _productionReportRepository = productionReportRepository;
        }

        public async Task<List<ProductionReportViewModel>> GetProductionReportAsync(ProductionReportRequestModel request, CancellationToken cancellationToken)
        {
            var result = await _productionReportRepository.GetProductionReportAsync(request.Lokasyon, request.BaslangicZamani, request.BitisZamani, cancellationToken);
            return _mapper.Map<List<ProductionReportViewModel>>(result);
        }
    }
}
