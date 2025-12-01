using Bankapp.Areas.Identity.Data;

namespace Bankapp.Models
{
    public class Account(string accountName, decimal balance, int accountNumber, string userId, ICollection<Transaction> transactions)
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = accountName;
        public decimal Balance { get; set; } = balance;
        public int AccountNumber { get; set; } = accountNumber;


        public string UserId { get; set; } = userId;
        public BankappUser User { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = transactions;
    }
}
