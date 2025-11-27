using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Bankapp.Data;
using Bankapp.Areas.Identity.Data;
using Bankapp.Repositories;
using Bankapp.Services;
namespace Bankapp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("BankappContextConnection") ?? throw new InvalidOperationException("Connection string 'BankappContextConnection' not found.");;

            builder.Services.AddDbContext<BankappContext>(options => options.UseSqlServer(connectionString));

            builder.Services.AddDefaultIdentity<BankappUser>(options => options.SignIn.RequireConfirmedAccount = false).AddEntityFrameworkStores<BankappContext>();
            
            // Add services to the container.
            builder.Services.AddRazorPages();

            builder.Services.AddScoped<IAccountRepository, AccountRepository>();

            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

            builder.Services.AddScoped<AccountService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthentication();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
