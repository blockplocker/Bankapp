using Bankapp.Models;

namespace Bankapp.Repositories
{
    public interface ITransactionRepository
    {
        Task<Transaction> GetTransactionById(int id);
        Task<IEnumerable<Transaction>> GetTransactionsByAccountId(int accountId);
        Task AddTransaction(Transaction transaction);
        Task UpdateTransaction(Transaction transaction);
        Task DeleteTransaction(int id);
    }
}
