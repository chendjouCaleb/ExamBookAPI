namespace Social.Entities
{
    public class PostFile
    {
        public long Id { get; set; }

        public Post? Post { get; set; }
        public long PostId { get; set; }
        
        public string FileId { get; set; } = "";
        public string ThumbId { get; set; } = "";
        public string PosterId { get; set; } = "";


        public bool IsPicture { get; set; }
        public bool IsGif { get; set; }
        public bool IsVideo { get; set; }
        public bool IsAudio { get; set; }
    }
}