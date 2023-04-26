using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using DriveIO.Models;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Space:Entity
    {
        public string Name { get; set; } = "";
        public string Identifier { get; set; } = "";
        public string NormalizedIdentifier { get; set; } = "";


        /// <summary>
        /// Tells if the space was verified and can be trusted.
        /// </summary>
        public bool IsCertified { get; set; }


        /// <summary>
        /// Tells if anyone can see public information of this space.
        /// If the space is private, only her members and students or admin can see it.
        /// </summary>
        public bool IsPublic { get; set; }

        public bool IsPrivate => !IsPublic;
        
        

        public string Twitter { get; set; } = "";
        public string Youtube { get; set; } = "";
        public string Facebook { get; set; } = "";
        public string Instagram { get; set; } = "";
        public string Website { get; set; } = "";

        [NotMapped]
        public Picture? ImageFile { get; set; }
        public string ImageId { get; set; } = "";
        public string CoverImageId { get; set; } = "";
        
        
        [JsonIgnore] public HashSet<Course> Courses { get; set; } = new();
        [JsonIgnore] public HashSet<Classroom> Classrooms { get; set; } = new();
        [JsonIgnore] public HashSet<Examination> Examinations { get; set; } = new();
        [JsonIgnore] public HashSet<Room> Rooms { get; set; } = new();
        [JsonIgnore] public HashSet<Member> Members { get; set; } = new();
        
    }
}