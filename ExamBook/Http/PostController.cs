using System.Collections.Generic;
using System.Threading.Tasks;
using ExamBook.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social.Entities;
using Social.Models;
using Social.Repositories;
using Social.Services;

namespace ExamBook.Http
{
    
    [Route("api/posts")]
    public class PostController:ControllerBase
    {
        private readonly PostService _postService;
        private readonly ApplicationSocialDbContext _dbContext;

        public PostController(PostService postService, ApplicationSocialDbContext dbContext)
        {
            _postService = postService;
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IEnumerable<Post>> List()
        {
            var query = _dbContext.Set<Post>();


            return await query.ToListAsync();
        }


        [HttpGet("{id}")]
        public async Task<Post> Find(string id)
        {
            return await _postService.FindByAsync(id);
        }


        [HttpPost]
        public Task<Post> AddPostAsync([FromBody] PostAddModel model)
        {
            return _postService.AddPostAsync(model);
        }


        [HttpDelete("{id}")]
        public async Task<NoContentResult> DeletePost(string id)
        {
            await _postService.DeleteAsync(id);
            return NoContent();
        }

    }
}