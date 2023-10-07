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
                .Include(cs => cs.ClassroomSpeciality.Speciality)
                .Include(cs => cs.CourseClassroom.Course.Space)
                .FirstOrDefaultAsync();

            if (courseSpeciality == null)
            {
                throw new ElementNotFoundException("CourseSpecialityNotFoundById", id);
            }

            return courseSpeciality;
        }

        public async Task<List<CourseSpeciality>> GetAllAsync(CourseClassroom courseClassroom,
            ICollection<ClassroomSpeciality> classroomSpecialities)
        {
            var classroomSpecialityIds = classroomSpecialities.Select(s => s.Id).ToHashSet();
            return await _dbContext.CourseSpecialities
                .Where(cc => cc.CourseClassroomId == courseClassroom.Id 
                             && classroomSpecialityIds.Contains(cc.ClassroomSpecialityId))
                .ToListAsync();
        }
        
		
		
		 public async Task<bool> Exists(CourseClassroom courseClassroom, ClassroomSpeciality classroomSpeciality)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(classroomSpeciality, nameof(classroomSpeciality));

            return await _dbContext.Set<CourseSpeciality>()
                .Where(cs => cs.CourseClassroomId == courseClassroom.Id)
                .Where(cs => cs.ClassroomSpecialityId == classroomSpeciality.Id)
                .Where(cs => cs.DeletedAt == null)
                .AnyAsync();
        }


         public async Task<ActionResultModel<List<CourseSpeciality>>> AddAsync(
            CourseClassroom courseClassroom, List<ClassroomSpeciality> classroomSpecialities, Member adminMember)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));
            AssertHelper.NotNull(classroomSpecialities, nameof(classroomSpecialities));
            AssertHelper.NotNull(adminMember, nameof(adminMember));

            var duplicate = await GetAllAsync(courseClassroom, classroomSpecialities);
            if (classroomSpecialities.Any())
            {
                throw new DuplicateValueException("CourseSpecialitiesExists", courseClassroom, duplicate);
            }

            var courseSpecialities = _CreateCourseSpecialities(courseClassroom, classroomSpecialities);
            var publishers = courseSpecialities.Select(c => c.Publisher!).ToList();
            var subjects = courseSpecialities.Select(c => c.Subject).ToList();
            await _dbContext.AddRangeAsync(courseSpecialities);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAllAsync(publishers);
            await _subjectService.SaveAllAsync(subjects);


            var otherPublisherIds = classroomSpecialities.Select(s => s.PublisherId)
                .Concat(courseClassroom.GetPublisherIds())
                .ToHashSet();
            var otherPublishers = await _eventService.GetPublishers(otherPublisherIds);
            
            publishers = publishers.Concat(otherPublishers).ToList();
            var actors = await _eventService.GetActors(adminMember.GetActorIds());
            var data = new { CourseSpecialityIds = courseSpecialities.Select(cs => cs.Id)};
            var @event = await _eventService.EmitAsync(publishers, subjects, actors, "COURSE_SPECIALITIES_ADD", data);

            _logger.LogInformation("New courseSpecialities: {}", courseSpecialities);
            return new ActionResultModel<List<CourseSpeciality>>(courseSpecialities, @event);
        }

        public List<CourseSpeciality> _CreateCourseSpecialities(CourseClassroom courseClassroom,
            ICollection<ClassroomSpeciality> classroomSpecialities)
        {
            var courseSpecialities = new List<CourseSpeciality>();
            foreach (var classroomSpeciality in classroomSpecialities)
            {
                var courseSpeciality =  _CreateCourseSpeciality(courseClassroom, classroomSpeciality);
                courseSpecialities.Add(courseSpeciality);
            }

            return courseSpecialities;
        }

        public CourseSpeciality _CreateCourseSpeciality(CourseClassroom courseClassroom, ClassroomSpeciality classroomSpeciality)
        {
            AssertHelper.NotNull(courseClassroom, nameof(courseClassroom));
            AssertHelper.NotNull(classroomSpeciality, nameof(classroomSpeciality));
            AssertHelper.NotNull(courseClassroom.Course.Space, nameof(courseClassroom.Course.Space));

            AssertHelper.IsTrue(courseClassroom.ClassroomId == classroomSpeciality.ClassroomId, "Bad entities");
            
            var publisher = _publisherService.Create("COURSE_SPECIALITY_PUBLISHER");
            var subject = _subjectService.Create("COURSE_SPECIALITY_SUBJECT");

            CourseSpeciality courseSpeciality = new()
            {
                PublisherId = publisher.Id,
                Publisher = publisher,
                Subject = subject,
                SubjectId = subject.Id,
                CourseClassroom = courseClassroom,
                ClassroomSpeciality = classroomSpeciality
            };
            return courseSpeciality;
        }

        public async Task<Event> DeleteAsync(CourseSpeciality courseSpeciality, Member admin)
        {
            AssertHelper.NotNull(courseSpeciality, nameof(courseSpeciality));
            AssertHelper.NotNull(admin, nameof(admin));
            

            courseSpeciality.DeletedAt = DateTimeOffset.UtcNow;
            _dbContext.Update(courseSpeciality);
            await _dbContext.SaveChangesAsync();

            var publishers = await _eventService.GetPublishers(courseSpeciality.GetPublisherIds());
            var subjects = await _eventService.GetSubjects(new[] {courseSpeciality.SubjectId});
            var actors = await _eventService.GetActors(admin.GetActorIds());
            return await _eventService.EmitAsync(publishers, subjects, actors, "COURSE_SPECIALITY_DELETE", courseSpeciality);
        }

	}
}