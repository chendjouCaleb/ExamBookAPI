namespace ExamBook.Entities
{
    public class Paper
    {
        public float Score { get; set; }
        public Test Test { get; set; }
        public long TestId { get; set; }
        
    }
}