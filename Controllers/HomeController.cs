using System.Diagnostics;
using System.Security.Claims;
using JapaneseMealReservation.AppData;
using JapaneseMealReservation.Models;
using JapaneseMealReservation.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JapaneseMealReservation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext dbContext;

        public HomeController(ILogger<HomeController> logger, AppDbContext dbContext)
        {
            _logger = logger;
            this.dbContext = dbContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
         
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(Login model)
        {
            // Check if the submitted form model is valid
            if (!ModelState.IsValid)
            {
                return View(model); // Return the same view with validation messages
            }

            // Attempt to find a user in the database that matches the username and password
            var user = dbContext.Users
                .FirstOrDefault(user => user.EmployeeId == model.EmployeeId && user.Password == model.Password);

            // If no user found, display an error message and return to the login view
            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid username or password.";
                return View(model); 
            }

            // Create a list of claims (user identity data), here storing the user's first name
            var claims = new List<Claim>
            {
                new Claim("EmployeeId", user.EmployeeId ?? string.Empty),  // custom claim type string
                new Claim(ClaimTypes.GivenName, user.FirstName ?? ""),
                new Claim(ClaimTypes.Surname, user.LastName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("Section", user.Section ?? ""),  // custom claim type string
                new Claim(ClaimTypes.Role, user.UserRole ?? ""),
                new Claim("EmployeeType", user.EmployeeType ?? "")
            };

            // Create a ClaimsIdentity using the claims and specify the authentication scheme
            var identity = new ClaimsIdentity(claims, "MyCookieAuth");

            // Create a ClaimsPrincipal that holds the identity
            var principal = new ClaimsPrincipal(identity);

            // Sign in the user by issuing the authentication cookie
            await HttpContext.SignInAsync("MyCookieAuth", principal);

            // Redirect the authenticated user to the welcome page
            return RedirectToAction("Index", "Home");
        }

      

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login", "Home");
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //Set role
            model.UserRole = model.Section?.ToUpper() == "GA" ? "ADMIN" : "EMPLOYEE";

            // Set employee type if ID contains "BIPH-JP"
            if (!string.IsNullOrEmpty(model.EmployeeId) && model.EmployeeId.Contains("BIPH-JP", StringComparison.OrdinalIgnoreCase))
            {
                model.EmployeeType = "Expat";
            }
            else
            {
                model.EmployeeType = "Local"; 
            }

            dbContext.Users.Add(model);
            dbContext.SaveChanges();

            TempData["RegisterSuccess"] = true;

            return RedirectToAction("Register");

        }

        [HttpGet]
        public IActionResult GetEmployeeById(string id)
        {
            var user = dbContext.Users.FirstOrDefault(u => u.EmployeeId == id);

            if (user == null)
                return NotFound();

            return Json(new
            {
                firstName = user.FirstName,
                lastName = user.LastName,
                section = user.Section,
                email = user.Email
            });
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = dbContext.Users.FirstOrDefault(u => u.EmployeeId == model.EmployeeId);

            if (user == null)
            {
                // Associate error with EmployeeId field
                ModelState.AddModelError(nameof(model.EmployeeId), "Employee ID not found.");
                return View(model);
            }

            // Ideally: Hash the password before storing
            user.Password = model.Password;

            dbContext.SaveChanges();

            TempData["Message"] = "Password successfully updated.";
            return RedirectToAction("Login");
        }


        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
