using System;
using System.Collections.Generic;

namespace DriveIO.Models
{
    public class Folder
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public string NormalizedName { get; set; } = "";

        public List<Folder> Folders { get; set; } = new ();
    }
}