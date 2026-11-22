using Microsoft.AspNetCore.Http;

namespace EduHub.Services.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string folder = "general");
    Task<string> UploadStreamAsync(Stream stream, string fileName, string contentType, string folder = "general");
    Task<bool> DeleteFileAsync(string fileUrl);
    string GenerateSecureStreamingUrl(string originalVideoUrl, int expiryMinutes = 120);
}
