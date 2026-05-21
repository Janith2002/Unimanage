using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UniManageSystem.Data;
using UniManageSystem.Models;
using UniManageSystem.Services;
using UniManageSystem.ViewModels;
using UniManageSystem.Helpers;

namespace UniManageSystem.Controllers
{
    [Authorize]
    public class CourseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CourseController(ApplicationDbContext context, IFileService fileService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _fileService = fileService;
            _userManager = userManager;
        }

        // GET: Course
        public async Task<IActionResult> Index(
            string currentFilter,
            string searchString,
            string lecturerFilter,
            int? pageNumber)
        {
            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentLecturerFilter"] = lecturerFilter;

            var user = await _userManager.GetUserAsync(User);
            var courses = _context.Courses.Include(c => c.Lecturer).Include(c => c.Enrollments).AsQueryable();

            // Populate Dropdown for non-lecturers
            if (!User.IsInRole("Lecturer"))
            {
                var allLecturers = await _userManager.GetUsersInRoleAsync("Lecturer");
                var lecturerList = allLecturers.Select(l => new 
                {
                    Id = l.Id,
                    DisplayName = string.IsNullOrWhiteSpace(l.FirstName) && string.IsNullOrWhiteSpace(l.LastName) ? l.Email : $"{l.FirstName} {l.LastName} ({l.Email})"
                });
                ViewBag.Lecturers = new SelectList(lecturerList, "Id", "DisplayName", lecturerFilter);
            }

            // Forced strict role limit
            if (User.IsInRole("Lecturer"))
            {
                courses = courses.Where(c => c.LecturerId == user.Id);
            }
            // Optional admin/student filter
            else if (!string.IsNullOrEmpty(lecturerFilter))
            {
                courses = courses.Where(c => c.LecturerId == lecturerFilter);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                courses = courses.Where(c => c.Title.Contains(searchString) 
                                          || c.Description.Contains(searchString));
            }

            // Number of records per page
            int pageSize = 15; // Increased to show more courses per page
            return View(await PaginatedList<Course>.CreateAsync(courses.AsNoTracking(), pageNumber ?? 1, pageSize));
        }

        // GET: Course/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Lecturer)
                .Include(c => c.Enrollments)
                .Include(c => c.Assignments)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (course == null) return NotFound();

            return View(course);
        }

        // GET: Course/Create
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            await PopulateLecturersDropDownListAsync();
            return View(new CourseViewModel());
        }

        // POST: Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CourseViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string? uniqueFileName = null;
                    if (model.ImageUpload != null)
                    {
                        uniqueFileName = await _fileService.UploadFileAsync(model.ImageUpload, "images/courses");
                    }

                    // Protect against potential XSS by encoding text fields if they are ever rendered raw
                    var course = new Course
                    {
                        Title = System.Net.WebUtility.HtmlEncode(model.Title),
                        Description = System.Net.WebUtility.HtmlEncode(model.Description),
                        Capacity = model.Capacity,
                        LecturerId = model.LecturerId,
                        ImageUrl = uniqueFileName
                    };

                    _context.Add(course);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Course created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An error occurred while creating the course. Please try again.");
                }
            }
            await PopulateLecturersDropDownListAsync(model.LecturerId);
            return View(model);
        }

        // GET: Course/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            var model = new CourseViewModel
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Capacity = course.Capacity,
                LecturerId = course.LecturerId,
                ExistingImageUrl = course.ImageUrl
            };

            await PopulateLecturersDropDownListAsync(course.LecturerId);
            return View(model);
        }

        // POST: Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, CourseViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var course = await _context.Courses.FindAsync(id);
                    if (course == null) return NotFound();

                    course.Title = System.Net.WebUtility.HtmlEncode(model.Title);
                    course.Description = System.Net.WebUtility.HtmlEncode(model.Description);
                    course.Capacity = model.Capacity;
                    course.LecturerId = model.LecturerId;

                    if (model.ImageUpload != null)
                    {
                        if (!string.IsNullOrEmpty(course.ImageUrl))
                        {
                            _fileService.DeleteFile(course.ImageUrl, "images/courses");
                        }
                        course.ImageUrl = await _fileService.UploadFileAsync(model.ImageUpload, "images/courses");
                    }

                    _context.Update(course);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Course updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(model.Id)) return NotFound();
                    else throw;
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An unexpected error occurred while updating the course.");
                }
            }
            await PopulateLecturersDropDownListAsync(model.LecturerId);
            return View(model);
        }

        // GET: Course/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (course == null) return NotFound();

            return View(course);
        }

        // POST: Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course != null)
                {
                    if (!string.IsNullOrEmpty(course.ImageUrl))
                    {
                        _fileService.DeleteFile(course.ImageUrl, "images/courses");
                    }
                    _context.Courses.Remove(course);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Course deleted successfully.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the course.";
                return RedirectToAction(nameof(Index));
            }
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.Id == id);
        }

        private async Task PopulateLecturersDropDownListAsync(object? selectedLecturer = null)
        {
            var lecturers = await _userManager.GetUsersInRoleAsync("Lecturer");
            var lecturerList = lecturers.Select(l => new 
            {
                Id = l.Id,
                DisplayName = string.IsNullOrWhiteSpace(l.FirstName) && string.IsNullOrWhiteSpace(l.LastName) ? l.Email : $"{l.FirstName} {l.LastName} ({l.Email})"
            });
            ViewBag.LecturerId = new SelectList(lecturerList, "Id", "DisplayName", selectedLecturer);
        }
    }
}
