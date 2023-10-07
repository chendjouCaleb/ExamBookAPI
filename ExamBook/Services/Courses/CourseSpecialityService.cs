using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExamBook.Entities;
using ExamBook.Exceptions;
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
        private readonly ILogger<CourseSpecialityService> _logger;


        public CourseSpecialityService(ApplicationDbContext dbContext, 
            EventService eventService, SubjectService subjectService, PublisherService publisherService,
            ILogger<CourseSpecialityService> logger)
        {
            _dbContext = dbContext;
            _eventService = eventService;
            _subjectService = subjectService;
            _publisherService = publisherService;
            _logger = logger;
        }


        public async Task<CourseSpeciality> GetByIdAsync(ulong id)
        {
            var courseSpeciality = await _dbContext.CourseSpecialities
                .Where(cs => cs.Id == id)
                .Include(cs => cs.Speciality.Space)
                .Include(cs => cs.CourseClassroom.Course)
                .FirstOrDefaultAsync();

            if (courseSpeciality == null)
            {
                throw new ElementNotFoundException("CourseSpecialityNotFoundById", id);
            }

            return courseSpeciality;
        }

        public async Task<List<CourseSpeciality>> GetAllAsync(CourseClassroom courseClassroom,
            ICollection<Speciality> specialities)
        {
            var specialityIds = specialities.Select(s => s.Id).ToHashSet();
            return await _dbContext.CourseSpecialities
                .Where(cc => cc.CourseClassroomId == courseClassroom.Id && specialityIds.Contains(cc.SpecialityId))
                .ToListAsync();
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
		
		
		 public async Task<bool> Exists(CourseClassroom courseClassroom, Speciality speciality)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(speciality, nameof(speciality));

            return await _dbContext.Set<CourseSpeciality>()
                .Where(cs => cs.CourseClassroomId == courseClassroom.Id)
                .Where(cs => cs.SpecialityId == speciality.Id)
                .Where(cs => cs.DeletedAt == null)
                .AnyAsync();
        }

        public async Task<ActionResultModel<CourseSpeciality>> AddAsync(CourseClassroom courseClassroom, 
            Speciality speciality, Member adminMember)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(adminMember, nameof(adminMember));
            
            if (await Exists(courseClassroom, speciality))
            {
                throw new IllegalOperationException("CourseSpecialityAlreadyExists", courseClassroom, speciality);
            }


            CourseSpeciality courseSpeciality = _CreateCourseSpeciality(courseClassroom, speciality);
            var publisher = courseSpeciality.Publisher!;
            await _dbContext.AddAsync(courseSpeciality);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAsync(publisher);
            await _subjectService.SaveAsync(courseSpeciality.Subject);

            var publishers = await _eventService.GetPublishers(courseSpeciality.GetPublisherIds());
            var actors = await _eventService.GetActors(adminMember.GetActorIds());
            var subjects = await _eventService.GetSubjects(new[] {courseSpeciality.SubjectId});
            var data = new {CourseSpecialityId = courseSpeciality.Id};
            var @event = await _eventService.EmitAsync(publishers, subjects, actors, "COURSE_SPECIALITY_ADD", data);

            return new ActionResultModel<CourseSpeciality>(courseSpeciality, @event);
        }
        
        
        public async Task<ActionResultModel<ICollection<CourseSpeciality>>> AddCourseSpecialitiesAsync(
            CourseClassroom courseClassroom,
            ICollection<Speciality> specialities, Member adminMember)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));
            AssertHelper.NotNull(specialities, nameof(specialities));
            AssertHelper.NotNull(adminMember, nameof(adminMember));

            var duplicate = await GetAllAsync(courseClassroom, specialities);
            if (specialities.Any())
            {
                throw new DuplicateValueException("CourseSpecialitiesExists", courseClassroom, duplicate);
            }
            

            var courseSpecialities = _CreateCourseSpecialities(courseClassroom, specialities);
            var publishers = courseSpecialities.Select(c => c.Publisher!).ToList();
            var subjects = courseSpecialities.Select(c => c.Subject).ToList();
            await _dbContext.AddRangeAsync(courseSpecialities);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAllAsync(publishers);
            await _subjectService.SaveAllAsync(subjects);


            var otherPublisherIds = specialities.Select(s => s.PublisherId)
                .Concat(courseClassroom.GetPublisherIds())
                .ToHashSet();
            var otherPublishers = await _eventService.GetPublishers(otherPublisherIds);
            
            publishers = publishers.Concat(otherPublishers).ToList();
            var actors = await _eventService.GetActors(adminMember.GetActorIds());
            var data = new { CourseSpecialityIds = courseSpecialities.Select(cs => cs.Id)};
            var @event = await _eventService.EmitAsync(publishers, subjects, actors, "COURSE_SPECIALITIES_ADD", data);

            _logger.LogInformation("New courseSpecialities: {}", courseSpecialities);
            return new ActionResultModel<ICollection<CourseSpeciality>>(courseSpecialities, @event);
        }

        public List<CourseSpeciality> _CreateCourseSpecialities(CourseClassroom courseClassroom, ICollection<Speciality> specialities)
        {
            var courseSpecialities = new List<CourseSpeciality>();
            foreach (var speciality in specialities)
            {
                var courseSpeciality =  _CreateCourseSpeciality(courseClassroom, speciality);
                courseSpecialities.Add(courseSpeciality);
            }

            return courseSpecialities;
        }

        public CourseSpeciality _CreateCourseSpeciality(CourseClassroom courseClassroom, Speciality speciality)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));

            AssertHelper.IsTrue(courseClassroom.Course.SpaceId == speciality.SpaceId, "Bad entities");
            
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

        public async Task<Event> DeleteAsync(CourseSpeciality courseSpeciality, Member admin)
        {
            AssertHelper.NotNull(courseSpeciality, nameof(courseSpeciality));
            AssertHelper.NotNull(admin, nameof(admin));
            

            courseSpeciality.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(courseSpeciality);
            await _dbContext.SaveChangesAsync();

            var publishers = await _eventService.GetPublishers(courseSpeciality.GetPublisherIds());
            var subjects = await _eventService.GetSubjects(new[] {courseSpeciality.SubjectId});
            var actors = await _eventService.GetActors(admin.GetActorIds());
            return await _eventService.EmitAsync(publishers, subjects, actors, "COURSE_SPECIALITY_DELETE", courseSpeciality);
        }

	}
}