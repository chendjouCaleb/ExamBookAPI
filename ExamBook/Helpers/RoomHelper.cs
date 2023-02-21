using System;
using ExamBook.Entities;

namespace ExamBook.Helpers
{
    public static class RoomHelper
    {
        public static void ThrowNameUsed(Space space, string name)
        {
            throw new InvalidOperationException(
                $"The name: {name} is used by room is space: {space.Name}.");
        }
        
        public static void ThrowMinimalCapacityError(uint capacity)
        {
            throw new InvalidOperationException($"The minimal capacity of room is {capacity}.");
        }
    }
}