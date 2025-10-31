namespace Finly.Models
{
    public class UserProfile
    {
        // Osobiste
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Address { get; set; }

        // Firmowe (opcjonalne)
        public string? CompanyName { get; set; }
        public string? CompanyNip { get; set; }
        public string? CompanyAddress { get; set; }
    }
}
