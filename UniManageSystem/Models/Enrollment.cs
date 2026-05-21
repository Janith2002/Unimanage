using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManageSystem.Models
{
    public enum EnrollmentStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public class Enrollment
    {
        public int Id { get; set; }

        public int CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public Course? Course { get; set; }

        public string StudentId { get; set; } = string.Empty;
        [ForeignKey(nameof(StudentId))]
        public ApplicationUser? Student { get; set; }

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;

        public string? ApprovedById { get; set; }
        [ForeignKey(nameof(ApprovedById))]
        public ApplicationUser? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [Range(0, 100)]
        public decimal? FinalGrade { get; set; }
    }
}
