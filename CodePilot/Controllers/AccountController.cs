using CodePilot.Areas.Identity.Data;
using CodePilot.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;
using System.Net;

namespace CodePilot.Controllers
{
    public class AccountController : Controller
    {
        SignInManager<ApplicationUser> _signInManager;
        UserManager<ApplicationUser> _userManager;
        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }
        public IActionResult RegLog(string message)
        {
            ViewData["alert"] = message;
            return View();
        }
        public IActionResult CheckUsername(string regUsername)
        {
            //var userByUsername = _userManager.FindByNameAsync(regUsername);
            var userByUsername = _userManager.Users.Count(x => x.UserName == regUsername);
            if (userByUsername > 0)
                return Json(false);
            else
                return Json(true);
        }
        public IActionResult CheckEmail(string regEmail)
        {
            //var userByEmail = _userManager.FindByEmailAsync(regEmail);
            var userByEmail = _userManager.Users.Count(x => x.Email == regEmail);
            if (userByEmail > 0)
                return Json(false);
            else
                return Json(true);
        }

        [HttpPost]
        public async Task<IActionResult> Register(AuthViewModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.regUsername,
                Email = model.regEmail,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("RegLog", new { message = "Registration successful! Now you can login. " });
            }
            else
            {
                return RedirectToAction("RegLog", new { message = "Registration failed! Please try again. " });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(AuthViewModel model)
        {
            ApplicationUser user = await _userManager.FindByNameAsync(model.logUsername);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(model.logUsername, model.Password, true, true);
                if (result.Succeeded)
                {
                    return RedirectToAction("Chat", "Chat");
                }
                else
                {
                    return RedirectToAction("RegLog", new { message = "Invalid credentials!" });
                }
            }
            else
            {
                return RedirectToAction("RegLog", new { message = "Invalid credentials!" });
            }
        }

        //[HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("RegLog", "Account", new { message = "You have been logged out!" });
        }
    }
}
