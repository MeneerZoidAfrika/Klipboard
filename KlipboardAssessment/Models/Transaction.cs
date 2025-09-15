using System.ComponentModel.DataAnnotations;

namespace KlipboardAssessment.Models
{
    public class Transaction
    {
        public int Id { get; set; } // PK

        [Required]
        public string AccountNumber { get; set; }
        public DateTime Date { get; set; }

        [MaxLength(200)]
        public string Reference { get; set; }
        public decimal Amount { get; set; }

        [MaxLength(1)]
        public string Type { get; set; } // Debit or Credit
        public int CustomerId { get; set; } //FK from Customer
        public Customer? Customer { get; set; } // Navigation property
    }
}
