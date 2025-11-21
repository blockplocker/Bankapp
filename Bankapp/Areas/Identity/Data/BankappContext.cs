using Bankapp.Areas.Identity.Data;
using Bankapp.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bankapp.Data;

public class BankappContext : IdentityDbContext<BankappUser>
{
    public BankappContext(DbContextOptions<BankappContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Specify decimal precision to avoid truncation warnings for SQL Server
        builder.Entity<Account>()
            .Property(a => a.Balance)
            .HasPrecision(18, 2);

        builder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

    }
}
