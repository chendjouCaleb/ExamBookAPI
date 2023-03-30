using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DriveIO.Models;
using DriveIO.Stores;
using Microsoft.Extensions.Logging;

namespace DriveIO.Services
{
    public class FileService
    {
        private readonly IFileStore _fileStore;
        private readonly ILogger<FileService> _logger;


        public async Task<BaseFile> AddImageAsync(Stream stream, string fullName, Folder folder)
        {
            Picture file = new();
            
            
            return file;
        }

        public async Task<BaseFile?> FindByIdAsync(string id)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteAsync(string id)
        {
            throw new NotImplementedException();
        }
        
        public async Task DeleteAsync(BaseFile file, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        
        public async Task DeleteOrThrowAsync(BaseFile file, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}