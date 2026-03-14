using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenGate.Data.EFCore;
using OpenGate.Data.EFCore.Entities;

namespace OpenGate.UI.Pages.Admin.Users;

public sealed class CreateModel(
    OpenGateDbContext db,
    UserManager<OpenGateUser> userManager,
    RoleManager<IdentityRole> roleManager) : AdminWritePageModel
{
    [BindProperty]
    public CreateUserInput Input { get; set; } = new();

    public IReadOnlyList<string> AvailableRoles { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
        => AvailableRoles = await AdminUserManagementSupport.GetAvailableRolesAsync(roleManager, cancellationToken);

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        AvailableRoles = await AdminUserManagementSupport.GetAvailableRolesAsync(roleManager, cancellationToken);

        ValidateSelectedRoles();
        await ValidateUniquenessAsync();

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new OpenGateUser
        {
            Email = Input.Email.Trim(),
            UserName = string.IsNullOrWhiteSpace(Input.UserName) ? Input.Email.Trim() : Input.UserName.Trim(),
            EmailConfirmed = Input.EmailConfirmed,
            IsActive = Input.IsActive,
            Profile = AdminUserManagementSupport.BuildOrUpdateProfile(
                null,
                Input.FirstName?.Trim(),
                Input.LastName?.Trim(),
                Input.DisplayName?.Trim(),
                Input.Locale?.Trim(),
                Input.TimeZone?.Trim())
        };

        var createResult = await userManager.CreateAsync(user, Input.Password);
        if (!createResult.Succeeded)
        {
            AdminUserManagementSupport.AddIdentityErrors(ModelState, createResult);
            return Page();
        }

        var rolesResult = await userManager.AddToRolesAsync(user, AdminUserManagementSupport.NormalizeRoles(Input.SelectedRoles));
        if (!rolesResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            AdminUserManagementSupport.AddIdentityErrors(ModelState, rolesResult);
            return Page();
        }

        db.AuditLogs.Add(AdminUserManagementSupport.CreateAuditLog(
            HttpContext,
            User,
            "Admin.UserCreated",
            new { user.Id, user.Email, Roles = AdminUserManagementSupport.NormalizeRoles(Input.SelectedRoles), Source = "AdminUi.Create" }));

        await db.SaveChangesAsync(cancellationToken);

        StatusMessage = $"Usuário {user.Email} criado com sucesso.";
        return RedirectToPage("/Admin/Users");
    }

    private void ValidateSelectedRoles()
    {
        var invalidRoles = AdminUserManagementSupport.NormalizeRoles(Input.SelectedRoles)
            .Except(AvailableRoles, StringComparer.Ordinal)
            .ToArray();

        if (invalidRoles.Length > 0)
        {
            ModelState.AddModelError(nameof(Input.SelectedRoles), $"Roles inválidos: {string.Join(", ", invalidRoles)}");
        }
    }

    private async Task ValidateUniquenessAsync()
    {
        var existingByEmail = await userManager.FindByEmailAsync(Input.Email.Trim());
        if (existingByEmail is not null)
        {
            ModelState.AddModelError(nameof(Input.Email), "Já existe um usuário com este e-mail.");
        }

        var normalizedUserName = string.IsNullOrWhiteSpace(Input.UserName) ? Input.Email.Trim() : Input.UserName.Trim();
        var existingByUserName = await userManager.FindByNameAsync(normalizedUserName);
        if (existingByUserName is not null)
        {
            ModelState.AddModelError(nameof(Input.UserName), "Já existe um usuário com este username.");
        }
    }
}

public sealed class CreateUserInput
{
    [Required, EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Username")]
    public string? UserName { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Primeiro nome")]
    public string? FirstName { get; set; }

    [Display(Name = "Sobrenome")]
    public string? LastName { get; set; }

    [Display(Name = "Display name")]
    public string? DisplayName { get; set; }

    [Display(Name = "Locale")]
    public string? Locale { get; set; }

    [Display(Name = "Time zone")]
    public string? TimeZone { get; set; }

    public bool IsActive { get; set; } = true;
    public bool EmailConfirmed { get; set; }
    public List<string> SelectedRoles { get; set; } = [];
}