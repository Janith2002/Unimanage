using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace UniManageSystem.ViewModels
{
    public class ChatViewModel
    {
        public string ChatUserId { get; set; } = string.Empty;
        public string ChatUserName { get; set; } = string.Empty;

        public List<MessageViewModel> Messages { get; set; } = new();
    }

    public class MessageViewModel
    {
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string SentDate { get; set; } = string.Empty;
        public bool IsMine { get; set; }
    }

    public class ContactViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UnreadCount { get; set; }
    }
}