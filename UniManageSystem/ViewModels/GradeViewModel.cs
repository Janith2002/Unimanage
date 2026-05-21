using System.ComponentModel.DataAnnotations;

namespace UniManageSystem.ViewModels
{
    public class GradeViewModel
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal Score { get; set; }

        public string? Feedback { get; set; }
    }
}
