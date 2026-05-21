using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniManageSystem.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        public int CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public Course? Course { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public decimal MaxScore { get; set; } = 100;

        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
}
