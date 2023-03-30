using System;
using System.IO;
using System.Threading.Tasks;
using DriveIO.Helpers;
using DriveIO.Models;
using DriveIO.Repositories;
using DriveIO.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExamBook.Http
{
    
    [Route("api/pictures")]
    public class PictureController: ControllerBase
    {
        private readonly ILogger<PictureController> _logger;
        private readonly PictureService _pictureService;
        private readonly IFolderRepository _folderRepository;
        

        public PictureController(
            ILogger<PictureController> logger, 
            PictureService pictureService, 
            IFolderRepository folderRepository)
        {
            _logger = logger;
            _pictureService = pictureService;
            _folderRepository = folderRepository;
        }
        

        [HttpGet("{id}")]
        public async Task<Picture> Get(string id)
        {
            return await _pictureService.GetImageInfo(id);
        }

        [HttpGet("download")]
        public async Task<FileResult> Download([FromQuery] string pictureId)
        {
            var picture = await _pictureService.GetImageInfo(pictureId);

            string path = $"E:/Lab/Drive/{picture.Name}";
            
            var memoryStream = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memoryStream);
            }

            memoryStream.Position = 0;
            var ext = Path.GetExtension(path);
            var mime = MimeHelper.GetMime(ext);

            //Response.Headers.Add("Content-Disposition", "attachment;filename=some.txt");
            Response.Headers.Add("Content-Disposition", $"inline;filename={picture.Name}");
           
            return File(memoryStream, mime);
        }


        [HttpPost]
        public async Task<OkObjectResult> Upload(IFormFile file)
        {
            var folder = await _folderRepository.FindByNameAsync("images");
            var fileName = $"{Guid.NewGuid()}.{Path.GetExtension(file.FileName)}";
            AddPictureOptions options = new()
            {
                FileName = fileName
            };
            
            var image = await _pictureService.AddImageAsync(folder!, file.OpenReadStream(), options);
            _logger.LogInformation("New photo uploaded");
            
            return Ok(image);
        }
    }
}