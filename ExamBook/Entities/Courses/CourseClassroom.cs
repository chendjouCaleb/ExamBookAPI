using System.Collections.Generic;

namespace ExamBook.Entities
{
	public class CourseClassroom:Entity
	{
		public Classroom Classroom { get; set; } = null!;
		public ulong ClassroomId { get; set; }

		public Course Course { get; set; } = null!;
		public ulong CourseId { get; set; }
		
		
		
		public string Code { get; set; } = "";
		public string NormalizedCode { get; set; } = "";

		public uint Coefficient { get; set; }
		public string Description { get; set; } = "";

		/// <summary>
		/// Tells if the course is restricted to some specialities.
		/// If the course has speciality this value should be false.
		/// If the course hasn't speciality, this value should be true.
		/// </summary>
		public bool IsGeneral { get; set; }
		
		public List<CourseSpeciality> CourseSpecialities { get; set; } = new();
		public List<CourseSession> CourseSessions { get; set; } = new();
		public List<CourseHour> CourseHours { get; set; } = new();
		public List<CourseTeacher> CourseTeachers { get; set; } = new();
		
		
		public HashSet<string> GetPublisherIds()
		{
			return new HashSet<string> {
				PublisherId, 
				Course.PublisherId,
				Classroom.PublisherId,
				Classroom.Space.PublisherId
			};
		}
	}
}