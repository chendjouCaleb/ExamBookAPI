using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class TestGroup:Entity
    {

        public uint Index { get; set; }
        
        public uint Capacity { get; set; }
        
        [JsonIgnore] 
        public Room Room { get; set; } = null!;

        public long? RoomId { get; set; }

        [JsonIgnore] public Test Test { get; set; } = null!;
        public long TestId { get; set; }


        [JsonIgnore] public List<Paper> Papers { get; set; } = new();
    }
}