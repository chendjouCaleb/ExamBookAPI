using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Vx.Models;

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

        [JsonIgnore] public List<ParticipantSpeciality> ParticipantSpecialities { get; set; } = new();
        

        [JsonIgnore] public List<Paper> Papers { get; set; } = new();
        
        [NotMapped] public Publisher? Publisher { get; set; } 
    }
}