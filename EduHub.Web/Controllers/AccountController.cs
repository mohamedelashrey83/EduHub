using EduHub.Core.Entities;
using EduHub.Core.Enums;
using EduHub.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace EduHub.Web.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AccountController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {Email} logged in successfully.", model.Email);
            return RedirectToLocal(returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid login attempt. Check your email and password.");
        return View(model);
    }

    /// <summary>
    /// One-click quick login for evaluators to test Admin, Instructor, or Student personas effortlessly!
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDemoLogin(string role, string? returnUrl = null)
    {
        string email = role.ToLower() switch
        {
            "admin" => "admin@eduhub.com",
            "instructor" => "instructor@eduhub.com",
            _ => "student@eduhub.com"
        };

        var user = await _userManager.FindByEmailAsync(email);
        if (user != null)
        {
            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("Demo quick-login executed for persona: {Role} ({Email})", role, email);

            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Admin");
            if (role.Equals("Instructor", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "Instructor");

            return RedirectToLocal(returnUrl);
        }

        TempData["ErrorMessage"] = "Demo account not found. Please ensure database seeding has completed.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Headline = model.Headline ?? (model.IsInstructor ? "EduHub Instructor" : "Student"),
            EmailConfirmed = true,
            AvatarUrl = "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde?w=150"
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            var roleToAssign = model.IsInstructor ? UserRoles.Instructor : UserRoles.Student;
            if (!await _roleManager.RoleExistsAsync(roleToAssign))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleToAssign));
            }
            await _userManager.AddToRoleAsync(user, roleToAssign);

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User created a new account with password as {Role}.", roleToAssign);

            if (model.IsInstructor)
                return RedirectToAction("Index", "Instructor");

            return RedirectToAction("Index", "Home");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound("User not found.");

        var roles = await _userManager.GetRolesAsync(user);

        var model = new ProfileViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? "",
            Headline = user.Headline,
            Bio = user.Bio,
            WebsiteUrl = user.WebsiteUrl,
            LinkedInUrl = user.LinkedInUrl,
            AvatarUrl = user.AvatarUrl,
            TotalEarnings = user.TotalEarnings,
            Roles = roles.ToList()
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.Headline = model.Headline;
        user.Bio = model.Bio;
        user.WebsiteUrl = model.WebsiteUrl;
        user.LinkedInUrl = model.LinkedInUrl;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction(nameof(Profile));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}
