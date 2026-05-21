using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniManageSystem.Data;
using UniManageSystem.Models;

namespace UniManageSystem.Controllers
{
    [Authorize]
    public class EnrollmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EnrollmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Enrollment
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Student)
                .OrderByDescending(e => e.EnrollmentDate)
                .AsNoTracking()
                .ToListAsync();

            return View(enrollments);
        }

        // GET: Enrollment/PendingRequests
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PendingRequests()
        {
            var requests = await _context.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Student)
                .Where(e => e.Status == EnrollmentStatus.Pending)
                .OrderBy(e => e.EnrollmentDate)
                .ToListAsync();

            ViewBag.Courses = await _context.Courses.ToListAsync();
            ViewBag.Students = await _userManager.GetUsersInRoleAsync("Student");

            return View(requests);
        }

        // POST: Enrollment/ApproveEnrollment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> ApproveEnrollment(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();
            
            bool isAdmin = User.IsInRole("Admin");

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Enrollments)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "Enrollment request not found.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }

            if (!isAdmin && enrollment.Course.LecturerId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "Unauthorized to approve this course request.";
                return RedirectToAction("Index", "Home");
            }

            var approvedCount = enrollment.Course.Enrollments.Count(e => e.Status == EnrollmentStatus.Approved);
            if (enrollment.Course.Capacity > 0 && approvedCount >= enrollment.Course.Capacity)
            {
                TempData["ErrorMessage"] = $"Course {enrollment.Course.Title} is at full capacity.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }

            enrollment.Status = EnrollmentStatus.Approved;
            enrollment.ApprovedById = currentUser.Id;
            enrollment.ApprovedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Enrollment approved successfully.";
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        // POST: Enrollment/RejectEnrollment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> RejectEnrollment(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();
            
            bool isAdmin = User.IsInRole("Admin");

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "Enrollment request not found.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }

            if (!isAdmin && enrollment.Course.LecturerId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "Unauthorized to reject this course request.";
                return RedirectToAction("Index", "Home");
            }

            enrollment.Status = EnrollmentStatus.Rejected;
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Enrollment rejected.";
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        // GET: Enrollment/CourseRoster/5
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> CourseRoster(int courseId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();
            
            bool isAdmin = User.IsInRole("Admin");

            var course = await _context.Courses
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null) return NotFound();

            if (!isAdmin && course.LecturerId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You do not have permission to view the roster for this course.";
                return RedirectToAction("Index", "Course");
            }

            return View(course);
        }

        // POST: Enrollment/RemoveStudent/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> RemoveStudent(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();
            
            bool isAdmin = User.IsInRole("Admin");

            var enrollment = await _context.Enrollments
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "Enrollment record not found.";
                return Redirect(Request.Headers["Referer"].ToString() ?? "/");
            }

            if (!isAdmin && enrollment.Course.LecturerId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "Unauthorized to remove student from this course.";
                return RedirectToAction("Index", "Home");
            }

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student removed from course.";
            return Redirect(Request.Headers["Referer"].ToString() ?? "/");
        }

        // POST: Enrollment/AssignStudentToCourse
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignStudentToCourse(string studentId, int courseId)
        {
            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null) return Challenge();

            var exists = await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
            if (exists)
            {
                TempData["ErrorMessage"] = "Student is already enrolled or has requested enrollment in this course.";
                return RedirectToAction(nameof(PendingRequests));
            }

            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                EnrollmentDate = DateTime.UtcNow,
                Status = EnrollmentStatus.Approved,
                ApprovedById = adminUser.Id,
                ApprovedDate = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Student manually assigned to course.";
            return RedirectToAction(nameof(PendingRequests));
        }

        // GET: Enrollment/MyCourses
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyCourses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var enrollments = await _context.Enrollments
                .Include(e => e.Course)
                .ThenInclude(c => c.Lecturer)
                .Where(e => e.StudentId == user.Id)
                .OrderByDescending(e => e.EnrollmentDate)
                .AsNoTracking()
                .ToListAsync();

            return View(enrollments);
        }

        // POST: Enrollment/RequestEnrollment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> RequestEnrollment(int courseId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                var course = await _context.Courses
                    .Include(c => c.Enrollments)
                    .FirstOrDefaultAsync(c => c.Id == courseId);

                if (course == null)
                {
                    TempData["ErrorMessage"] = "Course not found.";
                    return RedirectToAction("Index", "Course");
                }

                // Validation: Check for duplicate enrollments or pending requests
                bool isAlreadyEnrolled = course.Enrollments.Any(e => e.StudentId == user.Id);
                if (isAlreadyEnrolled)
                {
                    TempData["WarningMessage"] = "You have already requested or enrolled in this course.";
                    return RedirectToAction("Index", "Course");
                }

                // Validation: Enforce enrollment limits/capacity only for Approved
                var approvedCount = course.Enrollments.Count(e => e.Status == EnrollmentStatus.Approved);
                if (course.Capacity > 0 && approvedCount >= course.Capacity)
                {
                    TempData["ErrorMessage"] = "This course has reached its maximum capacity. Enrollment closed.";
                    return RedirectToAction("Index", "Course");
                }

                var enrollment = new Enrollment
                {
                    CourseId = courseId,
                    StudentId = user.Id,
                    EnrollmentDate = DateTime.UtcNow,
                    Status = EnrollmentStatus.Pending
                };

                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Enrollment requested for {course.Title}. Please wait for approval.";
                return RedirectToAction(nameof(MyCourses));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred during enrollment process. Please try again.";
                return RedirectToAction("Index", "Course");
            }
        }

        // POST: Enrollment/Drop/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Drop(int enrollmentId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                var enrollment = await _context.Enrollments
                    .Include(e => e.Course)
                    .FirstOrDefaultAsync(e => e.Id == enrollmentId && e.StudentId == user.Id);

                if (enrollment == null)
                {
                    TempData["ErrorMessage"] = "Enrollment record not found or access denied.";
                    return RedirectToAction(nameof(MyCourses));
                }

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"You have successfully dropped {enrollment.Course?.Title}.";
                return RedirectToAction(nameof(MyCourses));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while dropping the course. Please try again later.";
                return RedirectToAction(nameof(MyCourses));
            }
        }
    }
}
