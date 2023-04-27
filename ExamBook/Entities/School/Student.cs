using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Student:Entity
    {
        public string RId { get; set; } = "";
        public string NormalizedRId { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public char Sex { get; set; } = 'M';
        public DateTime BirthDate { get; set; }


        public string UserId { get; set; } = "";
        
        [JsonIgnore]
        public Space Space { get; set; } = null!;
        public ulong? SpaceId { get; set; }

        [JsonIgnore] public HashSet<Participant> Participants { get; set; } = new();
        [JsonIgnore] public List<StudentSpeciality> Specialities { get; set; } = new();
    }


    public class StudentSpeciality : Entity
    {
        public StudentSpeciality() {}
        public StudentSpeciality(Student? student, Speciality? Speciality)
        {
            Student = student;
            Speciality = Speciality;
        }

        public Student? Student { get; set; }
        public ulong? StudentId { get; set; }

        public Speciality? Speciality { get; set; }
        public ulong SpecialityId { get; set; }
    }
}