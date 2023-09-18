using System.Collections.Generic;

namespace ExamBook.Entities
{
    public class CourseSpeciality:Entity
    {


        public CourseClassroom CourseClassroom { get; set; } = null!;
        public ulong? CourseClassroomId { get; set; }


        public Speciality Speciality { get; set; } = null!;
        public ulong SpecialityId { get; set; }
        
        
        public HashSet<string> GetPublisherIds()
        {
            return new HashSet<string>
            {
                CourseClassroom.Course.Space.PublisherId, 
                CourseClassroom.Course.PublisherId, 
                CourseClassroom.PublisherId, 
                Speciality.PublisherId,
                PublisherId
            };
        }
    }
}