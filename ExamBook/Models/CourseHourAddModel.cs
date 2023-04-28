using System;
using System.ComponentModel.DataAnnotations;
using ExamBook.Entities;

namespace ExamBook.Models
{
    public class CourseHourAddModel
    {
        [Required]
        public DayOfWeek DayOfWeek { get; set; }
        
        [Required]
        public TimeOnly StartHour { get; set; }
        
        [Required]
        public TimeOnly EndHour { get; set; }

        public ulong RoomId { get; set; }
        public ulong CourseTeacherId { get; set; }
    }

    public class CourseHourHourModel
    {
        public CourseHourHourModel() {}

        public CourseHourHourModel(CourseHour courseHour)
        {
            DayOfWeek = courseHour.DayOfWeek;
            StartHour = courseHour.StartHour;
            EndHour = courseHour.EndHour;
        }
        
        [Required]
        public DayOfWeek DayOfWeek { get; set; }
        
        [Required]
        public TimeOnly StartHour { get; set; }
        
        [Required]
        public TimeOnly EndHour { get; set; }
    }
}