using LibrarySystem.BusinessLogic.Users;
using LibrarySystem.BusinessLogic.Users.DTOs;
using LibrarySystem.Presentation.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
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
        var token = response.JwtToken;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var claims = jwtToken.Claims.ToList();

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = response.ExpiryDate
            });

        HttpContext.Response.Cookies.Append("LibrarySystem.AuthToken", response.JwtToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = response.ExpiryDate
        });
    }
}