using Bankapp.Data;
using Bankapp.Models;
using Bankapp.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Bankapp.Repositories
{
    public class AccountRepository(BankappContext context) : IAccountRepository
    {
        private readonly BankappContext _context = context ?? throw new ArgumentNullException(nameof(context));

        public async Task<Account?> GetAccountByIdAsync(int id)
        {
            return await _context.Accounts.FindAsync(id);
        }
        public async Task<IEnumerable<Account>> GetAccountsByUserIdAsync(string id)
        {
            return await _context.Accounts.Where(a => a.UserId == id).ToListAsync();
        }
        public async Task AddAccountAsync(Account account)
        {
            await _context.Accounts.AddAsync(account);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAccountAsync(Account account)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAccountAsync(Account account)
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
        }
        public async Task<Account?> GetAccountByAccountNumberAsync(int accountNumber)
        {
            return await _context.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }
    }
}
