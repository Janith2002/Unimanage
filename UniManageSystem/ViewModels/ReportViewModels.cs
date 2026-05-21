using System;
using System.Collections.Generic;

namespace UniManageSystem.ViewModels
{
    public class AdminReportViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }
        public List<CoursePopularityDto> CoursePopularity { get; set; } = new();
    }

    public class LecturerReportViewModel
    {
        public List<LecturerCoursePerformanceDto> CoursePerformances { get; set; } = new();
    }

    public class StudentReportViewModel
    {
        public string StudentName { get; set; } = string.Empty;
        public decimal OverallAverage { get; set; }
        public List<StudentCourseGradeDto> CourseGrades { get; set; } = new();
        public List<StudentSubmissionDto> RecentSubmissions { get; set; } = new();
    }

    public class CoursePopularityDto
    {
        public string CourseName { get; set; } = string.Empty;
        public int Enrollments { get; set; }
        public int Capacity { get; set; }
        public decimal FillPercentage => Capacity > 0 ? ((decimal)Enrollments / Capacity) * 100 : 0;
    }

    public class LecturerCoursePerformanceDto
    {
        public string CourseName { get; set; } = string.Empty;
        public int EnrolledStudents { get; set; }
        public decimal AverageCourseGrade { get; set; }
        public List<AssignmentAverageDto> AssignmentAverages { get; set; } = new();
        public List<StudentGradeDto> StudentGrades { get; set; } = new();
    }

    public class AssignmentAverageDto
    {
        public string AssignmentTitle { get; set; } = string.Empty;
        public decimal AverageScore { get; set; }
        public decimal MaxScore { get; set; }
    }

    public class StudentGradeDto
    {
        public string StudentName { get; set; } = string.Empty;
        public decimal? FinalGrade { get; set; }
    }

    public class StudentCourseGradeDto
    {
        public string CourseName { get; set; } = string.Empty;
        public decimal? FinalGrade { get; set; }
    }

    public class StudentSubmissionDto
    {
        public string AssignmentTitle { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public decimal? Score { get; set; }
        public decimal MaxScore { get; set; }
        public DateTime SubmissionDate { get; set; }
    }
}