using System;
using ExamBook.Entities;

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
    
    public class CourseSessionDateModel
    {
        public CourseSessionDateModel() {}

        public CourseSessionDateModel(CourseSession courseSession)
        {
            ExpectedEndDateTime = courseSession.ExpectedEndDateTime;
            ExpectedStartDateTime = courseSession.ExpectedStartDateTime;
        }
        public DateTime? ExpectedStartDateTime { get; set; }
        public DateTime? ExpectedEndDateTime { get; set; }
    }
    
    public class CourseSessionReportModel
    {
        public string Report { get; set; } = "";
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
    }
}