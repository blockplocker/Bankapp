using Bankapp.Data;
using Bankapp.Models;
using Bankapp.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bankapp.Repositories
{
    public class TransactionRepository(BankappContext context) : ITransactionRepository
    {
        public BankappContext Context { get; } = context;
        public async Task<Transaction> GetTransactionByIdAsync(int id)
        {
            return await Context.Transactions.FindAsync(id);
        }
        public async Task<IEnumerable<Transaction>> GetTransactionsByAccountIdAsync(int accountId)
        {
            return await Context.Transactions.Where(t => t.AccountId == accountId).OrderByDescending(t => t.Date).ToListAsync();
        }
        public async Task AddTransactionAsync(Transaction transaction)
        {
            Context.Transactions.AddAsync(transaction);
            await Context.SaveChangesAsync();
        }
        public async Task UpdateTransactionAsync(Transaction transaction)
        {
            Context.Transactions.Update(transaction);
            await Context.SaveChangesAsync();
        }
        public async Task DeleteTransactionAsync(int id)
        {
            var transaction = await GetTransactionByIdAsync(id);
            if (transaction != null) {
                Context.Transactions.Remove(transaction);
                await Context.SaveChangesAsync();
            }
        }
    }
}
