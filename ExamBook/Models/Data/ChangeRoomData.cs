namespace ExamBook.Models.Data
{
    public class ChangeRoomData
    {
        public ChangeRoomData(ulong? lastRoomId, ulong? currentRoomId)
        {
            LastRoomId = lastRoomId;
            CurrentRoomId = currentRoomId;
        }

        public ChangeRoomData(ulong? currentRoomId)
        {
            CurrentRoomId = currentRoomId;
        }

        public ulong? LastRoomId { get; set; }
        public ulong? CurrentRoomId { get; set; }
    }
}