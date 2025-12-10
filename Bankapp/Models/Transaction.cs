
namespace Bankapp.Models
{
    public class Transaction(int accountId, decimal amount, string description , DateTime date, TransactionType type)
    {
        public int Id { get; set; }
        public decimal Amount { get; set; } = amount;
        public string Description { get; set; } = description;
        public DateTime Date { get; set; } = date;
        public TransactionType Type { get; set; } = type;
        public int AccountId { get; set; } = accountId;
        
        // Navigation properties
        public Account Account { get; set; }
    }
}
