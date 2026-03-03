using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using OpenGate.Data.EFCore.Entities;

namespace OpenGate.UI.Pages.Account;

public sealed partial class RegisterModel(
    UserManager<OpenGateUser> userManager,
    SignInManager<OpenGateUser> signInManager,
    ILogger<RegisterModel> logger) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public sealed class InputModel
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "Nome")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O sobrenome é obrigatório.")]
        [StringLength(100)]
        [Display(Name = "Sobrenome")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [StringLength(100, MinimumLength = 8,
            ErrorMessage = "A senha deve ter no mínimo {2} caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar senha")]
        [Compare("Password", ErrorMessage = "As senhas não conferem.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
        => ReturnUrl = returnUrl ?? Url.Content("~/");

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new OpenGateUser
        {
            UserName = Input.Email,
            Email    = Input.Email,
            Profile  = new UserProfile
            {
                FirstName = Input.FirstName,
                LastName  = Input.LastName
            }
        };

        var result = await userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            Log.AccountCreated(logger, Input.Email);
            await signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "New account created: {Email}.")]
        public static partial void AccountCreated(ILogger logger, string email);
    }
}

