using System;
using System.Collections.Generic;

namespace ExamBook.Entities
{
    public class Test:Entity
    {

        public string Code { get; set; } = "";
        public string NormalizedCode { get; set; } = "";
        
        public DateTime StartAt { get; set; }

        /// <summary>
        /// Duration in minutes.
        /// </summary>
        public uint Duration { get; set; } = 60;
        public uint Coefficient { get; set; }
        public uint Radical { get; set; }
        
        public Space Space { get; set; } = null!;
        public ulong SpaceId { get; set; }
        
        public Examination? Examination { get; set; }
        public ulong? ExaminationId { get; set; }
        
        public Course? Course { get; set; } 
        public ulong? CourseId { get; set; }

        public Room? Room { get; set; }
        public ulong? RoomId { get; set; }
        
        public bool IsLock { get; set; }

        public bool IsPublished { get; set; }
        public bool IsSpecialized { get; set; }
        
        public List<TestSpeciality> TestSpecialities { get; set; }
        public List<TestTeacher> TestTeachers { get; set; } = new();

        public void AssertRelationNotNull()
        {
            if (ExaminationId != null && Examination == null)
            {
                throw new ArgumentNullException(nameof(Examination));
            }

            if (CourseId != null && Course == null)
            {
                throw new ArgumentNullException(nameof(Course));
            }
        }
        public List<string> GetPublisherIds()
        {
            var publisherIds = new List<string> { PublisherId, Space.PublisherId };
            if (Examination != null)
            {
                publisherIds.Add(Examination.PublisherId);
            }

            if (Course != null)
            {
                publisherIds.Add(Course.PublisherId);
            }

            return publisherIds;
        }
    }
}