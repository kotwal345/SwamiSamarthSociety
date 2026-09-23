using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Web
{
    // Runs once at every startup (each step is idempotent). Order matters:
    // 1) rename the legacy "Chairman" role in place (if this DB predates multi-tenancy) so the
    //    later "ensure roles exist" step doesn't create a second, separate SocietyAdmin role;
    // 2) ensure all 3 roles exist;
    // 3) seed the one SuperAdmin account from configuration;
    // 4) backfill all pre-existing data as tenant #1, exactly once, ever.
    public static class StartupSeeding
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var config = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            var chairmanRole = await roleManager.FindByNameAsync("Chairman");
            if (chairmanRole is not null)
            {
                chairmanRole.Name = AppRoles.SocietyAdmin;
                chairmanRole.NormalizedName = AppRoles.SocietyAdmin.ToUpperInvariant();
                await roleManager.UpdateAsync(chairmanRole);
            }

            foreach (var role in new[] { AppRoles.SuperAdmin, AppRoles.SocietyAdmin, AppRoles.Member })
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            var superAdminEmail = config["SuperAdmin:Email"];
            var superAdminPassword = config["SuperAdmin:Password"];
            if (!string.IsNullOrWhiteSpace(superAdminEmail) && !string.IsNullOrWhiteSpace(superAdminPassword))
            {
                var existing = await db.Users.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.NormalizedEmail == superAdminEmail.ToUpperInvariant());
                if (existing is null)
                {
                    var superAdmin = new ApplicationUser
                    {
                        UserName = superAdminEmail,
                        Email = superAdminEmail,
                        EmailConfirmed = true,
                        SocietyId = null
                    };
                    var createResult = await userManager.CreateAsync(superAdmin, superAdminPassword);
                    if (createResult.Succeeded)
                        await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
                    else
                        logger.LogWarning("Failed to seed SuperAdmin account: {Errors}",
                            string.Join("; ", createResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogWarning("SuperAdmin:Email / SuperAdmin:Password not configured -- no SuperAdmin account seeded.");
            }

            if (!await db.Societies.IgnoreQueryFilters().AnyAsync())
                await SeedTenantOneAsync(db, logger);

            var userIds = await db.Users.IgnoreQueryFilters().Select(u => new { u.Id, u.Email }).ToListAsync();
            var idsWithRoles = (await db.UserRoles.Select(ur => ur.UserId).ToListAsync()).ToHashSet();
            foreach (var user in userIds.Where(u => !idsWithRoles.Contains(u.Id)))
                logger.LogWarning("User {Email} has no role assigned.", user.Email);
        }

        private static async Task SeedTenantOneAsync(ApplicationDbContext db, ILogger logger)
        {
            var society = new Society
            {
                Name = "Swami Samarth Society",
                Code = "SWAMI-SAMARTH",
                IsActive = true,
                CreatedBy = "system-migration"
            };
            db.Societies.Add(society);
            await db.SaveChangesAsync();
            var societyId = society.SocietyId;

            await db.Members.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.MonthlyCycles.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.MemberSharePayments.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.Loans.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.LoanGuarantors.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.LoanInstallments.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.LoanPayments.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.LoanInstallmentRules.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.AppSettings.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.BankAccounts.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.BankTransactions.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.FinancialTransactions.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.MemberExitSettlements.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.AuditLogs.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.ImportLogs.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));
            await db.SmsLogs.IgnoreQueryFilters().ExecuteUpdateAsync(s => s.SetProperty(x => x.SocietyId, societyId));

            // Every pre-existing login (the old single Chairman + any Members) belonged to this
            // one society -- but explicitly exclude SuperAdmin account(s) (seeded moments ago,
            // above) from this blanket backfill, since ExecuteUpdateAsync with IgnoreQueryFilters
            // would otherwise touch every AspNetUsers row including them.
            var superAdminUserIds = await db.UserRoles
                .Where(ur => db.Roles.Where(r => r.Name == AppRoles.SuperAdmin).Select(r => r.Id).Contains(ur.RoleId))
                .Select(ur => ur.UserId)
                .ToListAsync();
            await db.Users.IgnoreQueryFilters()
                .Where(u => !superAdminUserIds.Contains(u.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.SocietyId, societyId));

            logger.LogInformation("Seeded tenant #1 ('{Name}') from pre-existing single-tenant data.", society.Name);
        }
    }
}
