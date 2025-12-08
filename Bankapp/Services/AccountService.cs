using Bankapp.Models;
using Bankapp.Repositories.Interfaces;
using Bankapp.Services.Interfaces;
using System.Security.Cryptography;

namespace Bankapp.Services
{
    public class AccountService(IAccountRepository accountRepository, ITransactionRepository transactionRepository) : IAccountService
    {
        private readonly IAccountRepository _accountRepository = accountRepository;
        private readonly ITransactionRepository _TransactionRepository = transactionRepository;

        public async Task<Account> CreateAccountAsync(string userId, string accountName, decimal initialDeposit = 0m)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("UserId must not be null or empty.", nameof(userId));
            if (string.IsNullOrWhiteSpace(accountName))
                throw new ArgumentException("AccountName must not be null or empty.", nameof(accountName));
            if (initialDeposit < 0)
                throw new ArgumentException("Initial deposit cannot be negative.", nameof(initialDeposit));

            Account account = new(accountName, initialDeposit, GenerateAccountNumber(), userId);

            await _accountRepository.AddAccountAsync(account);
            return account;
        }

        public async Task<IEnumerable<Account>> GetAccountsForUserAsync(string userId)
        {
            return await _accountRepository.GetAccountsByUserIdAsync(userId);
        }

        public async Task<Account?> GetAccountByIdAsync(int accountId)
        {
            return await _accountRepository.GetAccountByIdAsync(accountId);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(int accountId)
        {
            return await _TransactionRepository.GetTransactionsByAccountIdAsync(accountId);
        }

        public async Task DepositAsync(int accountId, decimal amount)
        {
            ValidateAmount(amount);

            Account? account = await _accountRepository.GetAccountByIdAsync(accountId) ?? throw new InvalidOperationException("Account not found");

            account.Balance += amount;
            await _accountRepository.UpdateAccountAsync(account);

            Transaction transaction = new Transaction(accountId, amount, DateTime.UtcNow, TransactionType.Deposit);

            await _TransactionRepository.AddTransactionAsync(transaction);
        }

        public async Task WithdrawAsync(int accountId, decimal amount)
        {
            ValidateAmount(amount);

            Account? account = await _accountRepository.GetAccountByIdAsync(accountId) ?? throw new InvalidOperationException("Account not found");

            if (account.Balance < amount)
                throw new InvalidOperationException("Insufficient funds");

            account.Balance -= amount;
            await _accountRepository.UpdateAccountAsync(account);

            Transaction transaction = new(accountId, -amount, DateTime.UtcNow, TransactionType.Withdrawal);

            await _TransactionRepository.AddTransactionAsync(transaction);
        }

        public async Task TransferAsync(int fromAccountId, int toAccountId, decimal amount)
        {
            if (fromAccountId == toAccountId) throw new ArgumentException("Cannot transfer to the same account");
            ValidateAmount(amount);

            Account fromAccount = await _accountRepository.GetAccountByIdAsync(fromAccountId) ?? throw new InvalidOperationException("Source account not found");
            Account toAccount = await _accountRepository.GetAccountByIdAsync(toAccountId) ?? throw new InvalidOperationException("Destination account not found");
            if (fromAccount.Balance < amount)
                throw new InvalidOperationException("Insufficient funds in source account");

            fromAccount.Balance -= amount;
            toAccount.Balance += amount;
            await _accountRepository.UpdateAccountAsync(fromAccount);
            await _accountRepository.UpdateAccountAsync(toAccount);

            Transaction fromTransaction = new(fromAccountId, -amount, DateTime.UtcNow, TransactionType.Transfer);

            Transaction toTransaction = new(toAccountId, amount, DateTime.UtcNow, TransactionType.Transfer);

            await _TransactionRepository.AddTransactionAsync(fromTransaction);
            await _TransactionRepository.AddTransactionAsync(toTransaction);
        }

        public async Task<bool> AccountExistsAsync(int accountId)
        {
            return await _accountRepository.GetAccountByIdAsync(accountId) != null;
        }

        public async Task RenameAccountAsync(int accountId, string newAccountName)
        {
            if (string.IsNullOrWhiteSpace(newAccountName))
                throw new ArgumentException("Kontonamn får inte vara tomt.", nameof(newAccountName));
            Account? account = await _accountRepository.GetAccountByIdAsync(accountId) ?? throw new InvalidOperationException("Account not found");
            account.AccountName = newAccountName;
            await _accountRepository.UpdateAccountAsync(account);
        }

        public async Task<int> GetAccountIdByAccountNumberAsync(int accountNumber)
        {
            Account? account = await _accountRepository.GetAccountByAccountNumberAsync(accountNumber);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found");
            }
            return account.AccountId;
        }

        private static int GenerateAccountNumber()
        {
            // random 9-digit positive account number [100000000, 999999999]
            return RandomNumberGenerator.GetInt32(100_000_000, 1_000_000_000);
        }

        private static void ValidateAmount(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Amount must be greater than zero.");
        }
    }
}
