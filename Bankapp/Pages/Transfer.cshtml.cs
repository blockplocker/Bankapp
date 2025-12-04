using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Bankapp.Models;
using Bankapp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;

namespace Bankapp.Pages
{
    [Authorize]
    public class TransferModel : PageModel
    {
        private readonly IAccountService _accountService;
        public TransferModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        public List<Account> UserAccounts { get; set; } = new();
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Från konto")]
            public int? FromAccountId { get; set; }

            [Required]
            [Display(Name = "Till konto")]
            public int? ToAccountId { get; set; }

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
            if (Input.FromAccountId == Input.ToAccountId)
            {
                StatusMessage = "Du kan inte överföra till samma konto.";
                await OnGetAsync();
                return Page();
            }
            try
            {
                await _accountService.TransferAsync(Input.FromAccountId!.Value, Input.ToAccountId!.Value, Input.Amount);
                StatusMessage = "Överföring genomförd!";
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
