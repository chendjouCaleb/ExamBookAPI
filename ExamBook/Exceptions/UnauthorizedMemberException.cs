namespace ExamBook.Exceptions
{
    public class UnauthorizedMemberException:BaseException
    {
        public UnauthorizedMemberException(string code, params object[] parameters) : base(code, parameters) {}
    }
}