namespace ExamBook.Exceptions
{
    public class InvalidStateException:BaseException
    {
        public InvalidStateException(string message, params object[] parameters) : base(message, parameters)
        {
        }
    }
}