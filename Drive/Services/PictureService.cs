using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DriveIO.Helpers;
using DriveIO.Models;
using DriveIO.Repositories;
using DriveIO.Stores;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DriveIO.Services
{
    public class PictureService
    {
        private readonly IPictureRepository _pictureRepository;
        private readonly IFileStore _fileStore;
        private readonly DriveOptions _options;
        private readonly ILogger<PictureService> _logger;

        public PictureService(ILogger<PictureService> logger, 
            IPictureRepository pictureRepository, 
            IFileStore fileStore, 
            DriveOptions options)
        {
            _logger = logger;
            _pictureRepository = pictureRepository;
            _fileStore = fileStore;
            _options = options;
        }

        public async Task<IEnumerable<Picture>> ListAsync()
        {
            return await _pictureRepository.ListAsync();
        }

        public async Task<Picture> Get(string id)
        {
            var image = await _pictureRepository.GetByIdAsync(id);

            if (image == null)
            {
                throw new InvalidOperationException($"Picture with id: {id} not found.");
            }

            return image;
        }


        public Stream GetPictureStream(Picture picture)
        {
            Asserts.NotNull(picture.Folder, nameof(picture));
            return _fileStore.GetStreamAsync(picture);
        }

        public async Task<IEnumerable<Picture>> AddImagesAsync(Folder folder, Stream source,
            IEnumerable<AddPictureOptions> optionsList)
        {
            var pictures = new List<Picture>();

            var encoder = new JpegEncoder{Quality = 71};
            var image = await Image.LoadAsync(source);
            foreach (var options in optionsList)
            {
                double ratio = image.Width / (double)image.Height;
                var height = options.Height;
                if (image.Height < height)
                {
                    height = image.Height;
                }
                var width = Convert.ToInt32( height * ratio) ;
                string normalizedName = StringHelper.Normalize(options.FileName);
                var picture = new Picture
                {
                    NormalizedName = normalizedName,
                    Name = options.FileName,
                    Folder = folder
                };

                var resizeOptions = new ResizeOptions
                {
                    Size = new Size(width, height)
                };
                var imageItem = image.Clone(context =>
                {
                    context.Resize(resizeOptions);
                });
                
                var outputStream = new MemoryStream();
                await imageItem.SaveAsync(outputStream, encoder);
                outputStream.Position = 0;

                await _pictureRepository.SaveAsync(picture);
                await _fileStore.WriteFileAsync(outputStream, picture);
                
                pictures.Add(picture);
                
            }


            return pictures;
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

            await _pictureRepository.SaveAsync(picture);

            Image image = await Image.LoadAsync<Rgba32>(stream);
            var encoder = new JpegEncoder {Quality = 70};
            //var encoder = new JpegEncoder();
            
            var resizeOptions = new ResizeOptions
            {
                Size = new Size(722, 900),
                Mode = ResizeMode.Min,
            };
        
            image.Mutate(x => x.Resize(resizeOptions));
            
            

            var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, encoder);
            outputStream.Position = 0;

            _fileStore.WriteFileAsync(outputStream, picture);
            
            _logger.LogInformation("New image uploaded");

            return picture;
        }

        
        
        
    }
}