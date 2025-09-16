using JapaneseMealReservation.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JapaneseMealReservation.AppData;
using JapaneseMealReservation.ViewModels;
using DocumentFormat.OpenXml.InkML;

namespace JapaneseMealReservation.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext dbContext;

        public AdminController(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        //[Authorize]
        //public IActionResult Dashboard()
        //{
        //    var menuItems = dbContext.Menus.ToList();
        //    return View(menuItems);
        //}

        [Authorize]
        public IActionResult Dashboard()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var model = new DashboardPageModel
            {
                Menus = dbContext.Menus.ToList(),
                Order = new Order(),
                TotalBentoToday = dbContext.OrderSummaryView
                    .Where(o => o.MenuType != null &&
                                o.MenuType.Trim().ToLower() == "bento" &&
                                o.ReservationDate >= today && 
                                o.ReservationDate < tomorrow)
                    .Count(),



                TotalMakiToday = dbContext.OrderSummaryView
                    .Where(o => o.MenuType != null &&
                                o.MenuType.Trim().ToLower() == "maki" &&
                                o.ReservationDate == today &&
                                o.ReservationDate < tomorrow)
                    .Count(),

                TotalCurryToday = dbContext.OrderSummaryView
                    .Where(o => o.MenuType != null &&
                                o.MenuType.Trim().ToLower() == "curry" &&
                                o.ReservationDate == today &&
                                o.ReservationDate < tomorrow)
                    .Count(),

                TotalNoodlesToday = dbContext.OrderSummaryView
                    .Where(o => o.MenuType != null &&
                                o.MenuType.Trim().ToLower() == "noodles" &&
                                o.ReservationDate == today &&
                                o.ReservationDate < tomorrow)
                    .Count(),

                TotalBreakfastToday = dbContext.OrderSummaryView
                    .Where(o => o.MenuType != null &&
                                o.MenuType.Trim().ToLower() == "breakfast" &&
                                o.ReservationDate == today &&
                                o.ReservationDate < tomorrow)
                    .Count()
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Dashboard(Login model)
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
                return RedirectToAction("Index", "Home"); // Stay on the login page
            }

            // Create a list of claims (user identity data), here storing the user's first name
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FirstName ?? string.Empty)
            };

            // Create a ClaimsIdentity using the claims and specify the authentication scheme
            var identity = new ClaimsIdentity(claims, "MyCookieAuth");

            // Create a ClaimsPrincipal that holds the identity
            var principal = new ClaimsPrincipal(identity);

            // Sign in the user by issuing the authentication cookie
            await HttpContext.SignInAsync("MyCookieAuth", principal);

            // Redirect the authenticated user to the Admin page
            return RedirectToAction("Dashboard", "Admin");
        }

        //public IActionResult MenuList()
        //{
        //    var menus = dbContext.Menus.ToList();
        //    return View(menus); // Passes the list to the view
        //}

        public IActionResult FilterMenu(DateTime? startDate, DateTime? endDate)
        {
            var query = dbContext.Menus.AsQueryable();

            // If no filter is provided, default to the current week
            if (!startDate.HasValue || !endDate.HasValue)
            {
                // Calculate start (Monday) and end (Sunday) of current week
                var today = DateTime.Today;
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                var weekStart = today.AddDays(-1 * diff).Date; // Monday
                var weekEnd = weekStart.AddDays(6).Date;       // Sunday

                startDate = weekStart;
                endDate = weekEnd;
            }

            // Apply filter
            query = query.Where(m => m.AvailabilityDate >= startDate.Value &&
                                     m.AvailabilityDate <= endDate.Value);

            var menus = query
                .OrderBy(m => m.AvailabilityDate)
                .ToList();

            var model = new DashboardPageModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Menus = menus
            };

            return View("Dashboard", model);
        }



        [HttpGet]
        public IActionResult ExpatMonthlyDeduction()
        {
            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var expatOrders = dbContext.OrderSummaryView
                .Where(o => o.CustomerType == "Expat"
                            && o.ReservationDate >= monthStart
                            && o.ReservationDate < monthEnd
                            && o.Status == "Completed")
                .ToList();

            var grouped = expatOrders
                .GroupBy(o => new { o.FirstName, o.LastName })
                .Select(g => new ExpatMonthlyDeduction
                {
                    Name = g.Key.FirstName + " " + g.Key.LastName,
                    ExpatBento = g.Where(x => x.MenuType == "Bento").Sum(x => x.Price * x.Quantity),
                    ExpatCurryRice = g.Where(x => x.MenuType == "Curry").Sum(x => x.Price * x.Quantity),
                    ExpatNoodles = g.Where(x => x.MenuType == "Noodles").Sum(x => x.Price * x.Quantity),
                    MakiRoll = g.Where(x => x.MenuType == "Maki").Sum(x => x.Price * x.Quantity),
                    Breakfast = g.Where(x => x.MenuType == "Breakfast").Sum(x => x.Price * x.Quantity)
                }).ToList();

            return View(grouped);
        }


        // GET: /Admin/Search?query=abc
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            var users = await dbContext.Users
                .Where(u => u.FirstName.Contains(query) || u.LastName.Contains(query) || u.Email.Contains(query))
                .Select(u => new {
                    id = u.UserId,
                    adid = u.EmployeeId,   // or your ADID field
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    email = u.Email,
                    role = u.UserRole // Or use IdentityUserRole if you're using Identity
                })
                .ToListAsync();

            return Json(users);
        }

        // POST: /Admin/UpdateRole/5
        [HttpPost]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] RoleUpdateRequest request)
        {
            var user = await dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.UserRole = request.Role; // "EMPLOYEE" or "ADMIN"
            await dbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        public class RoleUpdateRequest
        {
            public string Role { get; set; }
        }

    }
}
