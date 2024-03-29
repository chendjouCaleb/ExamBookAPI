﻿using System.Collections.Generic;

namespace ExamBook.Entities
{
    public class TestSpeciality:Entity
    {
        public ExaminationSpeciality? ExaminationSpeciality { get; set; }
        public ulong? ExaminationSpecialityId { get; set; }

        public Speciality? Speciality { get; set; }
        public ulong? SpecialityId { get; set; }

        public CourseSpeciality? CourseSpeciality { get; set; }
        public ulong? CourseSpecialityId { get; set; }
        

        public Test Test { get; set; } = null!;
        public ulong TestId { get; set; }
    }
}