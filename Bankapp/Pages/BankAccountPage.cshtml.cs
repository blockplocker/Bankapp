using Bankapp.Areas.Identity.Data;
using Bankapp.Models;
using Bankapp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bankapp.Pages
{
    [Authorize]
    public class BankAccountPageModel(
        IAccountService _accountService,
        UserManager<BankappUser> _userManager) : PageModel
    {
        public IEnumerable<Account> Accounts { get; set; } = [];
        public Account? SelectedAccount { get; set; }

        public BankappUser? CurrentUser { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? AccountId { get; set; }

        public async Task OnGet()
        {
            CurrentUser = await _userManager.GetUserAsync(User);
            Accounts = await _accountService.GetAccountsForUserAsync(CurrentUser.Id);
            foreach (var account in Accounts)
            {
                account.Transactions = (await _accountService.GetTransactionsAsync(account.AccountId)).ToList();
            }
            if(AccountId != null)
            {
                SelectedAccount = await _accountService.GetAccountByIdAsync(AccountId.Value);
            }
        }
    }
}
