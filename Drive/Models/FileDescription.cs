using System.IO;

namespace DriveIO.Models
{
    public class FileDescription
    {
        public string FileName { get; set; } = "";
        public Stream Stream { get; set; } = null!;
    }
}