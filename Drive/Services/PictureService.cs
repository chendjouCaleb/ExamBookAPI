using System.IO;
using System.Threading.Tasks;
using DriveIO.Helpers;
using DriveIO.Models;
using DriveIO.Repositories;
using DriveIO.Stores;
using Microsoft.Extensions.Logging;

namespace DriveIO.Services
{
    public class PictureService
    {
        private readonly IPictureRepository _pictureRepository;
        private readonly IFileRepository _fileRepository;
        private readonly IFileStore _fileStore;
        private readonly DriveOptions _options;
        private readonly ILogger<PictureService> _logger;

        public PictureService(ILogger<PictureService> logger, 
            IPictureRepository pictureRepository, 
            IFileStore fileStore, 
            DriveOptions options, IFileRepository fileRepository)
        {
            _logger = logger;
            _pictureRepository = pictureRepository;
            _fileStore = fileStore;
            _options = options;
            _fileRepository = fileRepository;
        }
        
        public async Task<Picture> GetImageInfo(string id)
        {
            var image = await _pictureRepository.GetByIdAsync(id);

            if (image == null)
            {
                
            }

            return image!;
        }

        public async Task<Picture> AddImageAsync(Folder folder, Stream stream, AddPictureOptions options)
        {
            string normalizedFileName = StringHelper.Normalize(options.FileName);
            var picture = new Picture
            {
                NormalizedName = normalizedFileName,
                Name = options.FileName,
                Folder = folder
            };

            await _fileRepository.SaveAsync(picture);

            _fileStore.WriteFileAsync(stream, picture);
            
            _logger.LogInformation("New image uploaded");

            return picture;
        }

        
    }
}