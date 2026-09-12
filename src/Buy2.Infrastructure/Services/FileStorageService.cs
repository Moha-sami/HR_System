using Buy2.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Buy2.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    public async Task<string> UploadAsync(string fileName,IFormFile file)
    {
        var uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(),
            "storage", "uploads");

        Directory.CreateDirectory(uploadDirectory);

        var filePath = Path.Combine(uploadDirectory, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);

        await file.CopyToAsync(stream);

        return filePath;
    }
}