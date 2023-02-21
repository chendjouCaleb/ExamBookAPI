using System;

namespace ExamBook.Exceptions
{
    public class IncompatibleEntityException<T, I>:ApplicationException
    {
        public T entity1 { get; }
        public I entity2 { get; }

        public IncompatibleEntityException(T entity1, I entity2, string message = "{entity.constraints.incompatible}") : base(message)
        {
            this.entity1 = entity1;
            this.entity2 = entity2;
        }
        
    }
    
    public class IncompatibleEntityException:ApplicationException
    {
        public object entity1 { get; }
        public object entity2 { get; }

        public IncompatibleEntityException(object entity1, object entity2, string message = "{entity.constraints.incompatible}") : base(message)
        {
            this.entity1 = entity1;
            this.entity2 = entity2;
        }
        
    }
}