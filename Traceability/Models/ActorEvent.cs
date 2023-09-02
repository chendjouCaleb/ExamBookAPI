namespace Traceability.Models
{
	public class ActorEvent
	{
		public ActorEvent(){}
		public ActorEvent(Actor actor, Event @event)
		{
			Actor = actor;
			Event = @event;
		}

		public long Id { get; set; }
		public Actor? Actor { get; set; }
		public string ActorId { get; set; } = null!;
        
		public Event? Event { get; set; } 
		public long? EventId { get; set; }
	}
}