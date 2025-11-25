using Bankapp.Data;
using Bankapp.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Bankapp.Repositories
{
    public class AccountRepository(BankappContext context) : IAccountRepository
    {
        public BankappContext Context { get; } = context;

        public async Task<Account> GetAccountById(int id)
        {
            return await Context.Accounts.FindAsync(id);
        }
        public async Task<IEnumerable<Account>> GetAccountsByUserId(string id)
        {
            return await Context.Accounts.Where(a => a.UserId == id).ToListAsync();
        }
        public async Task AddAccount(Account account)
        {
            Context.Accounts.Add(account);
            await Context.SaveChangesAsync();
        }
        public async Task UpdateAccount(Account account)
        {
            Context.Accounts.Update(account);
            await Context.SaveChangesAsync();
        }
        public async Task DeleteAccount(int id)
        {
            Context.Accounts.Remove(await GetAccountById(id));
            await Context.SaveChangesAsync();
        }
    }
}
