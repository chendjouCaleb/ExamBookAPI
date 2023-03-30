using System;

namespace DriveIO.Models
{
    public class BaseFile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";
        public string ContentType { get; set; } = "";
        public string Sha512 { get; set; } = "";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? DeletedAt { get; set; } = null!;
        public bool IsDeleted => DeletedAt != null;


        public Folder? Folder { get; set; } = null!;
        public string FolderId { get; set; } = null!;
    }
}