using System.Collections.Generic;

namespace DriveIO
{
    public class DriveOptions
    {
        public ulong ImageMaxSize { get; set; }
        
        public long VideoMaxSize { get; set; }

        public long AudioMaxSize { get; set; }

        public long GifMaxSize { get; set; }

        public IEnumerable<string> AllowedPictureExtensions { get; set; } = new[] { ".jpeg", ".jpg", ".png"};
        public IEnumerable<string> AllowedVideosExtensions { get; set; } = new[] { ".mp4"};
        public IEnumerable<string> AllowedAudioExtensions { get; set; } = new[] { ".mp3"};
    }
}