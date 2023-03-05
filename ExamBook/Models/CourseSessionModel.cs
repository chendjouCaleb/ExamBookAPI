using System;

namespace ExamBook.Models
{
    public class CourseSessionAddModel
    {
        public DateTime ExpectedStartDateTime { get; set; }
        public DateTime ExpectedEndDateTime { get; set; }

        public string Description { get; set; } = "";

        public ulong CourseId { get; set; }
        public ulong CourseTeacherId { get; set; }
        public ulong? CourseHourId { get; set; }
    }
    
    public class CourseSessionDateTimeModel
    {
        public DateTime? ExpectedStartDateTime { get; set; }
        public DateTime? ExpectedEndDateTime { get; set; }
    }
}