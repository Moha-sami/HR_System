using Microsoft.AspNetCore.Http;

namespace Buy2.Application.Common.Interfaces;

public interface IExcelVoucherParserService
{
    public Task<List<string>> UploadExcelFileAsync(IFormFile file, CancellationToken cancellation);
}