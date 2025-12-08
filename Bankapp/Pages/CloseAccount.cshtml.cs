using System.Security.Claims;
using System.Threading.Tasks;
using Bankapp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bankapp.Pages
{
    [Authorize]
    public class CloseAccountModel : PageModel
    {
        private readonly IAccountService _accountService;
        public CloseAccountModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty(SupportsGet = true)]
        public int AccountId { get; set; }
        public string? StatusMessage { get; set; }

        public void OnGet(int accountId)
        {
            AccountId = accountId;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                await _accountService.CloseAccountAsync(AccountId);
                StatusMessage = "Konto stängt!";
                return RedirectToPage("BankAccountPage");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fel: {ex.Message}";
                return Page();
            }
        }
    }
}
