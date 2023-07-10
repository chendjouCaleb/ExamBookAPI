namespace ExamBook.Exceptions
{
    public class DuplicateValueException:BaseException
    {
        public DuplicateValueException(string code, params object[] parameters) : base(code, parameters)
        {
        }
    }
}