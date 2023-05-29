using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamBook.Entities
{
    public class Paper:Entity
    {
        public float? Score { get; set; }
        public uint IndexInGroup { get; set; }
        
        [NotMapped] public bool IsCorrected => Score != null;

        public bool IsPresent { get; set; }


        public Participant? Participant { get; set; }
        public ulong? ParticipantId { get; set; }

        public Student? Student { get; set; }
        public ulong? StudentId { get; set; }

        public Test? Test { get; set; }
        public ulong TestId { get; set; }

        public List<PaperSpeciality> PaperSpecialities { get; set; } = new();

        public TestSpeciality? TestSpeciality { get; set; }
        public ulong? TestSpecialityId { get; set; }
    }
        
        public class PaperSpeciality:Entity
        {
            public PaperSpeciality() {}
            public PaperSpeciality(Paper paper, ParticipantSpeciality? participantSpeciality)
            {
                ParticipantSpeciality = participantSpeciality;
                Paper = paper;
            }
            

            public Paper? Paper { get; set; }
            public ulong? PaperId { get; set; }

            public ParticipantSpeciality? ParticipantSpeciality { get; set; }
            public ulong? ParticipantSpecialityId { get; set; }
        }
}