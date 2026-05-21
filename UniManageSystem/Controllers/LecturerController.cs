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
    [Authorize(Roles = "Lecturer")]
    public class LecturerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LecturerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var courses = await _context.Courses
                .Include(c => c.Enrollments)
                .Include(c => c.Assignments)
                .ThenInclude(a => a.Submissions)
                .Where(c => c.LecturerId == user.Id)
                .ToListAsync();

            var assignmentsStats = courses.SelectMany(c => c.Assignments)
                .Select(a => new { a.Title, SubCount = a.Submissions.Count })
                .Take(5).ToList();

            var vm = new LecturerDashboardViewModel
            {
                TotalCoursesTaught = courses.Count,
                TotalStudentsEnrolled = courses.Sum(c => c.Enrollments.Count),
                PendingSubmissionsToGrade = courses.SelectMany(c => c.Assignments).SelectMany(a => a.Submissions).Count(s => !s.Score.HasValue),
                AssignmentNames = assignmentsStats.Select(a => a.Title).ToList(),
                SubmissionCounts = assignmentsStats.Select(a => a.SubCount).ToList(),
                RecentCourses = courses.OrderByDescending(c => c.Id).Take(5).ToList()
            };

            return View("~/Views/Dashboard/LecturerDashboard.cshtml", vm);
        }
    }
}
