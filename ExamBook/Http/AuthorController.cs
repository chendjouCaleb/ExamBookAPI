using System.Collections.Generic;
using System.Threading.Tasks;
using ExamBook.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social.Entities;
using Social.Services;

namespace ExamBook.Http
{
    [Route("api/authors")]
    public class AuthorController:ControllerBase
    {
        private ApplicationSocialDbContext _dbContext;
        private AuthorService _authorService;

        public AuthorController(ApplicationSocialDbContext dbContext, AuthorService authorService)
        {
            _dbContext = dbContext;
            _authorService = authorService;
        }

        [HttpGet]
        public async Task<IEnumerable<Author>> ListAsync()
        {
            return await _dbContext.Authors.ToListAsync();
        }


        [HttpGet("{id}")]
        public async Task<Author> Get(string id)
        {
            return await _authorService.FindAuthorAsync(id);
        }

        
        [HttpPost]
        public async Task<Author> AddAsync([FromQuery]string name)
        {
            var author = await _authorService.AddAuthorAsync(name);

            return author;
        }

        
        [HttpDelete("{id}")]
        public async Task<NoContentResult> DeleteAsync(string id)
        {
            await _authorService.DeleteAsync(id);
            return NoContent();
        }
    }
}