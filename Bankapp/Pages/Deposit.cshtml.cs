using Bankapp.Models;
using Bankapp.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Bankapp.Pages
{
    public class DepositModel(IAccountService accountService) : PageModel
    {
        private readonly IAccountService _accountService = accountService;

        [BindProperty]
        public InputModel Input { get; set; } = new();
        public List<Account> UserAccounts { get; set; } = [];
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Konto")]
            public int? AccountId { get; set; }

            [Required]
            [Range(0.01, double.MaxValue, ErrorMessage = "Beloppet måste vara större än 0.")]
            [Display(Name = "Belopp")]
            public decimal Amount { get; set; }
        }

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var accounts = await _accountService.GetAccountsForUserAsync(userId);
                UserAccounts = new List<Account>(accounts);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }
            try
            {
                await _accountService.DepositAsync(Input.AccountId!.Value, Input.Amount);
                StatusMessage = "Insättning genomförd!";
                return RedirectToPage("BankAccountPage");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fel: {ex.Message}";
                await OnGetAsync();
                return Page();
            }
        }
    }
}
