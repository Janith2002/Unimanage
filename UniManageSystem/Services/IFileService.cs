using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace UniManageSystem.Services
{
    /// <summary>
    /// Abstracts physical file system operations to improve testability and adhere to the Single Responsibility Principle.
    /// </summary>
    public interface IFileService
    {
        Task<string> UploadFileAsync(IFormFile file, string subFolder);
        void DeleteFile(string fileName, string subFolder);
    }
}
