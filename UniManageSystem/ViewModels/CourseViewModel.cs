using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace UniManageSystem.ViewModels
{
    public class CourseViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(1, 1000)]
        public int Capacity { get; set; }

        [Display(Name = "Course Image")]
        public IFormFile? ImageUpload { get; set; }

        public string? ExistingImageUrl { get; set; }

        [Display(Name = "Lecturer")]
        public string? LecturerId { get; set; }
    }
}
