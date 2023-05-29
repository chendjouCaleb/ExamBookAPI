namespace ExamBook.Exceptions
{
    public class IllegalOperationException:BaseException
    {
        public IllegalOperationException(string message, params object[] parameters) : base(message, parameters)
        {
        }
    }
}