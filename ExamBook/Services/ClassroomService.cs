using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
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
    public class ClassroomService
    {
        private readonly DbContext _dbContext;
        private readonly ILogger<ClassroomService> _logger;
        private readonly EventService _eventService;
        private readonly PublisherService _publisherService;

        public ClassroomService(DbContext dbContext, 
            ILogger<ClassroomService> logger,
            PublisherService publisherService, 
            EventService eventService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _publisherService = publisherService;
            _eventService = eventService;
        }

        
        public async Task<Classroom> GetAsync(ulong id)
        {
            var classroom = await _dbContext.Set<Classroom>().Where(r => r.Id == id)
                .Include(r => r.Space)
                .FirstOrDefaultAsync();

            if (classroom == null)
            {
                throw new ElementNotFoundException("ClassroomNotFound");
            }

            return classroom;
        }

        public async Task<ActionResultModel<Classroom>> AddAsync(Space space, ClassroomAddModel model, User user)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNull(model, nameof(model));

            if (await ContainsAsync(space, model.Name))
            {
                throw new UsedValueException("ClassroomNameUsed");
            }

            var room = await _dbContext.Set<Room>().FindAsync(model.RoomId);

            if (room != null && room.SpaceId != space.Id)
            {
                throw new IllegalValueException("RoomIsNotFromSpace");
            }
            
            
            var publisher = await _publisherService.AddAsync();
            Classroom classroom = new()
            {
                Space = space,
                Room = room,
                Name = model.Name,
                NormalizedName = StringHelper.Normalize(model.Name),
                PublisherId = publisher.Id
            };

            await _dbContext.AddAsync(classroom);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> { publisher.Id, space.PublisherId };

            if (room != null)
            {
                publisherIds.Add(room.PublisherId);
            }
            
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "CLASSROOM_ADD", classroom);

            _logger.LogInformation("New classroom {} in space: {}", classroom.Name, space.Name);
            return new ActionResultModel<Classroom>(classroom, @event);
        }
        
        
        
        public async Task<Event> ChangeNameAsync(Classroom classroom, string name, User user)
        {
            Asserts.NotNull(classroom.Space, nameof(classroom.Space));
            
            if (await ContainsAsync(classroom.Space!, name))
            {
                throw new UsedValueException("ClassroomNameUsed");
            }

            var data = new ChangeValueData<string>(classroom.Name, name);
            classroom.Name = name;
            classroom.NormalizedName = StringHelper.Normalize(name);
            _dbContext.Update(classroom);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new[] {classroom.PublisherId, classroom.Space!.PublisherId};
            
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "CLASSROOM_CHANGE_NAME", data);
        }
        
        
        
        public async Task<Event> ChangeRoomAsync(Classroom classroom, Room room, User user)
        {
            Asserts.NotNull(classroom.Space, nameof(classroom.Space));
            Asserts.NotNull(room.Space, nameof(room.Space));

            if (classroom.SpaceId != room.SpaceId)
            {
                throw new IllegalValueException("RoomIsNotFromSpace");
            }

            if (classroom.RoomId == room.Id)
            {
                throw new IllegalOperationException("ClassroomAlreadyUseRoom");
            }

            var currentRoom = await _dbContext.Set<Room>()
                .Where(r => r.Id == classroom.RoomId)
                .FirstOrDefaultAsync();

            var data = new ChangeValueData<ulong?>(currentRoom?.Id ?? 0, room.Id);
            classroom.Room = room;
            _dbContext.Update(classroom);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> {classroom.PublisherId, classroom.Space!.PublisherId, room.PublisherId};
            if (currentRoom != null)
            {
                publisherIds.Add(currentRoom.PublisherId);
            } 
            
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "CLASSROOM_CHANGE_ROOM", data);
        }


       


        

        

       

        public async Task<bool> ContainsAsync(Space space, string name)
        {
            Asserts.NotNull(space, nameof(space));
            Asserts.NotNullOrWhiteSpace(name, nameof(name));
            var normalizedName = StringHelper.Normalize(name);
            return await _dbContext.Set<Classroom>()
                .AnyAsync(c => space.Equals(c.Space) && c.NormalizedName == normalizedName);
        }


        public async Task<Classroom> GetByNameAsync(Space space, string name)
        {
            string normalizedName = StringHelper.Normalize(name);
            var classroom = await _dbContext.Set<Classroom>()
                .Where(r => r.NormalizedName == normalizedName && space.Id == r.SpaceId)
                .FirstOrDefaultAsync();

            if (classroom == null)
            {
                throw new ElementNotFoundException("ClassroomNotFoundByName");
            }

            return classroom;
        }
        
        
        public async Task<Event> DeleteAsync(Classroom classroom, User user)
        {
            Asserts.NotNull(classroom, nameof(classroom));
            Asserts.NotNull(classroom, nameof(classroom.Space));
            Asserts.NotNull(user, nameof(user));
            // var classroomSpecialities = await _dbContext.Set<ClassroomSpeciality>()
            //     .Where(cs => classroom.Equals(cs.Classroom))
            //     .ToListAsync();

            var room = await _dbContext.Set<Room>()
                .Where(r => r.Id == classroom.RoomId)
                .FirstOrDefaultAsync();

            classroom.Name = "";
            classroom.NormalizedName = "";
            classroom.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(classroom);
            await _dbContext.SaveChangesAsync();
            
            var publisherIds = new List<string> { classroom.PublisherId, classroom.Space!.PublisherId };

            if (room != null)
            {
                publisherIds.Add(room.PublisherId);
            }
            
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "CLASSROOM_DELETE", classroom);
        }
    }
}