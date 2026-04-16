using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StickyNotes.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;

    public RegisterModel(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public List<string> Errors { get; set; } = new();

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
            Errors.Add("Username and password are required.");
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            Errors.Add("Passwords do not match.");
            return Page();
        }

        var user = new IdentityUser { UserName = Username.Trim() };
        var result = await _userManager.CreateAsync(user, Password);

        if (result.Succeeded)
        {
            await _signInManager.SignInAsync(user, isPersistent: true);
            return RedirectToPage("/Index");
        }

        foreach (var error in result.Errors)
        {
            Errors.Add(error.Description);
        }
        return Page();
    }
}
