namespace Finly.Models
{
    public class Category
    {
        public int Id { get; set; }
        public int UserId { get; set; }               // właściciel kategorii
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";        // np. "Food24Regular"
        public string Color { get; set; } = "#808080";// HEX, np. "#4CAF50"
        /// <summary> "Expense" | "Income" | "Saving" </summary>
        public string Type { get; set; } = "Expense";
        public bool IsDeleted { get; set; }           // soft delete
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}