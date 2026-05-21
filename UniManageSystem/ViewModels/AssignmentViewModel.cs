using System;
using System.ComponentModel.DataAnnotations;

namespace UniManageSystem.ViewModels
{
    public class AssignmentViewModel
    {
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(7);

        [Required]
        [Range(0, 1000)]
        [Display(Name = "Max Score")]
        public decimal MaxScore { get; set; } = 100;
    }
}
