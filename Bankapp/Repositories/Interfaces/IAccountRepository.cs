using Bankapp.Models;

namespace Bankapp.Repositories.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account?> GetAccountByIdAsync(int id);
        Task<IEnumerable<Account>> GetAccountsByUserIdAsync(string id);
        Task AddAccountAsync(Account account);
        Task UpdateAccountAsync(Account account);
        Task DeleteAccountAsync(Account account);
        Task<Account?> GetAccountByAccountNumberAsync(int accountNumber);
    }
}
