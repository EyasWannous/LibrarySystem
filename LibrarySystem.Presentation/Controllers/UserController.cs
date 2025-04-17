using LibrarySystem.BusinessLogic.Users;
using LibrarySystem.BusinessLogic.Users.DTOs;
using LibrarySystem.Presentation.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LibrarySystem.Presentation.Controllers;

public class UserController : Controller
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var response = await _userService.RegisterAsync(
            model.FirstName,
            model.LastName,
            model.Email,
            model.PhoneNumber,
            model.Password
        );

        if (!response.IsSuccess)
        {
            foreach (var error in response.Errors)
                ModelState.AddModelError(string.Empty, error);
            return View(model);
        }

        await SignInUser(response);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var response = await _userService.LoginByEmailAsync(model.Email, model.Password);

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View(model);
        }

        await SignInUser(response);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        Response.Cookies.Delete("LibrarySystem.AuthToken");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private async Task SignInUser(UserManagerResponse response)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, response.User!.Id.ToString()),
            new(ClaimTypes.Email, response.User.Email),
            new(ClaimTypes.Name, $"{response.User.FirstName} {response.User.LastName}")
        };

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role
        );

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = response.ExpiryDate
            });

        //await HttpContext.SignInAsync(
        //    JwtBearerDefaults.AuthenticationScheme,
        //    new ClaimsPrincipal(claimsIdentity),
        //    new AuthenticationProperties
        //    {
        //        IsPersistent = true,
        //        ExpiresUtc = response.ExpiryDate
        //    });
    }
}