using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniManageSystem.Data;
using UniManageSystem.Models;
using UniManageSystem.Services;
using UniManageSystem.ViewModels;

namespace UniManageSystem.Controllers
{
    /// <summary>
    /// Handles the management of Assignments by Lecturers and homework submissions by Students.
    /// Includes strict role-based access checks.
    /// </summary>
    [Authorize]
    public class AssignmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly IWebHostEnvironment _webHostEnvironment; // Retaining strictly for `Download` streams
        private readonly UserManager<ApplicationUser> _userManager;

        public AssignmentController(ApplicationDbContext context, IFileService fileService, IWebHostEnvironment webHostEnvironment, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _fileService = fileService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
        }

        // GET: Assignment/CourseAssignments/5
        public async Task<IActionResult> CourseAssignments(int? courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var query = _context.Assignments.Include(a => a.Course).AsQueryable();

            if (courseId.HasValue)
            {
                query = query.Where(a => a.CourseId == courseId.Value);
                ViewBag.CourseId = courseId.Value;
            }
            else if (User.IsInRole("Lecturer"))
            {
                // If accessed globally from Dashboard, show all assignments for courses taught by this lecturer
                query = query.Where(a => a.Course.LecturerId == user.Id);
            }
            else if (User.IsInRole("Student"))
            {
                // If accessed by a student without courseId, show assignments for courses they are enrolled in
                var enrolledCourseIds = _context.Enrollments
                    .Where(e => e.StudentId == user.Id)
                    .Select(e => e.CourseId)
                    .ToList();
                query = query.Where(a => enrolledCourseIds.Contains(a.CourseId));
            }
            else if (User.IsInRole("Admin"))
            {
                // Admins can see all
            }
            else
            {
                return Forbid();
            }

            var assignments = await query.OrderByDescending(a => a.DueDate).AsNoTracking().ToListAsync();
            return View(assignments);
        }

        // GET: Assignment/Create/5 (CourseId)
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Create(int? courseId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var model = new AssignmentViewModel();

            if (courseId.HasValue)
            {
                var course = await _context.Courses.FindAsync(courseId.Value);
                if (course == null) return NotFound();
                
                if (User.IsInRole("Lecturer") && course.LecturerId != user.Id) return Forbid();
                model.CourseId = courseId.Value;
            }
            else
            {
                var courses = User.IsInRole("Admin") 
                    ? await _context.Courses.ToListAsync() 
                    : await _context.Courses.Where(c => c.LecturerId == user.Id).ToListAsync();
                    
                ViewBag.Courses = courses;
            }

            return View(model);
        }

        // POST: Assignment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Create(AssignmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var course = await _context.Courses.FindAsync(model.CourseId);
                    if (course == null) return NotFound();
                    
                    var user = await _userManager.GetUserAsync(User);
                    if (User.IsInRole("Lecturer") && course.LecturerId != user?.Id) return Forbid();

                    var assignment = new Assignment
                    {
                        CourseId = model.CourseId,
                        Title = System.Net.WebUtility.HtmlEncode(model.Title),
                        Description = System.Net.WebUtility.HtmlEncode(model.Description),
                        DueDate = model.DueDate,
                        MaxScore = model.MaxScore
                    };

                    _context.Add(assignment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Assignment created successfully.";
                    return RedirectToAction(nameof(CourseAssignments), new { courseId = model.CourseId });
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while creating the assignment.");
                }
            }
            return View(model);
        }

        // GET: Assignment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .Include(a => a.Submissions)
                .ThenInclude(s => s.Student)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignment == null) return NotFound();

            return View("AssignmentDetails", assignment);
        }

        // POST: Assignment/Submit
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Submit(SubmissionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid submission.";
                return RedirectToAction(nameof(Details), new { id = model.AssignmentId });
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Challenge();

                var assignment = await _context.Assignments.FindAsync(model.AssignmentId);
                if (assignment == null) return NotFound();

                // Validate file type and size (Example: Max 10MB, PDF/Word only)
                if (model.FileUpload.Length > 10 * 1024 * 1024 || model.FileUpload.Length == 0)
                {
                    TempData["ErrorMessage"] = "File size must be greater than 0 and less than 10MB.";
                    return RedirectToAction(nameof(Details), new { id = model.AssignmentId });
                }

                var ext = Path.GetExtension(model.FileUpload.FileName).ToLowerInvariant();
                if (ext != ".pdf" && ext != ".docx" && ext != ".txt")
                {
                    TempData["ErrorMessage"] = "Only PDF, DOCX, and TXT files are allowed.";
                    return RedirectToAction(nameof(Details), new { id = model.AssignmentId });
                }

                // Abstracted file upload logic
                string uniqueFileName = await _fileService.UploadFileAsync(model.FileUpload, "submissions");

                var submission = new Submission
                {
                    AssignmentId = model.AssignmentId,
                    StudentId = user.Id,
                    SubmissionDate = DateTime.UtcNow,
                    FilePath = uniqueFileName
                };

                // Check if there's already a submission and optionally replace it
                var existingSubmission = await _context.Submissions
                    .FirstOrDefaultAsync(s => s.AssignmentId == model.AssignmentId && s.StudentId == user.Id);
                    
                if (existingSubmission != null)
                {
                    // Delete old file safely first
                    _fileService.DeleteFile(existingSubmission.FilePath, "submissions");
                    
                    existingSubmission.FilePath = uniqueFileName;
                    existingSubmission.SubmissionDate = DateTime.UtcNow;
                    _context.Update(existingSubmission);
                }
                else
                {
                    _context.Submissions.Add(submission);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Assignment successfully uploaded!";
                return RedirectToAction(nameof(Details), new { id = model.AssignmentId });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while uploading your submission. Please try again.";
                return RedirectToAction(nameof(Details), new { id = model.AssignmentId });
            }
        }

        // GET: Assignment/Grade/5
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Grade(int? submissionId)
        {
            if (submissionId == null) return NotFound();

            var submission = await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(s => s.Id == submissionId);

            if (submission == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Lecturer") && submission.Assignment?.Course?.LecturerId != user?.Id) return Forbid();

            var model = new GradeViewModel
            {
                SubmissionId = submission.Id,
                Score = submission.Score ?? 0,
                Feedback = submission.Feedback
            };
            return View(model);
        }

        // POST: Assignment/Grade
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Grade(GradeViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var submission = await _context.Submissions
                        .Include(s => s.Assignment)
                        .ThenInclude(a => a.Course)
                        .FirstOrDefaultAsync(s => s.Id == model.SubmissionId);

                    if (submission == null) return NotFound();

                    var user = await _userManager.GetUserAsync(User);
                    if (User.IsInRole("Lecturer") && submission.Assignment?.Course?.LecturerId != user?.Id) return Forbid();

                    submission.Score = model.Score;
                    submission.Feedback = System.Net.WebUtility.HtmlEncode(model.Feedback);

                    _context.Update(submission);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Grade has been successfully recorded.";
                    return RedirectToAction(nameof(Details), new { id = submission.AssignmentId });
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while saving the grade.");
                }
            }
            return View(model);
        }

        // GET: Assignment/Download/5
        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .ThenInclude(a => a.Course)
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (submission == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Student") && submission.StudentId != user?.Id) return Forbid();
            if (User.IsInRole("Lecturer") && submission.Assignment?.Course?.LecturerId != user?.Id) return Forbid();

            var filepath = Path.Combine(_webHostEnvironment.WebRootPath, "submissions", submission.FilePath);
            if (!System.IO.File.Exists(filepath)) return NotFound();

            // Note: Secure this properly based on role so students only download their own and lecturers download all
            var memory = new MemoryStream();
            using (var stream = new FileStream(filepath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, GetContentType(filepath), Path.GetFileName(filepath));
        }

        private string GetContentType(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                _ => "application/octet-stream",
            };
        }

        // GET: Assignment/Edit/5
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Lecturer") && assignment.Course?.LecturerId != user?.Id) return Forbid();

            var model = new AssignmentViewModel
            {
                Id = assignment.Id,
                CourseId = assignment.CourseId,
                Title = assignment.Title,
                Description = assignment.Description,
                DueDate = assignment.DueDate,
                MaxScore = assignment.MaxScore
            };

            return View(model);
        }

        // POST: Assignment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Edit(int id, AssignmentViewModel model)
        {
            if (id != model.Id) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Lecturer") && assignment.Course?.LecturerId != user?.Id) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    assignment.Title = model.Title;
                    assignment.Description = model.Description;
                    assignment.DueDate = model.DueDate;
                    assignment.MaxScore = model.MaxScore;

                    _context.Update(assignment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Assignment updated successfully.";
                    return RedirectToAction(nameof(CourseAssignments), new { courseId = assignment.CourseId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssignmentExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(model);
        }

        // GET: Assignment/Delete/5
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assignment == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Lecturer") && assignment.Course?.LecturerId != user?.Id) return Forbid();

            return View(assignment);
        }

        // POST: Assignment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment != null)
            {
                var user = await _userManager.GetUserAsync(User);
                if (User.IsInRole("Lecturer") && assignment.Course?.LecturerId != user?.Id) return Forbid();

                _context.Assignments.Remove(assignment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Assignment deleted successfully.";
                return RedirectToAction(nameof(CourseAssignments), new { courseId = assignment.CourseId });
            }
            
            return RedirectToAction(nameof(CourseAssignments));
        }

        private bool AssignmentExists(int id)
        {
            return _context.Assignments.Any(e => e.Id == id);
        }
    }
}
