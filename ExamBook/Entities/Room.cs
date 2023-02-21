using Newtonsoft.Json;

namespace ExamBook.Entities
{
    public class Room:Entity
    {
        public string Name { get; set; } = "";
        public uint Capacity { get; set; }

        [JsonIgnore] public Space Space { get; set; } = null!;
        public ulong SpaceId { get; set; }
    }
}