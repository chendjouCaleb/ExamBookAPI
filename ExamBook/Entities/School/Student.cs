using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Student:Entity
    {
        public string Code { get; set; } = "";
        public string NormalizedCode { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public char Sex { get; set; } = 'M';
        public DateTime BirthDate { get; set; }


        public Space Space { get; set; } = null!;
        public ulong? SpaceId { get; set; }

        public Member? Member { get; set; }
        public ulong? MemberId { get; set; }

        [JsonIgnore] public HashSet<Participant> Participants { get; set; } 
        [JsonIgnore] public List<StudentSpeciality> Specialities { get; set; } 
    }


    public class StudentSpeciality : Entity
    {
        public StudentSpeciality() {}
        public StudentSpeciality(Student student, Speciality speciality)
        {
            Student = student;
            Speciality = speciality;
        }

        public Student? Student { get; set; }
        public ulong? StudentId { get; set; }

        public Speciality? Speciality { get; set; }
        public ulong SpecialityId { get; set; }
    }
}