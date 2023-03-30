using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DriveIO.Services;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Models;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExamBook.Services
{
    public class SpaceService
    {
        private readonly DbContext _dbContext;
        private readonly MemberService _memberService;
        private readonly FileService _fileService;
        private readonly FolderService _folderService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SpaceService> _logger;


        public SpaceService(DbContext dbContext, MemberService memberService, ILogger<SpaceService> spaceService)
        {
            _dbContext = dbContext;
            _memberService = memberService;
            _logger = spaceService;
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

            Space space = new()
            {
                Identifier = model.Identifier,
                NormalizedIdentifier = StringHelper.Normalize(model.Identifier),
                Name = model.Name,
                Twitter = model.Twitter,
                Facebook = model.Facebook,
                Youtube = model.Youtube,
                Instagram = model.Instagram,
                Website = model.Website
            };
            await _dbContext.AddAsync(space);

            MemberAddModel adminAddModel = new() {IsAdmin = true, UserId = userId};
            Member admin = await _memberService.CreateMember(space, adminAddModel);


            _logger.LogInformation("New space created. Name={}", space.Name);

            await _dbContext.AddAsync(admin);
            await _dbContext.SaveChangesAsync();

            return space;
        }


        public async Task ChangeIdentifier(Space space, ChangeSpaceIdentifierModel model)
        {
            if (await AnyAsync(model.Identifier))
            {
            }

            space.Identifier = model.Identifier;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }


        public async Task SetAsPublic(Space space)
        {
            Asserts.NotNull(space, nameof(space));
            if (space.IsPublic)
            {
            }

            space.IsPublic = true;
            _dbContext.Update(space);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SetAsPrivate(Space space)
        {
            Asserts.NotNull(space, nameof(space));
            if (!space.IsPublic)
            {
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