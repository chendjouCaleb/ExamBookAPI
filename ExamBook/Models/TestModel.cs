﻿using System;
using System.Collections.Generic;
using ExamBook.Entities;

namespace ExamBook.Models
{
    public class TestAddModel
    {
        public TestAddModel() {}

        public TestAddModel(CourseClassroom courseClassroom)
        {
            Name = courseClassroom.Course.Name;
            Coefficient = courseClassroom.Coefficient;
        }
        public string Name { get; set; } = "";
        public DateTime StartAt { get; set; }
        public uint Duration { get; set; } = 60;

        public DateTime EndAt => StartAt.AddMinutes(Duration);
        

        public uint Coefficient { get; set; } = 1;

        public uint Radical { get; set; } = 20;
    }
}