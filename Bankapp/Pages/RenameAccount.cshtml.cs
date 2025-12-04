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
    public class RenameAccountModel : PageModel
    {
        private readonly IAccountService _accountService;
        public RenameAccountModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();
        public string? StatusMessage { get; set; }
        [BindProperty(SupportsGet = true)]
        public int AccountId { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Nytt kontonamn")]
            public string NewAccountName { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int accountId)
        {
            AccountId = accountId;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();
            try
            {
                await _accountService.RenameAccountAsync(AccountId, Input.NewAccountName);
                StatusMessage = "Kontonamnet har ändrats!";
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
