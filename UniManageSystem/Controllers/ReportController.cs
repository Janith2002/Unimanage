using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniManageSystem.Data;
using UniManageSystem.Models;
using UniManageSystem.ViewModels;

namespace UniManageSystem.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Admin Reports
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminReport()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalCourses = await _context.Courses.CountAsync();
            var totalEnrollments = await _context.Enrollments.CountAsync();

            var coursePopularity = await _context.Courses
                .AsNoTracking()
                .Select(c => new CoursePopularityDto
                {
                    CourseName = c.Title,
                    Enrollments = c.Enrollments.Count,
                    Capacity = c.Capacity
                })
                .OrderByDescending(c => c.Enrollments)
                .ToListAsync();

            var vm = new AdminReportViewModel
            {
                TotalUsers = totalUsers,
                TotalCourses = totalCourses,
                TotalEnrollments = totalEnrollments,
                CoursePopularity = coursePopularity
            };

            return View(vm);
        }

        // Lecturer Reports
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> LecturerReport()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var courses = await _context.Courses
                .Include(c => c.Enrollments).ThenInclude(e => e.Student)
                .Include(c => c.Assignments).ThenInclude(a => a.Submissions)
                .Where(c => c.LecturerId == user.Id)
                .AsNoTracking()
                .ToListAsync();

            var vm = new LecturerReportViewModel();

            foreach (var course in courses)
            {
                var coursePerf = new LecturerCoursePerformanceDto
                {
                    CourseName = course.Title,
                    EnrolledStudents = course.Enrollments.Count,
                    AverageCourseGrade = course.Enrollments.Where(e => e.FinalGrade.HasValue).Average(e => e.FinalGrade) ?? 0,
                    StudentGrades = course.Enrollments.Select(e => new StudentGradeDto
                    {
                        StudentName = e.Student?.Email ?? "Unknown",
                        FinalGrade = e.FinalGrade
                    }).ToList(),
                    AssignmentAverages = course.Assignments.Select(a => new AssignmentAverageDto
                    {
                        AssignmentTitle = a.Title,
                        MaxScore = a.MaxScore,
                        AverageScore = a.Submissions.Where(s => s.Score.HasValue).Average(s => s.Score) ?? 0
                    }).ToList()
                };
                
                vm.CoursePerformances.Add(coursePerf);
            }

            return View(vm);
        }

        // Student Reports
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> StudentReport()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Where(e => e.StudentId == user.Id)
                .AsNoTracking()
                .ToListAsync();

            var submissions = await _context.Submissions
                .Include(s => s.Assignment).ThenInclude(a => a.Course)
                .Where(s => s.StudentId == user.Id && s.Score.HasValue)
                .OrderBy(s => s.SubmissionDate)
                .AsNoTracking()
                .ToListAsync();

            var gradedEnrollments = enrollments.Where(e => e.FinalGrade.HasValue).ToList();
            var overallAverage = gradedEnrollments.Any() ? gradedEnrollments.Average(e => e.FinalGrade!.Value) : 0;

            var vm = new StudentReportViewModel
            {
                StudentName = user.Email ?? "Student",
                OverallAverage = overallAverage,
                CourseGrades = enrollments.Select(e => new StudentCourseGradeDto
                {
                    CourseName = e.Course?.Title ?? "Unknown",
                    FinalGrade = e.FinalGrade
                }).ToList(),
                RecentSubmissions = submissions.Select(s => new StudentSubmissionDto
                {
                    AssignmentTitle = s.Assignment?.Title ?? "Unknown",
                    CourseName = s.Assignment?.Course?.Title ?? "Unknown",
                    Score = s.Score,
                    MaxScore = s.Assignment?.MaxScore ?? 100,
                    SubmissionDate = s.SubmissionDate
                }).ToList()
            };

            return View(vm);
        }

        // Export Actions
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> ExportAdminCsv()
        {
            var coursePopularity = await _context.Courses
                .AsNoTracking()
                .Select(c => new 
                {
                    c.Title,
                    Enrollments = c.Enrollments.Count,
                    c.Capacity
                })
                .OrderByDescending(c => c.Enrollments)
                .ToListAsync();

            var builder = new StringBuilder();
            builder.AppendLine("Course Name,Enrollments,Capacity,Fill Percentage");

            foreach (var course in coursePopularity)
            {
                var fill = course.Capacity > 0 ? ((decimal)course.Enrollments / course.Capacity) * 100 : 0;
                builder.AppendLine($"\"{course.Title}\",{course.Enrollments},{course.Capacity},{fill:F2}%");
            }

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "AdminReport.csv");
        }
    }
}