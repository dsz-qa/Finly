namespace Aplikacja_do_sledzenia_wydatkow.Models
{
    public class ExpenseDisplayModel
    {
        public int Id { get; set; }
        public double Amount { get; set; }
        public string Category { get; set; } // <-- wa¿ne
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string CategoryName { get; set; }
        public int UserId { get; set; }
    }
}