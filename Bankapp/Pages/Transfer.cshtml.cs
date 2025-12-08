using System.ComponentModel.DataAnnotations;
using System.Linq;
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
    public class TransferModel(IAccountService accountService) : PageModel
    {
        private readonly IAccountService _accountService = accountService;

        [BindProperty]
        public InputModel Input { get; set; } = new();
        public List<Account> UserAccounts { get; set; } = new();
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Från konto")]
            public int? FromAccountId { get; set; }

            [Display(Name = "Till konto")]
            public int? ToAccountId { get; set; }

            [Display(Name = "Mottagarens kontonummer")]
            public int? RecipientAccountNumber { get; set; }

            [Required]
            [Range(0.01, double.MaxValue, ErrorMessage = "Beloppet måste vara större än 0.")]
            [Display(Name = "Belopp")]
            public decimal Amount { get; set; }

            [Display(Name = "Typ av överföring")]
            public bool IsExternal { get; set; } = false; 
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

            // Reload accounts for validation and view rendering in case of error
            await OnGetAsync();

            // Validate FromAccountId ownership
            if (Input.FromAccountId == null || !UserAccounts.Any(a => a.AccountId == Input.FromAccountId.Value))
            {
                ModelState.AddModelError(nameof(Input.FromAccountId), "Ogiltigt källkonto.");
                return Page();
            }

            // Resolve ToAccountId based on transfer type
            if (Input.IsExternal)
            {
                if (Input.RecipientAccountNumber == null || Input.RecipientAccountNumber <= 0)
                {
                    ModelState.AddModelError(nameof(Input.RecipientAccountNumber), "Ange ett giltigt kontonummer för mottagaren.");
                    return Page();
                }

                int toAccountId;
                try
                {
                    toAccountId = await _accountService.GetAcountIdByAccountNumberAsync(Input.RecipientAccountNumber.Value);
                }
                catch (System.Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Fel vid sökning av kontonummer: {ex.Message}");
                    return Page();
                }

                if (toAccountId <= 0)
                {
                    ModelState.AddModelError(nameof(Input.RecipientAccountNumber), "Kontonummer hittades inte.");
                    return Page();
                }

                Input.ToAccountId = toAccountId;
            }
            else
            {
                if (Input.ToAccountId == null || !UserAccounts.Any(a => a.AccountId == Input.ToAccountId.Value))
                {
                    ModelState.AddModelError(nameof(Input.ToAccountId), "Välj ett giltigt mottagarkonto.");
                    return Page();
                }
            }

            // Prevent transferring to same account
            if (Input.FromAccountId == Input.ToAccountId)
            {
                StatusMessage = "Du kan inte överföra till samma konto.";
                return Page();
            }

            try
            {
                await _accountService.TransferAsync(Input.FromAccountId!.Value, Input.ToAccountId!.Value, Input.Amount);
                StatusMessage = "Överföring genomförd!";
                return RedirectToPage("BankAccountPage");
            }
            catch (System.Exception ex)
            {
                StatusMessage = $"Fel: {ex.Message}";
                return Page();
            }
        }
    }
}
