using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenGate.Data.EFCore;
using OpenGate.Data.EFCore.Entities;

namespace OpenGate.UI.Pages.Admin.Users;

public sealed class EditModel(
    OpenGateDbContext db,
    UserManager<OpenGateUser> userManager,
    RoleManager<IdentityRole> roleManager) : AdminWritePageModel
{
    [BindProperty]
    public EditUserInput Input { get; set; } = new();

    public IReadOnlyList<string> AvailableRoles { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        AvailableRoles = await AdminUserManagementSupport.GetAvailableRolesAsync(roleManager, cancellationToken);

        var user = await db.Users
            .AsNoTracking()
            .Include(candidate => candidate.Profile)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        Input = new EditUserInput
        {
            Email = user.Email ?? string.Empty,
            UserName = user.UserName,
            FirstName = user.Profile?.FirstName,
            LastName = user.Profile?.LastName,
            DisplayName = user.Profile?.DisplayName,
            Locale = user.Profile?.Locale,
            TimeZone = user.Profile?.TimeZone,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            SelectedRoles = roles.OrderBy(role => role, StringComparer.Ordinal).ToList()
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id, CancellationToken cancellationToken)
    {
        AvailableRoles = await AdminUserManagementSupport.GetAvailableRolesAsync(roleManager, cancellationToken);

        var user = await db.Users
            .Include(candidate => candidate.Profile)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        ValidateSelectedRoles();
        await ValidateUniquenessAsync(user.Id);
        ValidateSelfProtection(user.Id);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var wasActive = user.IsActive;
        user.Email = Input.Email.Trim();
        user.UserName = string.IsNullOrWhiteSpace(Input.UserName) ? Input.Email.Trim() : Input.UserName.Trim();
        user.EmailConfirmed = Input.EmailConfirmed;
        user.IsActive = Input.IsActive;
        user.Profile = AdminUserManagementSupport.BuildOrUpdateProfile(
            user.Profile,
            Input.FirstName?.Trim(),
            Input.LastName?.Trim(),
            Input.DisplayName?.Trim(),
            Input.Locale?.Trim(),
            Input.TimeZone?.Trim());

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            AdminUserManagementSupport.AddIdentityErrors(ModelState, updateResult);
            return Page();
        }

        var rolesResult = await AdminUserManagementSupport.ReplaceRolesAsync(userManager, user, Input.SelectedRoles);
        if (!rolesResult.Succeeded)
        {
            AdminUserManagementSupport.AddIdentityErrors(ModelState, rolesResult);
            return Page();
        }

        if (wasActive && !Input.IsActive)
        {
            await AdminUserManagementSupport.RevokeActiveSessionsAsync(db, user.Id, cancellationToken);
        }

        db.AuditLogs.Add(AdminUserManagementSupport.CreateAuditLog(
            HttpContext,
            User,
            "Admin.UserUpdated",
            new { user.Id, user.Email, user.IsActive, Roles = AdminUserManagementSupport.NormalizeRoles(Input.SelectedRoles), Source = "AdminUi.Edit" }));

        await db.SaveChangesAsync(cancellationToken);

        StatusMessage = $"Usuário {user.Email} atualizado com sucesso.";
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

    private async Task ValidateUniquenessAsync(string userId)
    {
        var email = Input.Email.Trim();
        var existingByEmail = await userManager.FindByEmailAsync(email);
        if (existingByEmail is not null && !string.Equals(existingByEmail.Id, userId, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(Input.Email), "Já existe um usuário com este e-mail.");
        }

        var normalizedUserName = string.IsNullOrWhiteSpace(Input.UserName) ? email : Input.UserName.Trim();
        var existingByUserName = await userManager.FindByNameAsync(normalizedUserName);
        if (existingByUserName is not null && !string.Equals(existingByUserName.Id, userId, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(Input.UserName), "Já existe um usuário com este username.");
        }
    }

    private void ValidateSelfProtection(string userId)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.Equals(currentUserId, userId, StringComparison.Ordinal))
        {
            return;
        }

        if (!Input.IsActive)
        {
            ModelState.AddModelError(nameof(Input.IsActive), "Você não pode desativar a própria conta pelo Admin UI.");
        }

        if (!AdminUserManagementSupport.HasWriteRole(AdminUserManagementSupport.NormalizeRoles(Input.SelectedRoles)))
        {
            ModelState.AddModelError(nameof(Input.SelectedRoles), "Sua própria conta deve manter pelo menos um role de escrita administrativa.");
        }
    }
}

public sealed class EditUserInput
{
    [Required, EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Username")]
    public string? UserName { get; set; }

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