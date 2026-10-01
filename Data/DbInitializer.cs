using Microsoft.AspNetCore.Identity;

namespace BeeDienLanh.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndUsersAsync(
            IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

            var userManager = serviceProvider
                .GetRequiredService<UserManager<IdentityUser>>();

            // ==============================
            // TẠO ROLE
            // ==============================
            string[] roles =
            {
                "Admin",
                "Customer",
                "Shipper"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }

            // ==============================
            // ADMIN DEMO
            // ==============================
            string adminEmail = "admin@beedienlanh.vn";
            string adminPassword = "Admin@123";

            var admin = await userManager.FindByEmailAsync(adminEmail);

            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    admin,
                    adminPassword);

                if (!result.Succeeded)
                {
                    return;
                }
            }

            // Đảm bảo tài khoản luôn có Role Admin
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
            {
                await userManager.AddToRoleAsync(
                    admin,
                    "Admin");
            }

            // ==============================
            // SHIPPER DEMO
            // ==============================
            string shipperEmail = "shipper@beedienlanh.vn";
            string shipperPassword = "Shipper@123";

            var shipper = await userManager.FindByEmailAsync(
                shipperEmail);

            if (shipper == null)
            {
                shipper = new IdentityUser
                {
                    UserName = shipperEmail,
                    Email = shipperEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(
                    shipper,
                    shipperPassword);

                if (!result.Succeeded)
                {
                    return;
                }
            }

            // Đảm bảo tài khoản luôn có Role Shipper
            if (!await userManager.IsInRoleAsync(
                    shipper,
                    "Shipper"))
            {
                await userManager.AddToRoleAsync(
                    shipper,
                    "Shipper");
            }
        }
    }
}