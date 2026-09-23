using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;
using SwamiSamarthSociety.Services;

namespace SwamiSamarthSociety.Web.BackgroundServices
{
    // Wakes up every 30 minutes and, on the 18th/19th/20th of the month between 7am and noon,
    // sends the monthly payment reminder SMS batch once per day for every active society
    // (tracked via a per-society AppSetting row so a restart or a slow tick never double-sends
    // the same day's batch, and so one society's batch can never mark another as "already sent").
    //
    // This runs with no HttpContext, so there's no ambient tenant from a signed-in user's
    // claims -- each society gets its own fresh DI scope with the tenant pinned explicitly via
    // ICurrentSocietyContext.SetExplicit before anything scoped to that society is resolved.
    public class MonthlyReminderBackgroundService : BackgroundService
    {
        private static readonly int[] ReminderDays = { 18, 19, 20 };
        private const int MorningStartHour = 7;
        private const int MorningEndHour = 12;
        private const string LastSentSettingKey = "LastPaymentReminderSentDate";

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MonthlyReminderBackgroundService> _logger;

        public MonthlyReminderBackgroundService(IServiceScopeFactory scopeFactory, ILogger<MonthlyReminderBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Monthly payment reminder check failed.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Shutting down.
                }
            }
        }

        private async Task CheckAndSendAsync(CancellationToken ct)
        {
            var now = DateTime.Now;
            if (!ReminderDays.Contains(now.Day)) return;
            if (now.Hour < MorningStartHour || now.Hour >= MorningEndHour) return;

            List<int> societyIds;
            using (var lookupScope = _scopeFactory.CreateScope())
            {
                var lookupDb = lookupScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                societyIds = await lookupDb.Societies.IgnoreQueryFilters()
                    .Where(s => s.IsActive)
                    .Select(s => s.SocietyId)
                    .ToListAsync(ct);
            }

            foreach (var societyId in societyIds)
            {
                await SendForSocietyAsync(societyId, now, ct);
            }
        }

        private async Task SendForSocietyAsync(int societyId, DateTime now, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var tenant = scope.ServiceProvider.GetRequiredService<ICurrentSocietyContext>();
            tenant.SetExplicit(societyId, isSuperAdmin: false);

            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var todayStr = now.ToString("yyyy-MM-dd");
            var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == LastSentSettingKey, ct);
            if (setting?.Value == todayStr) return; // already sent today's batch for this society

            var reminderService = scope.ServiceProvider.GetRequiredService<IPaymentReminderService>();
            var result = await reminderService.SendMonthlyRemindersAsync(ct);
            _logger.LogInformation(
                "Society {SocietyId}: monthly payment reminder batch sent: {Sent} sent, {Failed} failed, {Skipped} skipped (no mobile on file).",
                societyId, result.Sent, result.Failed, result.SkippedNoMobile);

            if (setting is null)
                db.AppSettings.Add(new AppSetting
                {
                    Key = LastSentSettingKey,
                    Value = todayStr,
                    Description = "Last date the automatic monthly payment reminder SMS batch ran (18th/19th/20th mornings)."
                });
            else
                setting.Value = todayStr;

            await db.SaveChangesAsync(ct);
        }
    }
}
