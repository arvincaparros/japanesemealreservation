using Microsoft.AspNetCore.Mvc;
using JapaneseMealReservation.Models;
using Microsoft.EntityFrameworkCore;
using JapaneseMealReservation.AppData;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using JapaneseMealReservation.ViewModels;

namespace JapaneseMealReservation.Controllers
{
    public class ReservationController : Controller
    {
        private readonly AppDbContext dbContext;

        public ReservationController(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public IActionResult MealReservation()
        {
            DateTime utcToday = DateTime.UtcNow.Date;

            // Get the start of the current week (Sunday)
            int diff = (7 + (utcToday.DayOfWeek - DayOfWeek.Sunday)) % 7;
            DateTime startOfWeek = utcToday.AddDays(-diff); // Sunday (start of week)

            // End of the week: Saturday 23:59:59.9999999
            DateTime endOfWeek = startOfWeek.AddDays(7).AddTicks(-1); // Includes entire Saturday

            // Query menus within the week
            var weeklyMenus = dbContext.Menus
                .Where(m => m.AvailabilityDate >= startOfWeek &&
                            m.AvailabilityDate <= endOfWeek &&
                            m.IsAvailable)
                .OrderBy(m => m.AvailabilityDate)
                .ToList();

            return View(weeklyMenus);

        }


        [HttpPost]
        public async Task<IActionResult> MealReservation(Order order)
        {
            if (!ModelState.IsValid)
            {
                // Return to the form view with validation errors
                return View(order);
            }

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Order placed successfully!";

            return RedirectToAction("Reservation");
        }

        [HttpGet]
        public IActionResult GetEmployeeInfo(string employeeId)
        {
            var employee = dbContext.Users
                .Where(e => e.EmployeeId == employeeId)
                .Select(e => new
                {
                    firstName = e.FirstName,
                    lastName = e.LastName,
                    section = e.Section
                })
                .FirstOrDefault();

            if (employee == null)
                return NotFound();

            return Json(employee);
        }

        //public IActionResult AdvanceOrdering()
        //{
        //    return View();
        //}


        [Authorize]
        public IActionResult AdvanceOrdering()
        {
            // Extract custom claims
            var employeeId = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
            var firstName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var lastName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
            var section = User.Claims.FirstOrDefault(c => c.Type == "Section")?.Value;
            var employeeType = User.Claims.FirstOrDefault(c => c.Type == "EmployeeType")?.Value;

            // Build anonymous object to pass via ViewBag
            ViewBag.EmployeeData = new
            {
                EmployeeId = employeeId,
                FirstName = firstName,
                LastName = lastName,
                Section = section,
                EmployeeType = employeeType
            };

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult SaveAdvanceOrder([FromBody] AdvanceOrder model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new { success = false, message = "Validation failed", errors });
            }


            // Use local time (PH timezone)
            var currentPHTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Singapore")); // UTC+8

            model.ReferenceNumber = $"ORD-{currentPHTime:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

            dbContext.AdvanceOrders.Add(model);
            dbContext.SaveChanges();

            return Json(new { success = true, message = "Reservation successful!", reference = model.ReferenceNumber });
        }

    }
}