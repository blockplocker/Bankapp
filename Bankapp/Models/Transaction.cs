
namespace Bankapp.Models
{
    public class Transaction(int accountId, decimal amount, DateTime date, TransactionType type)
    {
        public int Id { get; set; }
        public decimal Amount { get; set; } = amount;
        public DateTime Date { get; set; } = date;
        public TransactionType Type { get; set; } = type;


        public int AccountId { get; set; } = accountId;
        public Account Account { get; set; }
    }
}
