using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Helpers;
using ExamBook.Identity.Entities;
using ExamBook.Models;
using ExamBook.Models.Data;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Models;
using Traceability.Services;

namespace ExamBook.Services
{
	public class ClassroomService
	{
		private readonly ApplicationDbContext _dbContext;
		private readonly PublisherService _publisherService;
		private readonly SubjectService _subjectService;
		private readonly EventService _eventService;
		private readonly ILogger<ClassroomService> _logger;


		public ClassroomService(ApplicationDbContext dbContext,
			PublisherService publisherService, SubjectService subjectService, 
			EventService eventService, 
			ILogger<ClassroomService> logger)
		{
			_dbContext = dbContext;
			_publisherService = publisherService;
			_subjectService = subjectService;
			_eventService = eventService;
			_logger = logger;
		}


		public async Task<Classroom> GetByIdAsync(ulong id)
		{
			var classroom = await _dbContext.Classrooms
				.Include(c => c.Space)
				.Where(c => c.Id == id)
				.FirstOrDefaultAsync();

			if (classroom == null)
			{
				throw new ElementNotFoundException("ClassroomNotFoundById", id);
			}

			return classroom;
		}


		public async Task<bool> ContainsAsync(Space space, string name)
		{
			var normalizedName = StringHelper.Normalize(name);
			return await _dbContext.Classrooms
				.AnyAsync(cr => cr.SpaceId == space.Id && cr.NormalizedName == normalizedName);
		}
		
		
		public async Task<ActionResultModel<Classroom>> AddAsync(Space space, ClassroomAddModel model, User user)
        {
            AssertHelper.NotNull(space, nameof(space));
            AssertHelper.NotNull(model, nameof(model));

            if (await ContainsAsync(space, model.Name))
            {
                throw new UsedValueException("ClassroomNameUsed", model.Name, space);
            }

            var room = await _dbContext.Set<Room>().FindAsync(model.RoomId);

            if (room != null && room.SpaceId != space.Id)
            {
                throw new IllegalValueException("BadRoomSpace");
            }


            var publisher = _publisherService.Create("CLASSROOM_PUBLISHER");
            var subject = _subjectService.Create("CLASSROOM_SUBJECT");
            Classroom classroom = new()
            {
                Space = space,
                Room = room,
                Name = model.Name,
                NormalizedName = StringHelper.Normalize(model.Name),
                PublisherId = publisher.Id,
                SubjectId = subject.Id
            };

            await _dbContext.AddAsync(classroom);
            await _dbContext.SaveChangesAsync();

            await _publisherService.SaveAsync(publisher);
            await _subjectService.SaveAsync(subject);
            
            var publisherIds = new List<string> { publisher.Id, space.PublisherId };

            if (room != null)
            {
                publisherIds.Add(room.PublisherId);
            }

            var actorIds = new List<string> {user.ActorId};
            var @event = await _eventService.EmitAsync(publisherIds, actorIds, subject.Id, "CLASSROOM_ADD", classroom);

            _logger.LogInformation("New classroom {} in space: {}", classroom.Name, space.Name);
            return new ActionResultModel<Classroom>(classroom, @event);
        }



        public async Task<Event> ChangeNameAsync(Classroom classroom, string name, User user)
        {
            AssertHelper.NotNull(classroom.Space, nameof(classroom.Space));

            if (await ContainsAsync(classroom.Space, name))
            {
                throw new UsedValueException("ClassroomNameUsed", name, classroom.Space);
            }

            var data = new ChangeValueData<string>(classroom.Name, name);
            classroom.Name = name;
            classroom.NormalizedName = StringHelper.Normalize(name);
            _dbContext.Update(classroom);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new[] {classroom.PublisherId, classroom.Space.PublisherId};

            return await _eventService.EmitAsync(publisherIds, new[] {user.ActorId}, 
	            classroom.SubjectId,
	            "CLASSROOM_CHANGE_NAME", data);
        }



        public async Task<Event> ChangeRoomAsync(Classroom classroom, Room room, User user)
        {
            AssertHelper.NotNull(classroom.Space, nameof(classroom.Space));
            AssertHelper.NotNull(room.Space, nameof(room.Space));

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

            var publisherIds = new List<string> {classroom.PublisherId, classroom.Space.PublisherId, room.PublisherId};
            if (currentRoom != null)
            {
                publisherIds.Add(currentRoom.PublisherId);
            } 
			
            return await _eventService
	            .EmitAsync(publisherIds, new[] {user.ActorId}, classroom.SubjectId, "CLASSROOM_CHANGE_ROOM", data);
        }


        public async Task<Classroom> GetByNameAsync(Space space, string name)
        {
            string normalizedName = StringHelper.Normalize(name);
            var classroom = await _dbContext.Set<Classroom>()
                .Where(r => r.NormalizedName == normalizedName && space.Id == r.SpaceId)
                .FirstOrDefaultAsync();

            if (classroom == null)
            {
                throw new ElementNotFoundException("ClassroomNotFoundByName", name, space);
            }

            return classroom;
        }


        public async Task<Event> DeleteAsync(Classroom classroom, User user)
        {
            AssertHelper.NotNull(classroom, nameof(classroom));
            AssertHelper.NotNull(classroom, nameof(classroom.Space));
            AssertHelper.NotNull(user, nameof(user));
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

            var publisherIds = new List<string> { classroom.PublisherId, classroom.Space.PublisherId };

            if (room != null)
            {
                publisherIds.Add(room.PublisherId);
            }

            var actorIds = new List<string> {user.ActorId};
            return await _eventService.EmitAsync(publisherIds, actorIds, classroom.SubjectId, "CLASSROOM_DELETE", classroom);
        }
	}
}