using System;
using System.Runtime.Serialization;

namespace Vx.Asserts
{
    public class EventAssertionException:ApplicationException
    {
        public EventAssertionException()
        {
        }

        protected EventAssertionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public EventAssertionException(string? message) : base(message)
        {
        }

        public EventAssertionException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}