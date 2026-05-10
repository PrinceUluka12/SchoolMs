using Microsoft.AspNetCore.Http;

namespace SchoolMS.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string folder);
    Task DeleteFileAsync(string fileUrl);
    string GetFileUrl(string relativePath);
}