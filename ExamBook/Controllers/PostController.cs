using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DriveIO.Repositories;
using ExamBook.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social.Entities;
using Social.Models;
using Social.Services;

namespace ExamBook.Controllers
{
    
    [Route("api/posts")]
    public class PostController:ControllerBase
    {
        private readonly PostService _postService;
        private readonly IPictureRepository _pictureRepository;
        private readonly ApplicationSocialDbContext _dbContext;

        public PostController(PostService postService, 
            ApplicationSocialDbContext dbContext, 
            IPictureRepository pictureRepository)
        {
            _postService = postService;
            _dbContext = dbContext;
            _pictureRepository = pictureRepository;
        }

        [HttpGet]
        public async Task<IEnumerable<Post>> List()
        {
            var query = _dbContext.Set<Post>()
                .OrderByDescending(p => p.Id)
                .Include(p => p.Files);
                
            
            return await query.ToListAsync();
        }


        [HttpGet("{id}")]
        public async Task<Post> Find(long id)
        {
            return await _postService.FindByAsync(id);
        }

        [HttpGet("{id}/files")]
        public async Task<IEnumerable<PostFile>> GetPostFiles(long id)
        {
            var post = await _postService.FindByAsync(id);
            return await _postService.GetPostFiles(post);
        }



        [HttpPost]
        public Task<Post> AddPostAsync([FromBody] PostAddModel model)
        {
            return _postService.AddPostAsync(model);
        }

        [HttpPost("picture")]
        public async Task<PostFile> AddPostPicture([FromBody] PostAddPictureModel model)
        {
            var picture = await _pictureRepository.GetByIdAsync(model.FileId);
            var thumb = await _pictureRepository.GetByIdAsync(model.ThumbId);
            var post = await _postService.FindByAsync(model.PostId);

            var postFile = await _postService.AddPictureAsync(post!, picture!, thumb!);
            return postFile;
        }


        [HttpDelete("{id}")]
        public async Task<NoContentResult> DeletePost(long id)
        {
            await _postService.DeleteAsync(id);
            return NoContent();
        }

    }
}