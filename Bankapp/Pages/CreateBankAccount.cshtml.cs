using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using Bankapp.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bankapp.Pages
{
    [Authorize]
    public class CreateBankAccountModel : PageModel
    {
        private readonly IAccountService _accountService;
        public CreateBankAccountModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Kontonamn")]
            public string AccountName { get; set; }

            [Range(0, double.MaxValue, ErrorMessage = "Startinsättning kan inte vara negativ.")]
            [Display(Name = "Startinsättning")]
            public decimal InitialDeposit { get; set; } = 0m;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                StatusMessage = "Kunde inte identifiera användare.";
                return Page();
            }

            try
            {
                await _accountService.CreateAccountAsync(userId, Input.AccountName, Input.InitialDeposit);
                StatusMessage = "Konto skapat!";
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
