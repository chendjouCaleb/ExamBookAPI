using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Social.Entities;
using Social.Helpers;
using Social.Models;
using Social.Repositories;
using Social.Services;

namespace SocialTest
{
    public class ReactionServiceTests
    {
        private IServiceProvider _provider;
        private PostService _postService;
        private AuthorService _authorService;
        private ReactionService _reactionService;
        private IReactionRepository _reactionRepository;

        private Post post;
        private Author author;
        
        [SetUp]
        public async Task SetUp()
        {
            var services = new ServiceCollection();
            _provider = services.Setup().BuildServiceProvider();
            _postService = _provider.GetRequiredService<PostService>();
            _authorService = _provider.GetRequiredService<AuthorService>();
            _reactionService = _provider.GetRequiredService<ReactionService>();
            _reactionRepository = _provider.GetRequiredService<IReactionRepository>();
            
            author = await _authorService.AddAuthorAsync("author name");
            var postModel = new PostAddModel
            {
                AuthorId = author.Id,
                Content = "Post Contenu"
            };
            post = await _postService.AddPostAsync(postModel);
        }

        [Test]
        public async Task AddReactionAsync()
        {
            var reaction = await _reactionService.ReactAsync(post, author, "like");
            reaction = await _reactionRepository.GetByIdAsync(reaction.Id);
            
            Assert.NotNull(reaction!);
            Assert.AreEqual(author.Id, reaction.AuthorId);
            Assert.AreEqual(post.Id, reaction.PostId);
            Assert.AreEqual(StringHelper.Normalize("like"), reaction.Type);
        }
        
        [Test]
        public async Task AddReaction_ShouldNotDuplicateAsync()
        {
            var reaction1 = await _reactionService.ReactAsync(post, author, "like");
            var reaction2 = await _reactionService.ReactAsync(post, author, "unlike");
            reaction2 = await _reactionRepository.GetByIdAsync(reaction2.Id);
            
            Assert.NotNull(reaction2);
            Assert.AreEqual(reaction1.Id, reaction2.Id);
            Assert.AreEqual(StringHelper.Normalize("unlike"), reaction2.Type);
        }

        [Test]
        public async Task DeleteReaction()
        {
            var reaction = await _reactionService.ReactAsync(post, author, "like");
            await _reactionService.DeleteAsync(reaction);
            reaction = await _reactionRepository.GetByIdAsync(reaction.Id);
            
            Assert.Null(reaction);
        }
    }
}