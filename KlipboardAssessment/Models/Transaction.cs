using System.ComponentModel.DataAnnotations;

namespace KlipboardAssessment.Models
{
    public class Transaction
    {
        public int Id { get; set; } // PK

        [Required]
        public required string AccountNumber { get; set; }
        public DateTime Date { get; set; }

        [MaxLength(200)]
        public required string Reference { get; set; }
        public required decimal Amount { get; set; }

        [MaxLength(1)]
        public required string Type { get; set; } // Debit or Credit
        public int CustomerId { get; set; } //FK from Customer
        public Customer? Customer { get; set; } // Navigation property
    }
}
