﻿using System.Collections.Generic;

namespace ExamBook.Models
{
    public class ClassroomAddModel
    {
        public string Name { get; set; } = "";

        public HashSet<ulong> SpecialityIds { get; set; } = new();
        public ulong RoomId { get; set; }
    }
}