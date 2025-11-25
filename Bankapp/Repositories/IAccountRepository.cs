using Bankapp.Models;

namespace Bankapp.Repositories
{
    public interface IAccountRepository
    {
        Task<Account> GetAccountById(int id);
        Task<IEnumerable<Account>> GetAccountsByUserId(string id);
        Task AddAccount(Account account);
        Task UpdateAccount(Account account);
        Task DeleteAccount(int id);
    }
}
