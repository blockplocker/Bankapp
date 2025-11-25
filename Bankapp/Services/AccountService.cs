using Bankapp.Data;
using Bankapp.Models;
using Microsoft.EntityFrameworkCore;

namespace Bankapp.Services
{
    public class AccountService
    {
        private readonly BankappContext _context;

        public AccountService(BankappContext context)
        {
            _context = context;
        }

        public async Task<Account> CreateAccountAsync(string userId, string accountName, decimal initialDeposit = 0m)
        {
            var account = new Account
            {
                AccountName = accountName,
                Balance = initialDeposit,
                AccountNumber = await GenerateAccountNumberAsync(),
                UserId = userId,
                Transactions = new List<Transaction>()
            };

            if (initialDeposit > 0)
            {
                account.Transactions.Add(new Transaction
                {
                    Amount = initialDeposit,
                    Date = DateTime.UtcNow,
                    Type = TransactionType.Deposit,
                    Account = account
                });
            }

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<IEnumerable<Account>> GetAccountsForUserAsync(string userId)
        {
            return await _context.Accounts
                .Where(a => a.UserId == userId)
                .Include(a => a.Transactions)
                .ToListAsync();
        }

        public async Task<Account?> GetAccountByIdAsync(int accountId)
        {
            return await _context.Accounts
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsAsync(int accountId)
        {
            return await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task DepositAsync(int accountId, decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Deposit amount must be positive", nameof(amount));

            var account = await _context.Accounts.FindAsync(accountId) ?? throw new InvalidOperationException("Account not found");

            account.Balance += amount;

            _context.Transactions.Add(new Transaction
            {
                AccountId = accountId,
                Amount = amount,
                Date = DateTime.UtcNow,
                Type = TransactionType.Deposit
            });

            await _context.SaveChangesAsync();
        }

        public async Task WithdrawAsync(int accountId, decimal amount)
        {
            if (amount <= 0) throw new ArgumentException("Withdrawal amount must be positive", nameof(amount));

            var account = await _context.Accounts.FindAsync(accountId) ?? throw new InvalidOperationException("Account not found");

            if (account.Balance < amount)
                throw new InvalidOperationException("Insufficient funds");

            account.Balance -= amount;

            _context.Transactions.Add(new Transaction
            {
                AccountId = accountId,
                Amount = amount,
                Date = DateTime.UtcNow,
                Type = TransactionType.Withdrawal
            });

            await _context.SaveChangesAsync();
        }

        public async Task TransferAsync(int fromAccountId, int toAccountId, decimal amount)
        {
            if (fromAccountId == toAccountId) throw new ArgumentException("Cannot transfer to the same account");
            if (amount <= 0) throw new ArgumentException("Transfer amount must be positive", nameof(amount));

            
            var accounts = await _context.Accounts
                .Where(a => a.AccountId == fromAccountId || a.AccountId == toAccountId)
                .ToDictionaryAsync(a => a.AccountId);

            if (!accounts.TryGetValue(fromAccountId, out var fromAccount)) throw new InvalidOperationException("Source account not found");
            if (!accounts.TryGetValue(toAccountId, out var toAccount)) throw new InvalidOperationException("Destination account not found");

            if (fromAccount.Balance < amount)
                throw new InvalidOperationException("Insufficient funds in source account");

            fromAccount.Balance -= amount;
            toAccount.Balance += amount;

            _context.Transactions.Add(new Transaction
            {
                AccountId = fromAccountId,
                Amount = amount,
                Date = DateTime.UtcNow,
                Type = TransactionType.Transfer
            });

            _context.Transactions.Add(new Transaction
            {
                AccountId = toAccountId,
                Amount = amount,
                Date = DateTime.UtcNow,
                Type = TransactionType.Transfer
            });

            await _context.SaveChangesAsync();
        }

        public async Task<bool> AccountExistsAsync(int accountId)
        {
            return await _context.Accounts.AnyAsync(a => a.AccountId == accountId);
        }

        private async Task<int> GenerateAccountNumberAsync()
        {
           
            var max = await _context.Accounts.MaxAsync(a => (int?)a.AccountNumber) ?? 10000000 - 1;
            return max + 1;
        }
    }
}
