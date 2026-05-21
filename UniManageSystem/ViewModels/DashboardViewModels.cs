using System.Collections.Generic;
using UniManageSystem.Models;

namespace UniManageSystem.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalLecturers { get; set; }
        public int TotalCourses { get; set; }
        public int TotalEnrollments { get; set; }

        // For Chart.js (Course Popularity)
        public List<string> CourseNames { get; set; } = new();
        public List<int> EnrollmentCounts { get; set; } = new();
    }

    public class LecturerDashboardViewModel
    {
        public int TotalCoursesTaught { get; set; }
        public int TotalStudentsEnrolled { get; set; }
        public int PendingSubmissionsToGrade { get; set; }

        public List<Course> RecentCourses { get; set; } = new();

        // For Chart.js (Submissions per Assignment)
        public List<string> AssignmentNames { get; set; } = new();
        public List<int> SubmissionCounts { get; set; } = new();
    }

    public class StudentDashboardViewModel
    {
        public int EnrolledCoursesCount { get; set; }
        public int PendingAssignmentsCount { get; set; }
        public decimal AverageGrade { get; set; }

        public List<Assignment> UpcomingAssignments { get; set; } = new();

        // For Chart.js (Grades per Course)
        public List<string> CourseNames { get; set; } = new();
        public List<decimal> Grades { get; set; } = new();
    }
}