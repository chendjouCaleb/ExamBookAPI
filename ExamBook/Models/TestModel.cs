using System;
using System.Collections.Generic;
using ExamBook.Entities;

namespace ExamBook.Models
{
    public class TestAddModel
    {
        public TestAddModel() {}

        public TestAddModel(Course course)
        {
            Name = course.Name;
            Coefficient = course.Coefficient;
        }
        public string Name { get; set; } = "";
        public DateTime StartAt { get; set; }

        public DateTime EndAt => StartAt.AddMinutes(Duration);
        
        public uint Duration { get; set; } = 60;

        public uint Coefficient { get; set; } = 1;

        public uint Radical { get; set; } = 20;

        public ulong RoomId { get; set; }

        public HashSet<ulong> ExaminationSpecialityIds { get; set; } = new();
        
    }
}