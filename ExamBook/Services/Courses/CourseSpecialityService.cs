using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
using ExamBook.Identity.Entities;
using ExamBook.Models;
using ExamBook.Persistence;
using ExamBook.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Traceability.Models;
using Traceability.Services;

namespace ExamBook.Services
{
	public class CourseSpecialityService
	{
        private readonly ApplicationDbContext _dbContext;
        private readonly EventService _eventService;
        private readonly SubjectService _subjectService;
        private readonly PublisherService _publisherService;
        private readonly ILogger<CourseTeacherService> _logger;


        public CourseSpecialityService(ApplicationDbContext dbContext, 
            EventService eventService, SubjectService subjectService, PublisherService publisherService, ILogger<CourseTeacherService> logger)
        {
            _dbContext = dbContext;
            _eventService = eventService;
            _subjectService = subjectService;
            _publisherService = publisherService;
            _logger = logger;
        }

        public async Task<CourseSpeciality> GetCourseSpecialityAsync(ulong courseSpecialityId)
		{
			var courseSpeciality = await _dbContext.Set<CourseSpeciality>()
				.Include(cs => cs.CourseClassroom.Course.Space)
				.Include(cs => cs.Speciality)
				.Where(cs => cs.Id == courseSpecialityId)
				.FirstOrDefaultAsync();

			if (courseSpeciality == null)
			{
				throw new ElementNotFoundException("CourseSpecialityNotFoundById", courseSpecialityId);
			}


			return courseSpeciality;
		}
		
		
		 public async Task<bool> CourseSpecialityExists(CourseClassroom courseClassroom, Speciality speciality)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(speciality, nameof(speciality));

            return await _dbContext.Set<CourseSpeciality>()
                .Where(cs => cs.CourseClassroomId == courseClassroom.Id)
                .Where(cs => cs.SpecialityId == speciality.Id)
                .Where(cs => cs.DeletedAt == null)
                .AnyAsync();
        }

        public async Task<ActionResultModel<CourseSpeciality>> AddCourseSpecialityAsync(CourseClassroom courseClassroom, 
            Speciality speciality, User user)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(user, nameof(user));

            CourseSpeciality courseSpeciality = await _CreateCourseSpecialityAsync(courseClassroom, speciality);
            var publisher = courseSpeciality.Publisher!;
            await _dbContext.AddAsync(courseSpeciality);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAsync(publisher);

            var publisherIds = new List<string>
            {
                courseClassroom.Course.Space!.PublisherId, 
                courseClassroom.Course!.PublisherId, 
                courseClassroom.PublisherId, 
                speciality.PublisherId
            };
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SPECIALITY_ADD", courseSpeciality);

            return new ActionResultModel<CourseSpeciality>(courseSpeciality, @event);
        }
        
        
        public async Task<ActionResultModel<ICollection<CourseSpeciality>>> AddCourseSpecialitiesAsync(
            CourseClassroom courseClassroom, 
            ICollection<Speciality> specialities, User user)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));
            AssertHelper.NotNull(specialities, nameof(specialities));
            AssertHelper.NotNull(user, nameof(user));

            var courseSpecialities = await _CreateCourseSpecialitiesAsync(courseClassroom, specialities);
            var publishers = courseSpecialities.Select(c => c.Publisher!).ToList();
            await _dbContext.AddRangeAsync(courseSpecialities);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAllAsync(publishers);
            
            

            var publisherIds = new List<string>
            {
                courseClassroom.Course.Space!.PublisherId, 
                courseClassroom.Course!.PublisherId, 
                courseClassroom.PublisherId
            };
            publisherIds.AddRange(specialities.Select(s => s.PublisherId));
            publisherIds.AddRange(publishers.Select(p => p.Id).ToList());
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SPECIALITIES_ADD", courseSpecialities);

            return new ActionResultModel<ICollection<CourseSpeciality>>(courseSpecialities, @event);
        }

        public async Task<List<CourseSpeciality>> _CreateCourseSpecialitiesAsync(CourseClassroom courseClassroom, 
            ICollection<Speciality> specialities)
        {
            var courseSpecialities = new List<CourseSpeciality>();
            foreach (var speciality in specialities)
            {
                if (!await CourseSpecialityExists(courseClassroom, speciality))
                {
                    var courseSpeciality = await _CreateCourseSpecialityAsync(courseClassroom, speciality);
                    courseSpecialities.Add(courseSpeciality);
                }
            }

            return courseSpecialities;
        }

        public async Task<CourseSpeciality> _CreateCourseSpecialityAsync(CourseClassroom courseClassroom, Speciality speciality)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));

            if (courseClassroom.Course.SpaceId != speciality.SpaceId)
            {
                throw new IncompatibleEntityException(courseClassroom, speciality);
            }

            if (await CourseSpecialityExists(courseClassroom, speciality))
            {
                throw new IllegalOperationException("CourseSpecialityAlreadyExists");
            }

            var publisher = _publisherService.Create("COURSE_SPECIALITY_PUBLISHER");
            var subject = _subjectService.Create("COURSE_SPECIALITY_SUBJECT");

            CourseSpeciality courseSpeciality = new()
            {
                PublisherId = publisher.Id,
                Publisher = publisher,
                Subject = subject,
                SubjectId = subject.Id,
                CourseClassroom = courseClassroom,
                Speciality = speciality
            };
            return courseSpeciality;
        }

        public async Task<Event> DeleteCourseSpecialityAsync(CourseSpeciality courseSpeciality, User user)
        {
            AssertHelper.NotNull(courseSpeciality, nameof(courseSpeciality));
            AssertHelper.NotNull(user, nameof(user));
            

            courseSpeciality.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(courseSpeciality);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {courseSpeciality.PublisherId};
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SPECIALITY_DELETE", courseSpeciality);
        }
	}
}