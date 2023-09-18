namespace Traceability.Models
{
	public class SubjectEvent
	{
		public SubjectEvent(){}
		public SubjectEvent(Subject subject, Event @event)
		{
			Subject = subject;
			Event = @event;
		}

		public long Id { get; set; }
		public Subject? Subject { get; set; }
		public string SubjectId { get; set; } = null!;
        
		public Event? Event { get; set; } 
		public long? EventId { get; set; }
	}
}