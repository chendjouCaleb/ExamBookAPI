using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
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


        public Examination Examination { get; set; } = null!;
        public ulong ExaminationId { get; set; }

        public Student? Student { get; set; }
        public ulong? StudentId { get; set; }

        [JsonIgnore] public List<ParticipantSpeciality> ParticipantSpecialities { get; set; } = new();
        

        [JsonIgnore] public List<Paper> Papers { get; set; } = new();
    }
}