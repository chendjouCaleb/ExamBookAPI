using System;
using System.Collections.Generic;

namespace DriveIO.Helpers
{
    public class MimeHelper
    {
        public static Dictionary<string, string> Mimes = new ()
        {
            {".txt", "text/plain"},
            {".pdf", "application/pdf"},
            {".doc", "application/vnd.ms-word"},
            {".docx", "application/vnd.ms-word"},
            {".png", "image/png"},
            {".jpg", "image/jpeg"},
        };

        public static string GetMime(string ext)
        {
            string mime = Mimes[ext];

            if (string.IsNullOrWhiteSpace(mime))
            {
                throw new InvalidOperationException($"Mime type not found for '{mime}' extension.");
            }


            return mime;
        }
    }
}