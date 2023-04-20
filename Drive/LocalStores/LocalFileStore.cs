using System.IO;
using System.Threading.Tasks;
using DriveIO.Helpers;
using DriveIO.Models;
using DriveIO.Stores;

namespace DriveIO.LocalStores
{
    public class LocalFileStore:IFileStore
    {
        private readonly LocalFileStoreOptions _options;

        public LocalFileStore(LocalFileStoreOptions options)
        {
            _options = options;
        }

        public async Task WriteFileAsync(Stream stream, BaseFile file)
        {
            Asserts.NotNull(file.Folder, nameof(file.Folder));
            var folder = file.Folder!;
            string filePath = Path.Join(_options.DirectoryPath, folder.Name, file.Name);

            await using var fileStream = new FileStream(filePath, FileMode.Create);
            await stream.CopyToAsync(fileStream);
        }

        public Task DeleteAsync(BaseFile file)
        {
            var folder = file.Folder!;
            string filePath = Path.Join(_options.DirectoryPath, folder.Name, file.Name);
            File.Delete(filePath);
            
            return Task.CompletedTask;
        }

        public Stream GetStreamAsync(BaseFile file)
        {
            var folder = file.Folder!;
            string filePath = Path.Join(_options.DirectoryPath, folder.Name, file.Name);
            return new FileStream(filePath, FileMode.Open);
        }
    }
}