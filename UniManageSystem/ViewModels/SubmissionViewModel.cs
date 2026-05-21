using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace UniManageSystem.ViewModels
{
    public class SubmissionViewModel
    {
        [Required]
        public int AssignmentId { get; set; }

        [Required(ErrorMessage = "Please select a file to upload.")]
        [Display(Name = "Upload Homework")]
        public IFormFile FileUpload { get; set; } = null!;
    }
}
