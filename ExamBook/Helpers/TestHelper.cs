using System;
using ExamBook.Entities;

namespace ExamBook.Helpers
{
    public static class TestHelper
    {
        public static void ThrowNameUsed(Examination examination, string name)
        {
            throw new InvalidOperationException(
                $"The name: {name} is used by another test is examination: {examination.Name}.");
        }
        
        public static void ThrowMinimalCapacityError(uint capacity)
        {
            throw new InvalidOperationException($"The minimal capacity of room is {capacity}.");
        }
        
        public static void ThrowDuplicateTestGroup(Test test, Room room)
        {
            throw new InvalidOperationException($"The are already test group with room {room.Name} in test: {test.Name}.");
        }
    }
}