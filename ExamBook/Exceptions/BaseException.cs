using System;

namespace ExamBook.Exceptions
{
    public class BaseException:ApplicationException
    {
        public BaseException(string? message) : base(message)
        {
        }
    }
}