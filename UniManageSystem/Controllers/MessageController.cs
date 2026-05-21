using System;
using System.Collections.Generic;
using System.Linq;
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
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessageController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Message/Index
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var users = await _userManager.Users
                .Where(u => u.Id != user.Id)
                .Select(u => new ContactViewModel
                {
                    UserId = u.Id,
                    FullName = string.IsNullOrEmpty(u.FirstName) ? u.Email : $"{u.FirstName} {u.LastName}",
                    UnreadCount = _context.Messages.Count(m => m.ReceiverId == user.Id && m.SenderId == u.Id && !m.IsRead)
                })
                .ToListAsync();

            return View(users);
        }

        // GET: Message/Chat/{id}
        public async Task<IActionResult> Chat(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var chatUser = await _userManager.FindByIdAsync(id);
            if (user == null || chatUser == null) return NotFound();

            var messages = await _context.Messages
                .Where(m => (m.SenderId == user.Id && m.ReceiverId == id) || 
                            (m.SenderId == id && m.ReceiverId == user.Id))
                .OrderBy(m => m.SentDate)
                .ToListAsync();

            // Mark unread as read
            var unread = messages.Where(m => m.ReceiverId == user.Id && !m.IsRead).ToList();
            if (unread.Any())
            {
                unread.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();
            }

            var vm = new ChatViewModel
            {
                ChatUserId = chatUser.Id,
                ChatUserName = string.IsNullOrEmpty(chatUser.FirstName) ? chatUser.Email! : $"{chatUser.FirstName} {chatUser.LastName}",
                Messages = messages.Select(m => new MessageViewModel
                {
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Content = m.Content,
                    SentDate = m.SentDate.ToString("g"),
                    IsMine = m.SenderId == user.Id
                }).ToList()
            };

            return View(vm);
        }

        // GET: Message/Inbox
        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ReceiverId == user.Id)
                .OrderByDescending(m => m.SentDate)
                .ToListAsync();

            return View(messages);
        }

        // GET: Message/Sent
        public async Task<IActionResult> Sent()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var messages = await _context.Messages
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == user.Id)
                .OrderByDescending(m => m.SentDate)
                .ToListAsync();

            return View(messages);
        }

        // GET: Message/Read/5
        public async Task<IActionResult> Read(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null) return NotFound();

            if (message.ReceiverId == user.Id && !message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        // GET: Message/Send
        [Authorize(Roles = "Student,Lecturer,Admin")]
        public async Task<IActionResult> Send()
        {
            var user = await _userManager.GetUserAsync(User);
            var users = new List<ApplicationUser>();

            if (User.IsInRole("Student"))
            {
                var enrolledCourseIds = await _context.Enrollments
                    .Where(e => e.StudentId == user.Id)
                    .Select(e => e.CourseId)
                    .ToListAsync();
                
                users = await _context.Courses
                    .Where(c => enrolledCourseIds.Contains(c.Id) && c.Lecturer != null)
                    .Select(c => c.Lecturer)
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                users = await _userManager.Users.Where(u => u.Id != user.Id).ToListAsync();
            }

            ViewBag.Users = users;
            return View();
        }

        // POST: Message/SendMessage
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Student,Lecturer,Admin")]
        public async Task<IActionResult> SendMessage(string receiverId, string subject, string content)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(receiverId)) 
                {
                    ModelState.AddModelError("", "Content and receiver are required.");
                    return RedirectToAction(nameof(Send));
                }

                // Basic HTML sanitization or encoding for XSS protection before storage
                content = System.Net.WebUtility.HtmlEncode(content);
                subject = System.Net.WebUtility.HtmlEncode(subject ?? "No Subject");

                var message = new Message
                {
                    SenderId = user.Id,
                    ReceiverId = receiverId,
                    Subject = subject,
                    Content = content,
                    SentDate = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Message sent successfully!";
                return RedirectToAction(nameof(Sent));
            }
            catch (Exception ex)
            {
                // In a real application, log the exception (ex) here
                TempData["ErrorMessage"] = "An error occurred while sending the message.";
                return RedirectToAction(nameof(Send));
            }
        }
    }
}