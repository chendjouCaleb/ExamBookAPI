using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DriveIO.Helpers;
using DriveIO.Models;
using DriveIO.Repositories;
using DriveIO.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExamBook.Controllers
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

        [HttpGet]
        public async Task<IEnumerable<Picture>> List()
        {
            return await _pictureService.ListAsync();
        }

        [HttpGet("{id}")]
        public async Task<Picture> Get(string id)
        {
            return await _pictureService.Get(id);
        }

        [HttpGet("download")]
        public async Task<FileResult> Download([FromQuery] string pictureId)
        {
            var picture = await _pictureService.Get(pictureId);

            var stream = _pictureService.GetPictureStream(picture);
            
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            await stream.DisposeAsync();

            memoryStream.Position = 0;
            var ext = Path.GetExtension(picture.Name);
            var mime = MimeHelper.GetMime(ext);

            //Response.Headers.Add("Content-Disposition", "attachment;filename=some.txt");
            Response.Headers.Add("Content-Disposition", $"inline;filename={picture.Name}");
           
            return File(memoryStream, mime);
        }


        [HttpPost]
        public async Task<OkObjectResult> Upload(IFormFile file)
        {
            var folder = await _folderRepository.FindByNameAsync("images");
            AddPictureOptions options = new()
            {
                FileName = $"{DateTime.Now.Ticks}1{Path.GetExtension(file.FileName)}",
                Height = 900
            };

            AddPictureOptions thumbOptions = new()
            {
                FileName = $"{DateTime.Now.Ticks}1{Path.GetExtension(file.FileName)}",
                Height = 80
            };
            
            var pictures = await _pictureService.AddImagesAsync(folder!, file.OpenReadStream(),
                new []{options, thumbOptions});
            _logger.LogInformation("New photo uploaded");
            
            return Ok(pictures);
        }
    }
}