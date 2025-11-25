using Bankapp.Data;
using Bankapp.Models;
using Bankapp.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Bankapp.Services
{
    public class AccountService(IAccountRepository accountRepository, ITransactionRepository transactionRepository)
    {
        private readonly IAccountRepository _accountRepository = accountRepository;
        private readonly ITransactionRepository _TransactionRepository = transactionRepository;

        public async Task<Account> CreateAccountAsync(string userId, string accountName, decimal initialDeposit = 0m)
        {
            Account account = new Account
            {
                AccountName = accountName,
                Balance = initialDeposit,
                AccountNumber = await GenerateAccountNumberAsync(),
                UserId = userId,
                Transactions = new List<Transaction>()
            };

            await _accountRepository.AddAccount(account);
            return account;
        }

        public async Task<IEnumerable<Account>> GetAccountsForUserAsync(string userId)
        {
            return await _accountRepository.GetAccountsByUserId(userId);
        }

        public async Task<Account?> GetAccountByIdAsync(int accountId)
        {
            return await _accountRepository.GetAccountById(accountId);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(int accountId)
        {
            return await _TransactionRepository.GetTransactionsByAccountId(accountId);
        }

        public async Task DepositAsync(int accountId, decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Deposit amount must be positive", nameof(amount));

            var account = await _accountRepository.GetAccountById(accountId) ?? throw new InvalidOperationException("Account not found");

            account.Balance += amount;

            var transaction = new Transaction
            {
                AccountId = accountId,
                Amount = amount,
                Date = DateTime.UtcNow,
                Type = TransactionType.Deposit
            };

            await _TransactionRepository.AddTransaction(transaction);
        }

        public async Task WithdrawAsync(int accountId, decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Withdrawal amount must be positive", nameof(amount));

            var account = await _accountRepository.GetAccountById(accountId) ?? throw new InvalidOperationException("Account not found");

            if (account.Balance < amount)
                throw new InvalidOperationException("Insufficient funds");

            account.Balance -= amount;

            var transaction = new Transaction
            {
                AccountId = accountId,
                Amount = amount,
                Date = DateTime.UtcNow,
                Type = TransactionType.Withdrawal
            };

            await _TransactionRepository.AddTransaction(transaction);
        }

        public async Task TransferAsync(int fromAccountId, int toAccountId, decimal amount)
        {
            if (fromAccountId == toAccountId) throw new ArgumentException("Cannot transfer to the same account");
            if (amount <= 0) throw new ArgumentException("Transfer amount must be positive", nameof(amount));

            // throw new InvalidOperationException("Source account not found");
            // throw new InvalidOperationException("Destination account not found");
            Account fromAccount = await _accountRepository.GetAccountById(fromAccountId);
            Account toAccount = await _accountRepository.GetAccountById(toAccountId);

            if (fromAccount.Balance < amount)
                throw new InvalidOperationException("Insufficient funds in source account");

            fromAccount.Balance -= amount;
            toAccount.Balance += amount;

            Transaction fromTransaction = new Transaction
            {
                AccountId = fromAccountId,
                Amount = -amount,
                Date = DateTime.UtcNow,
                Type = TransactionType.Transfer
            };

            Transaction toTransaction = new Transaction
            {
                AccountId = toAccountId,
                Amount = amount,
                Date = DateTime.UtcNow,
                Type = TransactionType.Transfer
            };

            await _TransactionRepository.AddTransaction(fromTransaction);
            await _TransactionRepository.AddTransaction(toTransaction);
        }

        public async Task<bool> AccountExistsAsync(int accountId)
        {
            return await _accountRepository.GetAccountById(accountId) != null;
        }

        private async Task<int> GenerateAccountNumberAsync()
        {
            return 0; // Placeholder for account number generation logic
        }
    }
}
