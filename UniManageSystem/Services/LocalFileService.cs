using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UniManageSystem.Services
{
    public class LocalFileService : IFileService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        public LocalFileService(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null.");

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, subFolder);
            if (!Directory.Exists(uploadsFolder)) 
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            
            string uniqueFileName = Guid.NewGuid().ToString("N") + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return uniqueFileName;
        }

        public void DeleteFile(string fileName, string subFolder)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, subFolder, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}
