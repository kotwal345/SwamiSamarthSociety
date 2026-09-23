using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;
using SwamiSamarthSociety.Services;
using SwamiSamarthSociety.Web.BackgroundServices;
using SwamiSamarthSociety.Web.Identity;
using SwamiSamarthSociety.Web.Middleware;

namespace SwamiSamarthSociety.Web;

public class Program
{
    public static void Main(string[] args)
    {
        // Free for organizations under $1M annual revenue -- see https://questpdf.com/license/
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var builder = WebApplication.CreateBuilder(args);

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ICurrentSocietyContext, CurrentSocietyContext>();

        builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddRoles<IdentityRole>()
            .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        builder.Services.AddScoped<IMemberService, MemberService>();
        builder.Services.AddScoped<IInterestCalculationService, InterestCalculationService>();
        builder.Services.AddScoped<IInstallmentCalculationService, InstallmentCalculationService>();
        builder.Services.AddScoped<ILoanService, LoanService>();
        builder.Services.AddScoped<IMonthlyCycleService, MonthlyCycleService>();
        builder.Services.AddScoped<IPaymentCollectionService, PaymentCollectionService>();

        builder.Services.AddHttpClient<ISmsSender, Msg91SmsSender>();
        builder.Services.AddScoped<IPaymentReminderService, PaymentReminderService>();
        builder.Services.AddHostedService<MonthlyReminderBackgroundService>();

        builder.Services.AddHttpClient<IWhatsAppSender, WhatsAppCloudApiSender>();
        builder.Services.AddScoped<IPaymentConfirmationService, PaymentConfirmationService>();

        builder.Services.AddControllersWithViews();

        var app = builder.Build();

        // One-off maintenance hook: `dotnet run -- --reset-password <email> <newPassword>` force-
        // resets a login's password and flags it for a mandatory change on next sign-in, for
        // accounts whose password the owner has lost (e.g. a pre-existing account that predates
        // admin-provisioned logins). Never runs during normal startup.
        if (args.Length == 3 && args[0] == "--reset-password")
        {
            using var scope = app.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var email = args[1];
            var newPassword = args[2];

            var user = db.Users.IgnoreQueryFilters().FirstOrDefault(u => u.NormalizedEmail == email.ToUpperInvariant());
            if (user is null)
            {
                Console.WriteLine($"No account found for {email}.");
                return;
            }

            var token = userManager.GeneratePasswordResetTokenAsync(user).GetAwaiter().GetResult();
            var result = userManager.ResetPasswordAsync(user, token, newPassword).GetAwaiter().GetResult();
            if (!result.Succeeded)
            {
                Console.WriteLine("Failed: " + string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            user.MustChangePassword = true;
            userManager.UpdateAsync(user).GetAwaiter().GetResult();
            Console.WriteLine($"Password reset for {email}. They'll be prompted to set their own on next login.");
            return;
        }

        using (var scope = app.Services.CreateScope())
        {
            StartupSeeding.SeedAsync(scope.ServiceProvider).GetAwaiter().GetResult();
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<RequirePasswordChangeMiddleware>();

        app.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();

        app.Run();
    }
}
