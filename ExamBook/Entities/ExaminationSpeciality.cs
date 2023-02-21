using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class ExaminationSpeciality:Entity
    {
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
        
        
        [JsonIgnore]
        public Examination Examination { get; set; } = null!;
        public ulong ExaminationId { get; set; }


        [JsonIgnore] public List<ParticipantSpeciality> ParticipantSpecialities { get; set; } = new();

    }
}