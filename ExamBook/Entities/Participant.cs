using System;
using System.Collections.Generic;

namespace ExamBook.Entities
{
    public class Participant
    {

        /// <summary>
        /// Registration id. Unique identifier of participant is real world.
        /// </summary>
        public string Rid { get; set; } = "";

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public DateTime BirthDate { get; set; }
        public char Sex { get; set; }


        public Examination Examination { get; set; }
        public long ExaminationId { get; set; }

        public List<Paper> Papers { get; set; } = new();
    }
}