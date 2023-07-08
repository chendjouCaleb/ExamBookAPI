using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using ExamBook.Identity.Entities;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Participant:Entity
    {

        /// <summary>
        /// Registration id. Unique identifier of participant is real world.
        /// </summary>
        public string Code { get; set; } = "";
        public string NormalizedCode { get; set; } = "";

        public uint Index { get; set; }

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";

        public string FullName => $"{FirstName} {LastName}";
        public DateOnly BirthDate { get; set; }
        public char Sex { get; set; }

        public string? UserId { get; set; }
        
        [NotMapped]
        public User? User { get; set; }
        public Student? Student { get; set; }
        public ulong? StudentId { get; set; }

        
        public Examination Examination { get; set; } = null!;
        public ulong ExaminationId { get; set; }

        [JsonIgnore] public List<ParticipantSpeciality> ParticipantSpecialities { get; set; } = new();
        

        [JsonIgnore] public List<Paper> Papers { get; set; } = new();
    }
}