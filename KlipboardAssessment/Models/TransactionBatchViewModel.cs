namespace KlipboardAssessment.Models
{
    public class TransactionBatchViewModel
    {
        // For single-transaction UI scenarios you can use Transactions[0]
        public List<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
