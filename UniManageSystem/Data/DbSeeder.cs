using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using UniManageSystem.Models;
using System.Linq;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace UniManageSystem.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Define roles
            var roles = new[] { "Admin", "Lecturer", "Student" };
            
            // Seed Roles
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Admin User
            await CreateUserAsync(userManager, "admin@unimanage.edu", "Admin@123", "Admin", "System", "Administrator");
        }

        private static async Task CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string password, string role, string firstName, string lastName)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }
        }
    }
}
