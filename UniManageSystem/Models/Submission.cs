using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManageSystem.Models
{
    public class Submission
    {
        public int Id { get; set; }

        public int AssignmentId { get; set; }
        [ForeignKey(nameof(AssignmentId))]
        public Assignment? Assignment { get; set; }

        public string StudentId { get; set; } = string.Empty;
        [ForeignKey(nameof(StudentId))]
        public ApplicationUser? Student { get; set; }

        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public decimal? Score { get; set; }

        public string? Feedback { get; set; }

        public bool IsLate => SubmissionDate > Assignment?.DueDate;
    }
}
