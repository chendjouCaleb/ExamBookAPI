using System;
using Traceability.Models;

namespace Traceability.Asserts
{
    public class EventAssertionsBuilder
    {
        private IServiceProvider _provider;

        public EventAssertionsBuilder(IServiceProvider provider)
        {
            _provider = provider;
        }


        public EventAssertions Build(Event @event)
        {
            return new EventAssertions(@event, _provider);
        }
    }
}