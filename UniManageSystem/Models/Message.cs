using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManageSystem.Models
{
    public class Message
    {
        public int Id { get; set; }

        public string SenderId { get; set; } = string.Empty;
        [ForeignKey(nameof(SenderId))]
        public ApplicationUser? Sender { get; set; }

        public string ReceiverId { get; set; } = string.Empty;
        [ForeignKey(nameof(ReceiverId))]
        public ApplicationUser? Receiver { get; set; }

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime SentDate { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}
