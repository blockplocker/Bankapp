using Bankapp.Models;

namespace Bankapp.Services.Interfaces
{
    public interface IAccountService
    {
        Task<Account> CreateAccountAsync(string userId, string accountName, decimal initialDeposit = 0m);
        Task<IEnumerable<Account>> GetAccountsForUserAsync(string userId);
        Task<Account?> GetAccountByIdAsync(int accountId);
        Task<IEnumerable<Transaction>> GetTransactionsAsync(int accountId);
        Task DepositAsync(int accountId, decimal amount);
        Task WithdrawAsync(int accountId, decimal amount);
        Task TransferAsync(int fromAccountId, int toAccountId, decimal amount);
        Task<bool> AccountExistsAsync(int accountId);
        Task RenameAccountAsync(int accountId, string newAccountName);
        Task<int> GetAccountIdByAccountNumberAsync(int accountNumber);
    }
}
