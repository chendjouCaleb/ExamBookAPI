namespace Social.Models
{
    public class PostAddModel
    {
        public string Content { get; set; } = "";
        public string MetaData { get; set; } = "";
        public string AuthorId { get; set; } = "";
    }

    public class PostAddPictureModel
    {
        public long PostId { get; set; }
        public string FileId { get; set; } = "";
        public string ThumbId { get; set; } = "";
    }
}