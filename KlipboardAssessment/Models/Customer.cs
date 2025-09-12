using System.ComponentModel.DataAnnotations;

namespace KlipboardAssessment.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        [StringLength(15, MinimumLength = 15)] // The account number must be EXACTLY 15 char length
        public required string AccountNumber { get; set; }

        [Required]
        public required decimal Balance { get; set; }

        // Navigation property - ONE customer = MANY transactions
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
