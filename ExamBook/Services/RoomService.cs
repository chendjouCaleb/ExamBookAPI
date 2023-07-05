using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Identity.Models;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vx.Models;
using Vx.Services;

namespace ExamBook.Services
{
    public class RoomService
    {
        private readonly DbContext _dbContext;
        private readonly PublisherService _publisherService;
        private readonly EventService _eventService;
        private readonly ILogger<RoomService> _logger;

        public RoomService(DbContext dbContext, ILogger<RoomService> logger, PublisherService publisherService, EventService eventService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publisherService = publisherService;
            _eventService = eventService;
        }

        public async Task<Room> GetRoomAsync(ulong id)
        {
            var room = await _dbContext.Set<Room>().Where(r => r.Id == id)
                .Include(r => r.Space)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new ElementNotFoundException("RoomNotFound", id);
            }

            return room;
        }

        public async Task<List<Room>> GetRoomsAsync(ICollection<ulong> ids)
        {
            var rooms = await _dbContext.Set<Room>().Where(r => ids.Contains(r.Id))
                .Include(r => r.Space)
                .ToListAsync();

            var notFounds = ids.Where(id => rooms.All(r => r.Id != id)).ToList();
            if (notFounds.Count > 0)
            {
                throw new ElementNotFoundException("RoomNotFound", notFounds);
            }

            return rooms;
        }

        public async Task<ActionResultModel<Room>> AddRoomAsync(Space space, RoomAddModel model, User user)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(model));

            string normalizedName = StringHelper.Normalize(model.Name);
            
            if (await ContainsAsync(space, model.Name))
            {
                throw new UsedValueException("RoomNameUsed");
            }

            if (model.Capacity < 5)
            {
                RoomHelper.ThrowMinimalCapacityError(5);
            }

            var publisher = await _publisherService.AddAsync();

            Room room = new()
            {
                Name = model.Name,
                NormalizedName = normalizedName,
                Capacity = model.Capacity,
                Space = space,
                PublisherId = publisher.Id
            };

            _dbContext.Add(room);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new[] {publisher.Id, space.PublisherId};
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "ROOM_ADD", room);
            
            _logger.LogInformation("New room created: {}", room.NormalizedName);
            return new ActionResultModel<Room>(room, @event);
        }

        public async Task<Event> ChangeNameAsync(Room room, RoomChangeNameModel model, User user)
        {
            AssertHelper.NotNull(room.Space, nameof(room.Space));
            
            if (await ContainsAsync(room.Space, model.Name))
            {
                throw new UsedValueException("RoomNameUsed");
            }

            var data = new ChangeValueData<string>(room.Name, model.Name);
            room.Name = model.Name;
            room.NormalizedName = StringHelper.Normalize(model.Name);
            _dbContext.Update(room);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new[] {room.PublisherId, room.Space.PublisherId};
            
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "ROOM_CHANGE_NAME", data);
        }
        
        public async Task<Event> ChangeCapacityAsync(Room room, RoomChangeCapacityModel model, User user)
        {
            AssertHelper.NotNull(room.Space, nameof(room.Space));
            AssertHelper.NotNull(model, nameof(model));
            
            if (model.Capacity < 5)
            {
                RoomHelper.ThrowMinimalCapacityError(5);
            }

            var data = new ChangeValueData<uint>(room.Capacity, model.Capacity);

            room.Capacity = model.Capacity;
            _dbContext.Update(room);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new[] {room.PublisherId, room.Space.PublisherId};
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "ROOM_CHANGE_CAPACITY", data);
        }

        public async Task<bool> ContainsAsync(Space space, string name)
        {
            string normalizedName = StringHelper.Normalize(name);
            return await _dbContext.Set<Room>()
                .Where(r => r.NormalizedName == normalizedName && space.Id == r.SpaceId)
                .AnyAsync();
        }
        
        public async Task<Room> GetByNameAsync(Space space, string name)
        {
            string normalizedName = StringHelper.Normalize(name);
            var room = await _dbContext.Set<Room>()
                .Where(r => r.NormalizedName == normalizedName && space.Id == r.SpaceId)
                .FirstOrDefaultAsync();

            if (room == null)
            {
                throw new ElementNotFoundException("RoomNotFoundByName");
            }

            return room;
        }

        public async Task<Event> DeleteAsync(Room room, User user)
        {
            AssertHelper.NotNull(room, nameof(room));
            AssertHelper.NotNull(user, nameof(user));
            room.Capacity = 0;
            room.Name = "";
            room.NormalizedName = "";
            room.DeletedAt = DateTime.UtcNow;

            _dbContext.Update(room);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new[] {room.PublisherId, room.Space.PublisherId};
            
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "ROOM_DELETE", room);
        }
    }
}