using KumesScada.Application.Interfaces;
using KumesScada.Application.RequestModels;
using KumesScada.Application.ViewModels;
using MapsterMapper;

namespace KumesScada.Application.Services
{
    public class VGExcelImportService
        : IVGExcelImportService
    {
        private readonly IVGExcelReader _excelReader;

        private readonly IMapper _mapper;


        public VGExcelImportService(
            IVGExcelReader excelReader,
            IMapper mapper)
        {
            _excelReader = excelReader;
            _mapper = mapper;
        }


        public async Task<VGExcelImportViewModel> ImportAsync(
    VGExcelImportRequest request,
    CancellationToken cancellationToken = default)
        {
            var result = await _excelReader.ReadAsync(
                request.FileStream,
                request.FileName,
                request.Year,
                cancellationToken);

            var viewModel = new VGExcelImportViewModel
            {
                FileName = result.FileName,
                ImportDate = result.ImportDate,
                Warnings = result.Warnings,

                Data = _mapper.Map<List<VGExcelKumesGunlukViewModel>>(
                    result.Data)
            };

            viewModel.TotalRecordCount = viewModel.Data.Count;

            return viewModel;
        }
    }
}