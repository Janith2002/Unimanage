using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManageSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        
        // Navigation properties
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Course> TaughtCourses { get; set; } = new List<Course>();
        
        [InverseProperty(nameof(Message.Sender))]
        public ICollection<Message> SentMessages { get; set; } = new List<Message>();
        
        [InverseProperty(nameof(Message.Receiver))]
        public ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
