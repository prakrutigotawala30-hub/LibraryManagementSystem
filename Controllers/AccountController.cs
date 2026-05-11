using LibraryManagementSystem.Models;
using LibraryManagementSystem.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        private const string ADMIN_KEY = "LIBRARY@2026";
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // LOGIN
        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                false,
                false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                // 👇 ADMIN
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Home");
                }

                // 👇 USER
                if (await _userManager.IsInRoleAsync(user, "User"))
                {
                    return RedirectToAction("Index", "UserView");
                }

                await _signInManager.SignOutAsync();
                return RedirectToAction("AccessDenied");
            }

            ModelState.AddModelError("", "Invalid login attempt");
            return View(model);
        }

        // REGISTER
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // 👇 ADMIN CHECK LOGIC
                if (model.IsAdmin)
                {
                    if (model.PrivateKey == ADMIN_KEY)
                    {
                        await _userManager.AddToRoleAsync(user, "Admin");
                    }
                    else
                    {
                        // delete user if key wrong
                        await _userManager.DeleteAsync(user);

                        TempData["Error"] = "Invalid Admin Key!";
                        return View(model);
                    }
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }

                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // LOGOUT
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // ACCESS DENIED
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}