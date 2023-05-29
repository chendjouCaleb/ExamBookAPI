namespace ExamBook.Exceptions
{
    public class UsedValueException:BaseException
    {
        public UsedValueException(string code, params object[] parameters) : base(code, parameters)
        {
        }
    }
}