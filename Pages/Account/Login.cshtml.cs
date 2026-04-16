using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace StickyNotes.Pages.Account;

public class LoginModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;

    public LoginModel(SignInManager<IdentityUser> signInManager)
    {
        _signInManager = signInManager;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public bool RememberMe { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public IActionResult OnGet()
    {
        if (_signInManager.IsSignedIn(User))
        {
            return RedirectToPage("/Index");
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter both username and password.";
            return Page();
        }

        var result = await _signInManager.PasswordSignInAsync(
            Username.Trim(), Password, RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            return RedirectToPage("/Index");
        }

        ErrorMessage = "Invalid username or password.";
        return Page();
    }
}
