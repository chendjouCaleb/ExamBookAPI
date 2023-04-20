using System;
using System.IO;
using System.Threading.Tasks;
using DriveIO.Stores;

namespace DriveIO.LocalStores
{
    public class LocalFolderStore:IFolderStore
    {
        private readonly LocalFileStoreOptions _options;

        public LocalFolderStore(LocalFileStoreOptions options)
        {
            _options = options;
        }

        public async Task CreateAsync(string name)
        {
            await Task.Run(() =>
            {
                string path = Path.Join(_options.DirectoryPath, name);
                Directory.CreateDirectory(path);
            });
        }

        public Task CreateIfNotExistsAsync(string name)
        {
            string path = Path.Join(_options.DirectoryPath, name);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return Task.CompletedTask;
        }

        public Task<bool> ContainsAsync(string name)
        {
            string path = Path.Join(_options.DirectoryPath, name);
            var exists = Directory.Exists(path);
            return Task.FromResult(exists);
        }

        public Task DeleteAsync(string name)
        {
            string path = Path.Join(_options.DirectoryPath, name);
            Directory.Delete(path);
            return Task.CompletedTask;
        }
    }
}