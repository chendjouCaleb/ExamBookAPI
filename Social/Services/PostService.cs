using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DriveIO.Helpers;
using DriveIO.Models;
using Microsoft.Extensions.Logging;
using Social.Entities;
using Social.Models;
using Social.Repositories;
using Vx.Models;
using Vx.Services;

namespace Social.Services
{
    public class PostService
    {
        private readonly IPostRepository _postRepository;
        private readonly IAuthorRepository _authorRepository;
        private readonly IRepostRepository _repostRepository;
        private readonly PublisherService _publisherService;
        private readonly ActorService _actorService;
        private readonly SubscriptionService _subscriptionService;
        private readonly EventService _eventService;
        private readonly ILogger<PostService> _logger;

        public PostService(IPostRepository postRepository, 
            IAuthorRepository authorRepository, 
            ILogger<PostService> logger, 
            SubscriptionService subscriptionService, 
            PublisherService publisherService,
            EventService eventService, ActorService actorService, IRepostRepository repostRepository)
        {
            _postRepository = postRepository;
            _authorRepository = authorRepository;
            _logger = logger;
            _subscriptionService = subscriptionService;
            _publisherService = publisherService;
            _eventService = eventService;
            _actorService = actorService;
            _repostRepository = repostRepository;
        }


        public async Task<Post> FindByAsync(long id)
        {
            var post = await _postRepository.FindAsync(id);

            if (post == null)
            {
                throw new InvalidOperationException($"Post with id={id} not found");
            }

            return post;
        }

        public async Task<IEnumerable<PostFile>> GetPostFiles(Post post)
        {
            return await _authorRepository.GetPostFilesAsync(post);
        }

        public async Task<Post> AddPostAsync(PostAddModel model)
        {
            return await AddPostAsync(model, null);
        }
        

        public async Task<Post> AddPostAsync(PostAddModel model, Post? parent)
        {
            var author = await _authorRepository.GetByIdAsync(model.AuthorId);

            if (author == null)
            {
                throw new InvalidOperationException($"Author with id={model.AuthorId} not found.");
            }

            var publisher = await _publisherService.AddAsync();
            long? parentPostId = null;
            if (parent != null)
            {
                parentPostId = parent.Id;
            }
            
            Post post = new()
            {
                Author = author,
                Content = model.Content,
                MetaData = model.MetaData,
                PublisherId = publisher.Id,
                ParentPostId = parentPostId,
                ParentPost = parent
            };
            
            await _postRepository.SaveAsync(post);

            var authorPublisher = await _publisherService.GetByIdAsync(author.PublisherId);
            var authorActor = await _actorService.GetByIdAsync(author.ActorId);
            
            await _eventService.EmitAsync(authorPublisher, authorActor, "POST_ADD", new {PostId = post.Id});
            _logger.LogInformation("New post; id={}", post.Id);

            return post;
        }


        public async Task<Repost> AddRepost(Post post, Author author)
        {
            if (await _repostRepository.ExistsByPostAuthor(post, author))
            {
                var m = $"The post[id={post.Id}] already reposted by author[id={author.Id}].";
                throw new InvalidOperationException(m);
            }

            var repost = new Repost
            {
                Author = author,
                ChildPost = post,
                ChildPostId = post.Id
            };

            await _repostRepository.SaveAsync(repost);
            return repost;
        }

        public async Task<bool> HasRepost(Post post, Author author)
        {
            return await _repostRepository.ExistsByPostAuthor(post, author);
        }


        public async Task<PostFile> AddPictureAsync(Post post, Picture picture, Picture thumb)
        {
            Asserts.NotNull(post, nameof(post));
            Asserts.NotNull(picture, nameof(picture));
            Asserts.NotNull(thumb, nameof(thumb));
            
            PostFile postFile = new()
            {
                FileId = picture.Id,
                ThumbId = thumb.Id,
                IsPicture = true,
                Post = post
            };
            await _postRepository.SavePostFileAsync(postFile);
            return postFile;
        }


        public async Task<Subscription> Subscribe(Post post, Author author)
        {
            Asserts.NotNull(post, nameof(post));
            Asserts.NotNull(author, nameof(author));

            var publisher = await _publisherService.GetByIdAsync(post.PublisherId);
            Asserts.NotNull(publisher, nameof(publisher));
            
            var subscriber = await _subscriptionService.SubscribeAsync(publisher);
            return subscriber;
        }

        public async Task DeleteAsync(Post post)
        {
            await _postRepository.DeleteAsync(post);
        }

        public async Task DeleteAsync(long id)
        {
            var post = await FindByAsync(id);
            await DeleteAsync(post);
        }
    }
}