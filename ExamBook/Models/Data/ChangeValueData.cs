namespace ExamBook.Models.Data
{
    public class ChangeValueData<T>
    {
        public ChangeValueData(T? lastValue, T? currentValue)
        {
            LastValue = lastValue;
            CurrentValue = currentValue;
        }

        public ChangeValueData(T? currentValue)
        {
            CurrentValue = currentValue;
        }

        public T? LastValue { get; set; }
        public T? CurrentValue { get; set; }
    }
}