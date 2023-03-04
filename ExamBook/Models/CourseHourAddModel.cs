using System;
using System.ComponentModel.DataAnnotations;

namespace ExamBook.Models
{
    public class CourseHourAddModel
    {
        [Required]
        public DayOfWeek DayOfWeek { get; set; }
        
        [Required]
        public TimeSpan StartHour { get; set; }
        
        [Required]
        public TimeSpan EndHour { get; set; }

        public ulong RoomId { get; set; }
        public ulong CourseTeacherId { get; set; }
        public ulong CourseId { get; set; }
    }

    public class CourseHourChangeHourModel
    {
        [Required]
        public DayOfWeek DayOfWeek { get; set; }
        
        [Required]
        public TimeSpan StartHour { get; set; }
        
        [Required]
        public TimeSpan EndHour { get; set; }
    }
}