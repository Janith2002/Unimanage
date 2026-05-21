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
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Assignments)
                .Where(e => e.StudentId == user.Id)
                .ToListAsync();

            var courseNames = new System.Collections.Generic.List<string>();
            var grades = new System.Collections.Generic.List<decimal>();
            decimal totalGrades = 0;
            int gradedCourses = 0;

            foreach (var enr in enrollments)
            {
                if (enr.FinalGrade.HasValue)
                {
                    courseNames.Add(enr.Course?.Title ?? "Unknown");
                    grades.Add(enr.FinalGrade.Value);
                    totalGrades += enr.FinalGrade.Value;
                    gradedCourses++;
                }
            }

            var vm = new StudentDashboardViewModel
            {
                EnrolledCoursesCount = enrollments.Count,
                PendingAssignmentsCount = enrollments.SelectMany(e => e.Course?.Assignments ?? Enumerable.Empty<Assignment>()).Count(),
                AverageGrade = gradedCourses > 0 ? (totalGrades / gradedCourses) : 0,
                CourseNames = courseNames,
                Grades = grades
            };

            return View("~/Views/Dashboard/StudentDashboard.cshtml", vm);
        }
    }
}
