using Microsoft.AspNetCore.Identity;

namespace Bankapp.Models
{
    public class Account
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public int AccountNumber { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }
        public ICollection<Transaction> Transactions { get; set; }
    }
}
