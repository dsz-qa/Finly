namespace Aplikacja_do_sledzenia_wydatkow.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public double Amount { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public DateTime Date { get; set; }  // format: yyyy-MM-dd
        public string Description { get; set; }
        public int UserId { get; set; }
        public string Category { get; set; }
    }
}