using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniManageSystem.Models;

namespace UniManageSystem.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure Lecturer role exists
            if (!await roleManager.RoleExistsAsync("Lecturer"))
            {
                await roleManager.CreateAsync(new IdentityRole("Lecturer"));
            }

            // Create Sri Lankan lecturers
            var lecturersData = new[] 
            {
                new { Email = "kasun.perera@gmail.com", FirstName = "Kasun", LastName = "Perera" },
                new { Email = "nimal.silva@yahoo.com", FirstName = "Nimal", LastName = "Silva" },
                new { Email = "chaminda.bandara@hotmail.com", FirstName = "Chaminda", LastName = "Bandara" },
                new { Email = "malini.fernando@gmail.com", FirstName = "Malini", LastName = "Fernando" },
                new { Email = "ruwan.rajapaksha@yahoo.com", FirstName = "Ruwan", LastName = "Rajapaksha" },
                new { Email = "sunethra.jayasuriya@hotmail.com", FirstName = "Sunethra", LastName = "Jayasuriya" },
                new { Email = "dinesh.kumara@gmail.com", FirstName = "Dinesh", LastName = "Kumara" },
                new { Email = "ajantha.mendis@yahoo.com", FirstName = "Ajantha", LastName = "Mendis" },
                new { Email = "nuwan.rathnayake@hotmail.com", FirstName = "Nuwan", LastName = "Rathnayake" },
                new { Email = "kamal.dissanayake@gmail.com", FirstName = "Kamal", LastName = "Dissanayake" }
            };

            foreach (var l in lecturersData)
            {
                if (await userManager.FindByEmailAsync(l.Email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = l.Email,
                        Email = l.Email,
                        FirstName = l.FirstName,
                        LastName = l.LastName,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(user, "Password123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Lecturer");
                    }
                }
            }

            // In order to fix existing badly allocated courses pointing ONLY to Kasun,
            // we remap them appropriately if they exist.
            var existingAllCourses = context.Courses.ToList();
            if (existingAllCourses.Any())
            {
                var lsExisting = new ApplicationUser[10];
                for (int i = 0; i < 10; i++)
                {
                    lsExisting[i] = await userManager.FindByEmailAsync(lecturersData[i].Email);
                }

                if (lsExisting.All(l => l != null))
                {
                    for (int i = 0; i < existingAllCourses.Count; i++)
                    {
                      // Modulo 10 ensures fair distribution
                       existingAllCourses[i].LecturerId = lsExisting[i % 10].Id;
                    }
                    await context.SaveChangesAsync();
                }
            }

            // Cleanup old generic lecturer if it exists and allocate courses
            var oldLecturer = await userManager.FindByEmailAsync("lecturer@ucms.com");
            if (oldLecturer != null)
            {
                var newLecturer = await userManager.FindByEmailAsync("kasun.perera@gmail.com"); // Using .gmail instead
                if (newLecturer != null)
                {
                    var existingCourses = context.Courses.Where(c => c.LecturerId == oldLecturer.Id).ToList();
                    foreach (var course in existingCourses)
                    {
                        course.LecturerId = newLecturer.Id;
                    }
                    await context.SaveChangesAsync();
                }
                
                await userManager.DeleteAsync(oldLecturer);
            }

            // Cleanup old generic default student if it exists
            var oldDefaultStudent = await userManager.FindByEmailAsync("student@ucms.com");
            if (oldDefaultStudent != null)
            {
                var theirEnrollments = context.Enrollments.Where(e => e.StudentId == oldDefaultStudent.Id).ToList();
                if(theirEnrollments.Any())
                {
                    context.Enrollments.RemoveRange(theirEnrollments);
                    await context.SaveChangesAsync();
                }
                await userManager.DeleteAsync(oldDefaultStudent);
            }

            // Cleanup ALL old `@ucms.com` users (cleanup previous generation runs) EXCEPT admin if left behind
            var allUcmsUsers = await userManager.Users.Where(u => u.Email.Contains("@ucms.com")).ToListAsync();
            foreach(var oldU in allUcmsUsers)
            {
                // First ensure any assigned courses are cleared or reassigned to avoid constraint errors
                var theirCourses = context.Courses.Where(c => c.LecturerId == oldU.Id).ToList();
                if(theirCourses.Any()) 
                {
                   var fallbackLecturer = await userManager.FindByEmailAsync("kasun.perera@gmail.com");
                   foreach(var tc in theirCourses) {
                       tc.LecturerId = fallbackLecturer?.Id ?? oldU.Id;
                   }
                   await context.SaveChangesAsync();
                }

                // Remove enrollments for old students too
                var theirEnrollments = context.Enrollments.Where(e => e.StudentId == oldU.Id).ToList();
                if(theirEnrollments.Any())
                {
                    context.Enrollments.RemoveRange(theirEnrollments);
                    await context.SaveChangesAsync();
                }

                await userManager.DeleteAsync(oldU);
            }

            // Create 20 sample students
            var studentsData = new[] 
            {
                new { Email = "saman.kumara@gmail.com", FirstName = "Saman", LastName = "Kumara" },
                new { Email = "nimali.perera@yahoo.com", FirstName = "Nimali", LastName = "Perera" },
                new { Email = "ruwan.fernando@hotmail.com", FirstName = "Ruwan", LastName = "Fernando" },
                new { Email = "chamath.silva@gmail.com", FirstName = "Chamath", LastName = "Silva" },
                new { Email = "sanduni.bandara@yahoo.com", FirstName = "Sanduni", LastName = "Bandara" },
                new { Email = "kasun.jayasooriya@gmail.com", FirstName = "Kasun", LastName = "Jayasooriya" },
                new { Email = "tharushi.rathnayake@hotmail.com", FirstName = "Tharushi", LastName = "Rathnayake" },
                new { Email = "dinesh.hettiarachchi@gmail.com", FirstName = "Dinesh", LastName = "Hettiarachchi" },
                new { Email = "amanda.gunawardena@yahoo.com", FirstName = "Amanda", LastName = "Gunawardena" },
                new { Email = "lahiru.rajapaksha@gmail.com", FirstName = "Lahiru", LastName = "Rajapaksha" },
                new { Email = "oshadi.mendis@yahoo.com", FirstName = "Oshadi", LastName = "Mendis" },
                new { Email = "ishara.dissanayake@gmail.com", FirstName = "Ishara", LastName = "Dissanayake" },
                new { Email = "gayan.sirisekara@hotmail.com", FirstName = "Gayan", LastName = "Sirisekara" },
                new { Email = "harshani.senanayake@gmail.com", FirstName = "Harshani", LastName = "Senanayake" },
                new { Email = "nuwan.gamage@yahoo.com", FirstName = "Nuwan", LastName = "Gamage" },
                new { Email = "kavindi.weerasinghe@gmail.com", FirstName = "Kavindi", LastName = "Weerasinghe" },
                new { Email = "chaturanga.ekanayake@hotmail.com", FirstName = "Chaturanga", LastName = "Ekanayake" },
                new { Email = "madhushani.peiris@gmail.com", FirstName = "Madhushani", LastName = "Peiris" },
                new { Email = "asanka.wijesinghe@yahoo.com", FirstName = "Asanka", LastName = "Wijesinghe" },
                new { Email = "piyumi.wimalasiri@gmail.com", FirstName = "Piyumi", LastName = "Wimalasiri" }
            };

            foreach (var s in studentsData)
            {
                if (await userManager.FindByEmailAsync(s.Email) == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = s.Email,
                        Email = s.Email,
                        FirstName = s.FirstName,
                        LastName = s.LastName,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(user, "Password123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Student");
                    }
                }
            }

            // Allocate students to existing courses to populate data seamlessly
            var defaultStudent = await userManager.FindByEmailAsync("saman.kumara@gmail.com");
            if (defaultStudent != null)
            {
                var allCourses = context.Courses.Include(c => c.Enrollments).ToList();
                foreach (var c in allCourses)
                {
                    if (!c.Enrollments.Any(e => e.StudentId == defaultStudent.Id))
                    {
                        context.Enrollments.Add(new Enrollment
                        {
                            CourseId = c.Id,
                            StudentId = defaultStudent.Id,
                            EnrollmentDate = DateTime.UtcNow,
                            Status = EnrollmentStatus.Pending // Put inside Pending Requests for screenshot
                        });
                    }
                }
                await context.SaveChangesAsync();
            }

            // Delete old dummy courses
            var dummyCourses = context.Courses.Where(c => c.Title == "Introduction to Computer Science" || c.Title == "Advanced Data Structures" || c.Title == "Web Development Fundamentals").ToList();
            if (dummyCourses.Any())
            {
                // Remove their enrollments first
                var dummyCourseIds = dummyCourses.Select(c => c.Id).ToList();
                var dummyEnrollments = context.Enrollments.Where(e => dummyCourseIds.Contains(e.CourseId)).ToList();
                context.Enrollments.RemoveRange(dummyEnrollments);
                
                context.Courses.RemoveRange(dummyCourses);
                await context.SaveChangesAsync();
            }

            if (!context.Courses.Any(c => c.Title == "Mobile App Development"))
            {
                var ls = new ApplicationUser[10];
                for (int i = 0; i < 10; i++)
                {
                    ls[i] = await userManager.FindByEmailAsync(lecturersData[i].Email);
                }

                if (ls.All(l => l != null))
                {
                    context.Courses.AddRange(
                        new Course { Title = "Advanced C# Programming (SL)", Description = "Advanced programming concepts using C# taught by Kasun Perera.", ImageUrl = "placeholder.png", Capacity = 45, LecturerId = ls[0].Id },
                        new Course { Title = "Sri Lankan History and Culture", Description = "Deep dive into the rich history and traditions of Sri Lanka.", ImageUrl = "placeholder.png", Capacity = 60, LecturerId = ls[1].Id },
                        new Course { Title = "Tropical Agriculture", Description = "Modern agricultural practices suitable for tropical climates.", ImageUrl = "placeholder.png", Capacity = 50, LecturerId = ls[2].Id },
                        new Course { Title = "Sinhala Literature", Description = "Introduction to classic and modern Sinhala literary works.", ImageUrl = "placeholder.png", Capacity = 40, LecturerId = ls[3].Id },
                        new Course { Title = "South Asian Economics", Description = "An overview of economic trends in South Asia.", ImageUrl = "placeholder.png", Capacity = 55, LecturerId = ls[4].Id },
                        new Course { Title = "AI & Machine Learning", Description = "Practical applications of ML algorithms.", ImageUrl = "placeholder.png", Capacity = 40, LecturerId = ls[5].Id },
                        new Course { Title = "Web Engineering Patterns", Description = "Best practices in modern web development architectures.", ImageUrl = "placeholder.png", Capacity = 50, LecturerId = ls[6].Id },
                        new Course { Title = "Environmental Science", Description = "Detailed studies on climate change and ecology.", ImageUrl = "placeholder.png", Capacity = 60, LecturerId = ls[7].Id },
                        new Course { Title = "Coastal Ecosystems Management", Description = "Sustainable practices for protecting maritime regions.", ImageUrl = "placeholder.png", Capacity = 35, LecturerId = ls[8].Id },
                        new Course { Title = "Mobile App Development", Description = "Building cross-platform smartphone software systems.", ImageUrl = "placeholder.png", Capacity = 50, LecturerId = ls[9].Id },
                        // Adding 5 more courses so total is 15. Reusing lecturers 0 to 4.
                        new Course { Title = "Business Process Analysis", Description = "Analyzing business models and IT alignment.", ImageUrl = "placeholder.png", Capacity = 55, LecturerId = ls[0].Id },
                        new Course { Title = "Cloud Computing Architectures", Description = "Deploying and managing distributed systems on the cloud.", ImageUrl = "placeholder.png", Capacity = 45, LecturerId = ls[1].Id },
                        new Course { Title = "Information Security Fundamentals", Description = "Basic concepts of securing modern data systems.", ImageUrl = "placeholder.png", Capacity = 60, LecturerId = ls[2].Id },
                        new Course { Title = "Digital Marketing Strategy", Description = "Modern techniques for online audience engagement.", ImageUrl = "placeholder.png", Capacity = 70, LecturerId = ls[3].Id },
                        new Course { Title = "Software Quality Assurance", Description = "Testing principles for robust application development.", ImageUrl = "placeholder.png", Capacity = 40, LecturerId = ls[4].Id }
                    );

                    await context.SaveChangesAsync();
                }
            }

            if (!context.Assignments.Any())
            {
                var targetCourses = context.Courses.Take(3).ToList();
                if (targetCourses.Count >= 3)
                {
                    context.Assignments.AddRange(
                        new Assignment { CourseId = targetCourses[0].Id, Title = "Midterm Project Submissions", Description = "Submit your comprehensive project documentation and source code.", DueDate = DateTime.UtcNow.AddDays(7), MaxScore = 100 },
                        new Assignment { CourseId = targetCourses[1].Id, Title = "Research Concept Paper", Description = "A 2000-word paper elaborating core ideas.", DueDate = DateTime.UtcNow.AddDays(14), MaxScore = 50 },
                        new Assignment { CourseId = targetCourses[2].Id, Title = "Weekly Theory Quiz", Description = "Complete your weekly quiz covering chapters 1 to 3.", DueDate = DateTime.UtcNow.AddDays(3), MaxScore = 20 }
                    );
                    await context.SaveChangesAsync();
                }
            }

            if (!context.Submissions.Any())
            {
                var assignment = context.Assignments.FirstOrDefault();
                var student = await userManager.FindByEmailAsync("saman.kumara@gmail.com");

                if (assignment != null && student != null)
                {
                    context.Submissions.Add(new Submission
                    {
                        AssignmentId = assignment.Id,
                        StudentId = student.Id,
                        SubmissionDate = DateTime.UtcNow.AddHours(-12),
                        FilePath = "/uploads/submissions/sample_midterm.pdf",
                        Score = 85,
                        Feedback = "Great logic implementation. Let's focus on cleaning up the code architecture next time."
                    });
                    
                    await context.SaveChangesAsync();
                }
            }

            if (!context.Messages.Any())
            {
                var admin = await userManager.FindByEmailAsync("admin@unimanage.edu");
                var lecturer = await userManager.FindByEmailAsync("kasun.perera@gmail.com");
                var student = await userManager.FindByEmailAsync("saman.kumara@gmail.com");

                if (admin != null && lecturer != null && student != null)
                {
                    context.Messages.AddRange(
                        new Message
                        {
                            SenderId = admin.Id,
                            ReceiverId = lecturer.Id,
                            Subject = "Welcome to UniManage System",
                            Content = "Dear Kasun, welcome to the platform. Please let us know if you need any assistance setting up your initial courses.",
                            SentDate = DateTime.UtcNow.AddDays(-2),
                            IsRead = true
                        },
                        new Message
                        {
                            SenderId = student.Id,
                            ReceiverId = lecturer.Id,
                            Subject = "Question regarding Midterm Project",
                            Content = "Hello Professor, could you please clarify if we need to include a UML diagram in our project documentation? Thank you.",
                            SentDate = DateTime.UtcNow.AddHours(-24),
                            IsRead = false
                        },
                        new Message
                        {
                            SenderId = lecturer.Id,
                            ReceiverId = student.Id,
                            Subject = "RE: Question regarding Midterm Project",
                            Content = "Hi Saman. Yes, please include at least a basic class diagram to explain your architecture. Best regards.",
                            SentDate = DateTime.UtcNow.AddHours(-22),
                            IsRead = false
                        }
                    );
                    
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
