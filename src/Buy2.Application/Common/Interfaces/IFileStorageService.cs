using Microsoft.AspNetCore.Http;

namespace Buy2.Application.Common.Interfaces;
public interface IFileStorageService
{
    public Task<string> UploadAsync(string fileName, IFormFile file);
}