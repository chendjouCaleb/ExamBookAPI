namespace ExamBook.Services
{
	public class CourseSpecialityService
	{
		public async Task<CourseSpeciality> GetCourseSpecialityAsync(ulong courseSpecialityId)
		{
			var courseSpeciality = await _dbContext.Set<CourseSpeciality>()
				.Include(cs => cs.Course)
				.Include(cs => cs.Speciality)
				.Where(cs => cs.Id == courseSpecialityId)
				.FirstOrDefaultAsync();

			if (courseSpeciality == null)
			{
				throw new ElementNotFoundException("CourseSpecialityNotFoundById", courseSpecialityId);
			}


			return courseSpeciality;
		}
		
		
		 public async Task<bool> CourseSpecialityExists(Course course, Speciality speciality)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(speciality, nameof(speciality));

            return await _dbContext.Set<CourseSpeciality>()
                .Where(cs => cs.CourseId == course.Id)
                .Where(cs => cs.SpecialityId == speciality.Id)
                .Where(cs => cs.DeletedAt == null)
                .AnyAsync();
        }

        public async Task<ActionResultModel<CourseSpeciality>> AddCourseSpecialityAsync(Course course, 
            Speciality speciality, User user)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(user, nameof(user));

            CourseSpeciality courseSpeciality = await _CreateCourseSpecialityAsync(course, speciality);
            var publisher = courseSpeciality.Publisher!;
            await _dbContext.AddAsync(courseSpeciality);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAsync(publisher);

            var publisherIds = new List<string>
            {
                course.Space!.PublisherId, 
                course.PublisherId, 
                speciality.PublisherId
            };
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SPECIALITY_ADD", courseSpeciality);

            return new ActionResultModel<CourseSpeciality>(courseSpeciality, @event);
        }
        
        
        public async Task<ActionResultModel<ICollection<CourseSpeciality>>> AddCourseSpecialitiesAsync(Course course, 
            ICollection<Speciality> specialities, User user)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(course.Space, nameof(course.Space));
            AssertHelper.NotNull(specialities, nameof(specialities));
            AssertHelper.NotNull(user, nameof(user));

            var courseSpecialities = await _CreateCourseSpecialitiesAsync(course, specialities);
            var publishers = courseSpecialities.Select(c => c.Publisher!).ToList();
            await _dbContext.AddRangeAsync(courseSpecialities);
            await _dbContext.SaveChangesAsync();
            await _publisherService.SaveAllAsync(publishers);
            
            

            var publisherIds = new List<string> {course.Space!.PublisherId, course.PublisherId};
            publisherIds.AddRange(specialities.Select(s => s.PublisherId));
            publisherIds.AddRange(publishers.Select(p => p.Id).ToList());
            var @event = await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SPECIALITIES_ADD", courseSpecialities);

            return new ActionResultModel<ICollection<CourseSpeciality>>(courseSpecialities, @event);
        }

        public async Task<List<CourseSpeciality>> _CreateCourseSpecialitiesAsync(Course course, 
            ICollection<Speciality> specialities)
        {
            var courseSpecialities = new List<CourseSpeciality>();
            foreach (var speciality in specialities)
            {
                if (!await CourseSpecialityExists(course, speciality))
                {
                    var courseSpeciality = await _CreateCourseSpecialityAsync(course, speciality);
                    courseSpecialities.Add(courseSpeciality);
                }
            }

            return courseSpecialities;
        }

        public async Task<CourseSpeciality> _CreateCourseSpecialityAsync(Course course, Speciality speciality)
        {
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(course.Space, nameof(course.Space));

            if (course.SpaceId != speciality.SpaceId)
            {
                throw new IncompatibleEntityException(course, speciality);
            }

            if (await CourseSpecialityExists(course, speciality))
            {
                throw new IllegalOperationException("CourseSpecialityAlreadyExists");
            }

            var publisher = await _publisherService.CreateAsync();

            CourseSpeciality courseSpeciality = new()
            {
                PublisherId = publisher.Id,
                Publisher = publisher,
                Course = course,
                Speciality = speciality
            };
            return courseSpeciality;
        }

        public async Task<Event> DeleteCourseSpecialityAsync(CourseSpeciality courseSpeciality, User user)
        {
            AssertHelper.NotNull(courseSpeciality, nameof(courseSpeciality));
            AssertHelper.NotNull(user, nameof(user));
            var course = await _dbContext.Set<Course>().FindAsync(courseSpeciality.CourseId);
            var speciality = await _dbContext.Set<Speciality>().FindAsync(courseSpeciality.SpecialityId);
            var space = await _dbContext.Set<Space>().FindAsync(course!.SpaceId);
            
            AssertHelper.NotNull(course, nameof(course));
            AssertHelper.NotNull(speciality, nameof(speciality));
            AssertHelper.NotNull(space, nameof(space));

            courseSpeciality.DeletedAt = DateTime.UtcNow;
            _dbContext.Update(courseSpeciality);
            await _dbContext.SaveChangesAsync();

            var publisherIds = new List<string> {space!.PublisherId, speciality!.PublisherId, course.PublisherId};
            return await _eventService.EmitAsync(publisherIds, user.ActorId, "COURSE_SPECIALITY_DELETE", courseSpeciality);
        }
	}
}