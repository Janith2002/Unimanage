using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using UniManageSystem.Data;
using UniManageSystem.Models;
using UniManageSystem.ViewModels;

namespace UniManageSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            var lecturers = await _userManager.GetUsersInRoleAsync("Lecturer");
            var topCourses = await _context.Courses
                .Select(c => new { c.Title, EnrollmentCount = c.Enrollments.Count })
                .OrderByDescending(c => c.EnrollmentCount)
                .Take(5)
                .ToListAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalStudents = students.Count,
                TotalLecturers = lecturers.Count,
                TotalCourses = await _context.Courses.CountAsync(),
                TotalEnrollments = await _context.Enrollments.CountAsync(),
                CourseNames = topCourses.Select(c => c.Title).ToList(),
                EnrollmentCounts = topCourses.Select(c => c.EnrollmentCount).ToList()
            };

            return View("~/Views/Dashboard/AdminDashboard.cshtml", vm);
        }

        public async Task<IActionResult> Lecturers(string searchString)
        {
            var lecturers = await _userManager.GetUsersInRoleAsync("Lecturer");
            ViewData["CurrentFilter"] = searchString;

            if (!string.IsNullOrEmpty(searchString))
            {
                lecturers = lecturers.Where(u => 
                    (u.FirstName != null && u.FirstName.Contains(searchString, System.StringComparison.OrdinalIgnoreCase)) ||
                    (u.LastName != null && u.LastName.Contains(searchString, System.StringComparison.OrdinalIgnoreCase)) ||
                    (u.Email != null && u.Email.Contains(searchString, System.StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            return View(lecturers);
        }

        public async Task<IActionResult> Students(string searchString)
        {
            var students = await _userManager.GetUsersInRoleAsync("Student");
            ViewData["CurrentFilter"] = searchString;

            if (!string.IsNullOrEmpty(searchString))
            {
                students = students.Where(u => 
                    (u.FirstName != null && u.FirstName.Contains(searchString, System.StringComparison.OrdinalIgnoreCase)) ||
                    (u.LastName != null && u.LastName.Contains(searchString, System.StringComparison.OrdinalIgnoreCase)) ||
                    (u.Email != null && u.Email.Contains(searchString, System.StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            return View(students);
        }
    }
}
