using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Services
{
    public class PaymentReminderService : IPaymentReminderService
    {
        private readonly ApplicationDbContext _db;
        private readonly ISmsSender _smsSender;
        private readonly ICurrentSocietyContext _tenant;

        public PaymentReminderService(ApplicationDbContext db, ISmsSender smsSender, ICurrentSocietyContext tenant)
        {
            _db = db;
            _smsSender = smsSender;
            _tenant = tenant;
        }

        public async Task<ReminderBatchResult> SendMonthlyRemindersAsync(CancellationToken ct = default)
        {
            var result = new ReminderBatchResult();

            // Members/Loans/MonthlyCycles below are automatically scoped to _tenant.SocietyId by
            // ApplicationDbContext's global query filter -- the caller (a controller for an
            // interactive "send now", or MonthlyReminderBackgroundService for the automatic
            // nightly batch) is responsible for the ambient tenant being set correctly first.
            var society = _tenant.SocietyId is { } societyId
                ? await _db.Societies.FirstOrDefaultAsync(s => s.SocietyId == societyId, ct)
                : null;
            var societyName = society?.NameMarathi ?? society?.Name ?? "आमची सोसायटी";

            var members = await _db.Members.Where(m => m.Status == "Active").ToListAsync(ct);
            var activeLoans = await _db.Loans.Where(l => l.Status == "Active").ToListAsync(ct);
            var openCycle = await _db.MonthlyCycles.FirstOrDefaultAsync(c => c.Status == "Open", ct);
            var openInstallments = openCycle is null
                ? new List<LoanInstallment>()
                : await _db.LoanInstallments.Where(i => i.MonthlyCycleId == openCycle.MonthlyCycleId).ToListAsync(ct);

            result.TotalActiveMembers = members.Count;

            foreach (var member in members)
            {
                if (string.IsNullOrWhiteSpace(member.MobileNumber))
                {
                    result.SkippedNoMobile++;
                    continue;
                }

                var loan = activeLoans.FirstOrDefault(l => l.MemberId == member.MemberId);
                string message;
                string reminderType;

                if (loan is not null)
                {
                    var installment = openInstallments.FirstOrDefault(i => i.LoanId == loan.LoanId);
                    decimal principal, interest;
                    bool exact;
                    if (installment is not null)
                    {
                        principal = installment.PrincipalAmount;
                        interest = installment.InterestAmount;
                        exact = true;
                    }
                    else
                    {
                        // No open cycle yet (or this loan has no installment row in it) -- estimate
                        // from the loan's usual installment and current outstanding balance so the
                        // reminder still carries a useful number, clearly marked as approximate.
                        principal = loan.InstallmentAmount ?? 0m;
                        interest = Math.Round(loan.OutstandingPrincipal * loan.InterestRate / 100m, 0);
                        exact = false;
                    }
                    var total = principal + interest + member.MonthlyShareAmount;
                    var approxNote = exact ? "" : " (अंदाजे)";
                    message = $"नमस्कार {member.FullNameMarathi ?? member.FullName}, || {societyName} || - " +
                              $"या महिन्याचा हप्ता: मुद्दल रु.{principal:N0} + व्याज रु.{interest:N0} + शेअर रु.{member.MonthlyShareAmount:N0} " +
                              $"= एकूण रु.{total:N0}{approxNote}, दि. २० पर्यंत जमा करा.";
                    reminderType = "Loan";
                }
                else
                {
                    message = $"नमस्कार {member.FullNameMarathi ?? member.FullName}, || {societyName} || - " +
                              $"या महिन्याची शेअर रक्कम रु.{member.MonthlyShareAmount:N0} दि. २० पर्यंत जमा करा.";
                    reminderType = "Share";
                }

                var sendResult = await _smsSender.SendAsync(member.MobileNumber!, message, ct);

                _db.SmsLogs.Add(new SmsLog
                {
                    MemberId = member.MemberId,
                    PhoneNumber = member.MobileNumber!,
                    Message = message,
                    ReminderType = reminderType,
                    SentDate = DateTime.UtcNow,
                    Success = sendResult.Success,
                    ProviderResponse = sendResult.ResponseOrError
                });

                if (sendResult.Success) result.Sent++; else result.Failed++;
            }

            await _db.SaveChangesAsync(ct);
            return result;
        }
    }
}
