using System;

namespace ExamBook.Exceptions
{
    public class BaseException:ApplicationException
    {
        public string Code { get; }
        public object[] Params { get;  } 
        
        
        public BaseException(string code, params object[] parameters) : base(code)
        {
            Code = code;
            Params = parameters;
        }
    }
}