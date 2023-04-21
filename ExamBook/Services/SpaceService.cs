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
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vx.Models;
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
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SpaceService> _logger;
        


        public SpaceService(DbContext dbContext, 
            MemberService memberService,
            FileService fileService, 
            FolderService folderService, 
            ILogger<SpaceService> spaceService, 
            IConfiguration configuration, EventService eventService, PublisherService publisherService, UserService userService)
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

        public async Task<Publisher> GetPublisherAsync(Space space)
        {
            if (string.IsNullOrWhiteSpace(space.PublisherId))
            {
                throw new IllegalStateException("SpaceHasNoPublisher");
            }

            return await _publisherService.GetByIdAsync(space.PublisherId);
        }

       

        public async Task<ActionResultModel<Space>> AddAsync(string userId, SpaceAddModel model)
        {
            if (await AnyAsync(model.Identifier))
            {
                throw new UsedValueException("SpaceIdentifierUsed");
            }

            var user = await _userService.FindByIdAsync(userId);
            var actor = await _userService.GetActor(user);

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
            Member admin = _memberService.NewMember(space, adminAddModel);

            var @event = await _eventService.EmitAsync(new[] {publisher}, actor, "SPACE_ADD", space);

            _logger.LogInformation("New space created. Name={}", space.Name);
    
            await _dbContext.AddAsync(admin);
            await _dbContext.SaveChangesAsync();

            return new ActionResultModel<Space>(space, @event);
        }


        public async Task<Event> ChangeIdentifier(Space space, string identifier, User user)
        {
            if (await AnyAsync(identifier))
            {
                throw new UsedValueException("SpaceIdentifierUsed");
            }

            var publisher = await GetPublisherAsync(space);
            var actor = await _userService.GetActor(user);
            var data = new ChangeValueData<string>(space.Identifier, identifier);
            
            space.Identifier = identifier;
            space.NormalizedIdentifier = StringHelper.Normalize(identifier); 
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
            
            return await _eventService.EmitAsync(new[] {publisher}, actor, "SPACE_CHANGE_IDENTIFIER", data);
        }
        
        
        public async Task<Event> ChangeName(Space space, string name, User user)
        {
            var publisher = await GetPublisherAsync(space);
            var actor = await _userService.GetActor(user);
            var data = new ChangeValueData<string>(space.Name, name);
            
            space.Name = name;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
            
            return await _eventService.EmitAsync(new[] {publisher}, actor, "SPACE_CHANGE_NAME", data);
        }


        public async Task<Event> SetAsPublic(Space space, User user)
        {
            Asserts.NotNull(space, nameof(space));
            if (space.IsPublic)
            {
                throw new IllegalOperationException("SpaceIsNotPrivate");
            }
            var publisher = await GetPublisherAsync(space);
            var actor = await _userService.GetActor(user);

            space.IsPublic = true;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();

            return await _eventService.EmitAsync(publisher, actor, "SPACE_AS_PUBLIC", new { });
        }

        public async Task<Event> SetAsPrivate(Space space, User user)
        {
            Asserts.NotNull(space, nameof(space));
            if (space.IsPrivate)
            {
                throw new IllegalOperationException("SpaceIsNotPublic");
            }
            
            var publisher = await GetPublisherAsync(space);
            var actor = await _userService.GetActor(user);

            space.IsPublic = false;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
            
            return await _eventService.EmitAsync(publisher, actor, "SPACE_AS_PRIVATE", new { });
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