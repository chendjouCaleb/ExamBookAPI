using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Social.Entities;
using Social.Models;
using Social.Repositories;

namespace Social.Services
{
    public class PostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IAuthorRepository _authorRepository;
        private readonly ILogger<PostService> _logger;

        public PostService(IPostRepository postRepository, 
            IAuthorRepository authorRepository, 
            ILogger<PostService> logger)
        {
            _postRepository = postRepository;
            _authorRepository = authorRepository;
            _logger = logger;
        }


        public async Task<Post> FindByAsync(string id)
        {
            return await _postRepository.FindAsync(id);
        }
        
        public async Task<Post> AddPostAsync(PostAddModel model)
        {
            var author = await _authorRepository.FindByIdAsync(model.AuthorId);

            if (author == null)
            {
                throw new InvalidOperationException($"Author with id={model.AuthorId} not found.");
            }
            
            Post post = new Post
            {
                Author = author,
                Content = model.Content,
                MetaData = model.MetaData
            };

            await _postRepository.SaveAsync(post);
            _logger.LogInformation("New post; id={}", post.Id);

            return post;
        }

        public async Task DeleteAsync(Post post)
        {
            await _postRepository.DeleteAsync(post);
        }

        public async Task DeleteAsync(string id)
        {
            var post = await FindByAsync(id);
            await DeleteAsync(post);
        }
    }
}