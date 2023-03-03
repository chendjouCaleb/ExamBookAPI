using System;
using ExamBook.Identity.Models;

namespace ExamBook.Identity
{
    public class UserHelper
    {
        public static void AssertNotDeleted(User user)
        {
            if (user.Deleted)
            {
                throw new InvalidOperationException($"User with id=${user.Id} is deleted.");
            }
        }
    }
}