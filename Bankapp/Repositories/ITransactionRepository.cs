using Bankapp.Models;

namespace Bankapp.Repositories
{
    public interface ITransactionRepository
    {
        Task<Transaction> GetTransactionByIdAsync(int id);
        Task<IEnumerable<Transaction>> GetTransactionsByAccountIdAsync(int accountId);
        Task AddTransactionAsync(Transaction transaction);
        Task UpdateTransactionAsync(Transaction transaction);
        Task DeleteTransactionAsync(int id);
    }
}
