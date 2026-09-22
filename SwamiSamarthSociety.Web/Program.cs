using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Services;
using SwamiSamarthSociety.Web.BackgroundServices;

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

        builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddRoles<IdentityRole>()
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

        using (var scope = app.Services.CreateScope())
        {
            SeedRolesAsync(scope.ServiceProvider).GetAwaiter().GetResult();
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

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();

        app.Run();
    }

    // Ensures the Chairman/Member roles exist, and that every user has exactly one of them.
    // Any user with no role yet becomes Chairman if nobody holds that role, else Member --
    // this is what promotes the very first account (created before roles existed) to Chairman.
    private static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var role in new[] { AppRoles.Chairman, AppRoles.Member })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var hasChairman = (await userManager.GetUsersInRoleAsync(AppRoles.Chairman)).Count > 0;

        foreach (var user in userManager.Users.ToList())
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Count > 0) continue;

            if (!hasChairman)
            {
                await userManager.AddToRoleAsync(user, AppRoles.Chairman);
                hasChairman = true;
            }
            else
            {
                await userManager.AddToRoleAsync(user, AppRoles.Member);
            }
        }
    }
}
