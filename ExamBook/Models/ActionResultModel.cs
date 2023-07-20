using Traceability.Models;

namespace ExamBook.Models
{
    public class ActionResultModel<TItem>
    {
        public ActionResultModel(TItem item, Event @event)
        {
            Item = item;
            Event = @event;
        }

        public TItem Item { get; set; }
        public Event Event { get; set; }
    }
}