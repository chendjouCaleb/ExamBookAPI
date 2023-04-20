using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DriveIO.Services;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Social.Services;
using Vx.Services;

namespace ExamBook.Services
{
    public class SpaceService
    {
        private readonly DbContext _dbContext;
        private readonly MemberService _memberService;
        private readonly FileService _fileService;
        private readonly FolderService _folderService;
        private readonly EventService _eventService;
        private readonly PublisherService _publisherService;
        private readonly AuthorService _authorService;
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SpaceService> _logger;
        


        public SpaceService(DbContext dbContext, 
            MemberService memberService,
            FileService fileService, 
            FolderService folderService, 
            ILogger<SpaceService> spaceService, 
            IConfiguration configuration, EventService eventService, PublisherService publisherService, UserService userService, AuthorService authorService)
        {
            _dbContext = dbContext;
            _memberService = memberService;
            _logger = spaceService;
            _fileService = fileService;
            _folderService = folderService;
            _configuration = configuration;
            _eventService = eventService;
            _publisherService = publisherService;
            _userService = userService;
            _authorService = authorService;
        }


        /// <summary>
        /// Get space by identifier.
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public async Task<Space> GetAsync(string identifier)
        {
            var normalized = StringHelper.Normalize(identifier);
            var space = await _dbContext.Set<Space>()
                .FirstOrDefaultAsync(s => s.NormalizedIdentifier == normalized);

            if (space == null)
            {
                throw new NullReferenceException();
            }

            return space;
        }

        public async Task<Space> AddAsync(string userId, SpaceAddModel model,
            Stream? image, Stream? coverImage)
        {
            throw new NotImplementedException();
        }

        public async Task<Space> AddAsync(string userId, SpaceAddModel model)
        {
            if (await AnyAsync(model.Identifier))
            {
                throw new InvalidOperationException($"The identifier '{model.Identifier}' is used.");
            }

            var user = await _userService.FindByIdAsync(userId);
            var author = await _userService.GetAuthor(user);

            var publisher = await _publisherService.AddAsync();
            Space space = new()
            {
                Identifier = model.Identifier,
                NormalizedIdentifier = StringHelper.Normalize(model.Identifier),
                Name = model.Name,
                Twitter = model.Twitter,
                Facebook = model.Facebook,
                Youtube = model.Youtube,
                Instagram = model.Instagram,
                Website = model.Website,
                IsPublic = model.IsPublic,
                
                PublisherId = publisher.Id
            };
            await _dbContext.AddAsync(space);

            MemberAddModel adminAddModel = new() {IsAdmin = true, UserId = userId};
            Member admin = await _memberService.CreateMember(space, adminAddModel);

            _eventService.Emit(new[] {publisher}, author, "SPACE_ADD", space);
            
            

            _logger.LogInformation("New space created. Name={}", space.Name);
    
            await _dbContext.AddAsync(admin);
            await _dbContext.SaveChangesAsync();

            return space;
        }


        public async Task ChangeIdentifier(Space space, string identifier)
        {
            if (await AnyAsync(identifier))
            {
                throw new InvalidOperationException($"The identifier '{identifier}' is used.");
            }

            space.Identifier = identifier;
            space.NormalizedIdentifier = StringHelper.Normalize(identifier); 
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }


        public async Task SetAsPublic(Space space)
        {
            Asserts.NotNull(space, nameof(space));
            if (space.IsPublic)
            {
                throw new IllegalOperationException("SpaceIsNotPrivate");
            }

            space.IsPublic = true;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SetAsPrivate(Space space)
        {
            Asserts.NotNull(space, nameof(space));
            if (space.IsPrivate)
            {
                throw new IllegalOperationException("SpaceIsNotPublic");
            }

            space.IsPublic = false;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }


        public async Task SetAsCertified(Space space)
        {
            Asserts.NotNull(space, nameof(space));
            if (space.IsCertified)
            {
                throw new IllegalOperationException($"The space @{space.Identifier} is already certified.");
            }

            space.IsCertified = true;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SetAsNonCertified(Space space)
        {
            Asserts.NotNull(space, nameof(space));
            if (!space.IsCertified)
            {
                throw new IllegalOperationException($"The space @{space.Identifier} is already non certified.");
            }

            space.IsCertified = false;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }


        public async Task ChangeInfo(Space space, SpaceChangeInfoModel model)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNull(model, nameof(model));

            space.Name = model.Name;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ChangeImageAsync(Space space, Stream fileStream)
        {
            if (string.IsNullOrEmpty(space.ImageId))
            {
                var image = await _fileService.FindByIdAsync(space.ImageId);
                await _fileService.DeleteAsync(image!, CancellationToken.None);
            }

            var folderName = _configuration["File:Paths:SpaceImages"]!;
            var folder = await _folderService.FindByNameAsync(folderName);
            var file = await _fileService.AddImageAsync(fileStream, $"{space.Id}.png", folder);
            space.ImageId = file.Id;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }

        public Task ChangeCoverImageAsync(Space space)
        {
            throw new NotImplementedException();
        }

        public async Task ChangeTwitterAsync(Space space, string twitter)
        {
            Asserts.NotNull(space, nameof(space));
            space.Twitter = twitter;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }

        public Task ChangeFacebook(Space space, string facebook)
        {
            throw new NotImplementedException();
        }

        public Task ChangeInstagram(Space space, string instagram)
        {
            throw new NotImplementedException();
        }

        public Task ChangeYoutube(Space space, string youtube)
        {
            throw new NotImplementedException();
        }

        public Task ChangeWebsite(Space space, string url)
        {
            throw new NotImplementedException();
        }


        public async Task Delete(Space space)
        {
            await Destroy(space);
        }


        public async Task<bool> AnyAsync(string identifier)
        {
            var normalizedIdentifier = StringHelper.Normalize(identifier);
            return await _dbContext.Set<Space>()
                .AnyAsync(s => s.NormalizedIdentifier == normalizedIdentifier);
        }


        public async Task MarkAsDeleted(Space space)
        {
            space.DeletedAt = DateTime.Now;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Destroy(Space space)
        {
            var members = _dbContext.Set<Member>().Where(m => m.SpaceId == space.Id);

            _dbContext.RemoveRange(members);
            _dbContext.Remove(space);
            await _dbContext.SaveChangesAsync();
        }
    }
}