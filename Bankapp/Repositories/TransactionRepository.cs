using Bankapp.Data;
using Bankapp.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Bankapp.Repositories
{
    public class TransactionRepository(BankappContext context) : ITransactionRepository
    {
        public BankappContext Context { get; } = context;
        public async Task<Transaction> GetTransactionById(int id)
        {
            return await Context.Transactions.FindAsync(id);
        }
        public async Task<IEnumerable<Transaction>> GetTransactionsByAccountId(int accountId)
        {
            return await Context.Transactions.Where(t => t.AccountId == accountId).OrderByDescending(t => t.Date).ToListAsync();
        }
        public async Task AddTransaction(Transaction transaction)
        {
            Context.Transactions.AddAsync(transaction);
            await Context.SaveChangesAsync();
        }
        public async Task UpdateTransaction(Transaction transaction)
        {
            Context.Transactions.Update(transaction);
            await Context.SaveChangesAsync();
        }
        public async Task DeleteTransaction(int id)
        {
            var transaction = await GetTransactionById(id);
            if (transaction != null) {
                Context.Transactions.Remove(transaction);
                await Context.SaveChangesAsync();
            }
        }
    }
}
