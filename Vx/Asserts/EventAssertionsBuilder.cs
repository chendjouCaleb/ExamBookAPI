using System;
using Vx.Models;

namespace Vx.Asserts
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