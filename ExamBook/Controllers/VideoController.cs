using System.IO;
using DriveIO.Repositories;
using DriveIO.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExamBook.Controllers
{
    
    [Route("api/videos")]
    public class VideoController
    {
        
        private readonly ILogger<PictureController> _logger;
        private readonly VideoService _videoService;
        private readonly IFolderRepository _folderRepository;

        public VideoController(ILogger<PictureController> logger, 
            VideoService videoService, 
            IFolderRepository folderRepository)
        {
            _logger = logger;
            _videoService = videoService;
            _folderRepository = folderRepository;
        }

        
        
        public void Download()
        {
            FileStream stream = new FileStream("", FileMode.Open);
        }
    }
}