using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Services
{
    public class MonthlyCycleService : IMonthlyCycleService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInterestCalculationService _interestCalculationService;
        private readonly IInstallmentCalculationService _installmentCalculationService;

        public MonthlyCycleService(
            ApplicationDbContext db,
            IInterestCalculationService interestCalculationService,
            IInstallmentCalculationService installmentCalculationService)
        {
            _db = db;
            _interestCalculationService = interestCalculationService;
            _installmentCalculationService = installmentCalculationService;
        }

        public Task<List<MonthlyCycle>> GetAllAsync() =>
            _db.MonthlyCycles.OrderByDescending(c => c.Year).ThenByDescending(c => c.Month).ToListAsync();

        public Task<MonthlyCycle?> GetByIdAsync(int id) =>
            _db.MonthlyCycles.FirstOrDefaultAsync(c => c.MonthlyCycleId == id);

        public Task<MonthlyCycle?> GetOpenCycleAsync() =>
            _db.MonthlyCycles.FirstOrDefaultAsync(c => c.Status == "Open");

        public async Task<(int Year, int Month)> GetNextCyclePeriodAsync()
        {
            var latest = await _db.MonthlyCycles
                .OrderByDescending(c => c.Year).ThenByDescending(c => c.Month)
                .FirstOrDefaultAsync();

            if (latest is null) return (DateTime.Today.Year, DateTime.Today.Month);
            return latest.Month == 12 ? (latest.Year + 1, 1) : (latest.Year, latest.Month + 1);
        }

        public async Task<MonthlyCycle> OpenNextCycleAsync(decimal openingBankBalance)
        {
            var existingOpen = await GetOpenCycleAsync();
            if (existingOpen is not null)
                throw new InvalidOperationException(
                    $"An open cycle already exists ({existingOpen.Month:D2}/{existingOpen.Year}). Close it before opening a new one.");

            var (year, month) = await GetNextCyclePeriodAsync();
            // The society collects shares and loan installments on the 20th of every month, not the calendar
            // month boundary -- EndDate (the 20th of this cycle's own month) is what LoanInstallment.DueDate
            // and every payment date get stamped with, so it must be the actual billing day.
            var endDate = new DateTime(year, month, 20);
            var startDate = endDate.AddMonths(-1);

            var activeMembers = await _db.Members.Where(m => m.Status == "Active").ToListAsync();
            var expectedShare = activeMembers.Sum(m => m.MonthlyShareAmount);

            var cycle = new MonthlyCycle
            {
                Year = year,
                Month = month,
                StartDate = startDate,
                EndDate = endDate,
                Status = "Open",
                ExpectedShareAmount = expectedShare,
                CollectedShareAmount = 0,
                PendingShareAmount = expectedShare,
                TotalPrincipalCollected = 0,
                TotalInterestCollected = 0,
                TotalLoanDistributed = 0,
                TotalPenaltyCollected = 0,
                OpeningBankBalance = openingBankBalance,
                CreatedDate = DateTime.UtcNow
            };
            _db.MonthlyCycles.Add(cycle);
            await _db.SaveChangesAsync();

            foreach (var member in activeMembers)
            {
                _db.MemberSharePayments.Add(new MemberSharePayment
                {
                    MemberId = member.MemberId,
                    MonthlyCycleId = cycle.MonthlyCycleId,
                    ExpectedAmount = member.MonthlyShareAmount,
                    PaidAmount = 0,
                    Status = "Pending"
                });
            }

            var rules = await _db.LoanInstallmentRules.Where(r => r.IsActive).ToListAsync();
            var activeLoans = await _db.Loans.Where(l => l.Status == "Active").Include(l => l.Member).ToListAsync();
            var activeLoanIds = activeLoans.Select(l => l.LoanId).ToList();

            // Rule (b): हफ्ता न भरल्यास शिल्लक कर्ज रकमेवर ५% दांड आकारला जाईल -- if a loan's most
            // recently closed installment wasn't paid in full, this new installment carries: (1) a
            // penalty of PenaltyPercent% of what was actually left unpaid on that installment (not the
            // whole remaining loan -- short by ₹70 means a penalty on that ₹70), and (2) that same ₹70
            // added back on top of this month's regular principal as Arrears, demanded back now instead
            // of silently riding along inside OutstandingPrincipal until the loan happens to pay it off.
            var penaltyPercent = await GetSettingDecimalAsync("InstallmentPenaltyPercent", 5m);
            var lastInstallmentByLoan = (await _db.LoanInstallments
                    .Where(i => activeLoanIds.Contains(i.LoanId))
                    .ToListAsync())
                .GroupBy(i => i.LoanId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.InstallmentNumber).First());

            foreach (var loan in activeLoans)
            {
                lastInstallmentByLoan.TryGetValue(loan.LoanId, out var lastInstallment);
                var (penalty, arrears) = ComputeLateCharges(lastInstallment, penaltyPercent);

                // Arrears is principal already counted inside OutstandingPrincipal -- carve it out of
                // what's available for this month's *regular* principal installment so the two never
                // double up past what's actually still owed.
                var principalBase = Math.Max(0, loan.OutstandingPrincipal - arrears);
                var principal = _installmentCalculationService.CalculateMonthlyPrincipalInstallment(
                    loan.OriginalLoanAmount, principalBase, loan.InstallmentAmount, rules);
                var interest = _interestCalculationService.CalculateMonthlyInterest(loan.OutstandingPrincipal, loan.InterestRate);
                var installmentNumber = await _db.LoanInstallments.CountAsync(i => i.LoanId == loan.LoanId) + 1;

                // ShareAmount/TotalAmount here mirror the member's share purely for display parity with
                // the club's Excel row layout. MemberSharePayment.PaidAmount is always the authoritative
                // record of share money actually collected — never this field.
                _db.LoanInstallments.Add(new LoanInstallment
                {
                    LoanId = loan.LoanId,
                    MonthlyCycleId = cycle.MonthlyCycleId,
                    InstallmentNumber = installmentNumber,
                    DueDate = cycle.EndDate,
                    OpeningPrincipal = loan.OutstandingPrincipal,
                    PrincipalAmount = principal,
                    InterestAmount = interest,
                    ShareAmount = loan.Member.MonthlyShareAmount,
                    PenaltyAmount = penalty,
                    ArrearsAmount = arrears,
                    TotalAmount = principal + interest + penalty + arrears + loan.Member.MonthlyShareAmount,
                    PaidAmount = 0,
                    ClosingPrincipal = loan.OutstandingPrincipal,
                    Status = "Pending"
                });
            }

            await _db.SaveChangesAsync();
            return cycle;
        }

        public async Task<int> RecalculateOpenCycleDueAsync()
        {
            var cycle = await GetOpenCycleAsync();
            if (cycle is null) return 0;

            // Only touch installments nobody has started paying yet this cycle -- once a payment is
            // recorded against one, its due amount is settled business and shouldn't shift underneath it.
            var installments = await _db.LoanInstallments
                .Include(i => i.Loan)
                .Where(i => i.MonthlyCycleId == cycle.MonthlyCycleId && i.PaidAmount == 0)
                .ToListAsync();
            if (installments.Count == 0) return 0;

            var loanIds = installments.Select(i => i.LoanId).Distinct().ToList();
            var previousByLoan = (await _db.LoanInstallments
                    .Where(i => loanIds.Contains(i.LoanId) && i.MonthlyCycleId != cycle.MonthlyCycleId)
                    .ToListAsync())
                .GroupBy(i => i.LoanId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.InstallmentNumber).First());

            var rules = await _db.LoanInstallmentRules.Where(r => r.IsActive).ToListAsync();
            var penaltyPercent = await GetSettingDecimalAsync("InstallmentPenaltyPercent", 5m);
            var changed = 0;
            foreach (var installment in installments)
            {
                previousByLoan.TryGetValue(installment.LoanId, out var previous);
                var (penalty, arrears) = ComputeLateCharges(previous, penaltyPercent);
                if (installment.PenaltyAmount == penalty && installment.ArrearsAmount == arrears) continue;

                var loan = installment.Loan;
                var principalBase = Math.Max(0, loan.OutstandingPrincipal - arrears);
                var principal = _installmentCalculationService.CalculateMonthlyPrincipalInstallment(
                    loan.OriginalLoanAmount, principalBase, loan.InstallmentAmount, rules);

                installment.TotalAmount += (penalty - installment.PenaltyAmount) + (arrears - installment.ArrearsAmount) + (principal - installment.PrincipalAmount);
                installment.PenaltyAmount = penalty;
                installment.ArrearsAmount = arrears;
                installment.PrincipalAmount = principal;
                changed++;
            }

            if (changed > 0) await _db.SaveChangesAsync();
            return changed;
        }

        // Rule (b): हफ्ता न भरल्यास शिल्लक कर्ज रकमेवर ५% दांड आकारला जाईल -- if the loan's previous
        // installment wasn't fully paid, this cycle's installment carries a penalty of penaltyPercent%
        // of what was actually left unpaid on that installment (principal + interest + any earlier
        // penalty/arrears, minus what was paid) -- not on the loan's whole remaining balance. A member
        // who's ₹70 short pays a penalty on that ₹70, not on their entire outstanding principal. That
        // same ₹70 shortfall also becomes this cycle's Arrears, due back in full on top of the regular
        // installment.
        private static (decimal Penalty, decimal Arrears) ComputeLateCharges(LoanInstallment? previousInstallment, decimal penaltyPercent)
        {
            if (previousInstallment is null) return (0m, 0m);
            var loanDue = previousInstallment.PrincipalAmount + previousInstallment.InterestAmount
                + previousInstallment.PenaltyAmount + previousInstallment.ArrearsAmount;
            var shortfall = loanDue - previousInstallment.PaidAmount;
            if (shortfall <= 0) return (0m, 0m);

            var penalty = Math.Round(shortfall * penaltyPercent / 100m, 0, MidpointRounding.AwayFromZero);
            return (penalty, shortfall);
        }

        private async Task<decimal> GetSettingDecimalAsync(string key, decimal fallback)
        {
            var value = await _db.AppSettings.Where(s => s.Key == key).Select(s => s.Value).FirstOrDefaultAsync();
            return value is not null && decimal.TryParse(value, out var parsed) ? parsed : fallback;
        }

        public async Task CloseCycleAsync(int cycleId, decimal closingBankBalance)
        {
            var cycle = await _db.MonthlyCycles.FindAsync(cycleId);
            if (cycle is null) throw new InvalidOperationException("Cycle not found.");
            if (cycle.Status != "Open") throw new InvalidOperationException("This cycle is already closed.");

            cycle.Status = "Closed";
            cycle.ClosingBankBalance = closingBankBalance;
            cycle.ClosedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task<List<MonthlyCycleRow>> GetCollectionSheetRowsAsync(int cycleId)
        {
            var cycle = await _db.MonthlyCycles.FindAsync(cycleId);
            if (cycle is null) throw new InvalidOperationException("Cycle not found.");

            var activeMembers = await _db.Members.Where(m => m.Status == "Active").OrderBy(m => m.MemberCode).ToListAsync();
            var sharePayments = await _db.MemberSharePayments.Where(p => p.MonthlyCycleId == cycleId).ToListAsync();
            var installments = await _db.LoanInstallments
                .Include(i => i.Loan)
                .Where(i => i.MonthlyCycleId == cycleId)
                .ToListAsync();

            // Cumulative share total per member across every cycle up to and including this one.
            var cumulativeShares = await _db.MemberSharePayments
                .Where(p => p.MonthlyCycle.Year < cycle.Year
                    || (p.MonthlyCycle.Year == cycle.Year && p.MonthlyCycle.Month <= cycle.Month))
                .GroupBy(p => p.MemberId)
                .Select(g => new { MemberId = g.Key, Total = g.Sum(p => p.ExpectedAmount) })
                .ToDictionaryAsync(x => x.MemberId, x => x.Total);

            var rows = new List<MonthlyCycleRow>();
            foreach (var member in activeMembers)
            {
                var sharePayment = sharePayments.FirstOrDefault(p => p.MemberId == member.MemberId);
                // A member can have two installments in the same cycle if an old loan is paid off
                // and a new one is disbursed the same month; show the newer loan's installment,
                // matching what the club's paper register shows for that row.
                var installment = installments
                    .Where(i => i.Loan.MemberId == member.MemberId)
                    .OrderByDescending(i => i.Loan.LoanDate)
                    .FirstOrDefault();
                var loan = installment?.Loan;

                rows.Add(new MonthlyCycleRow
                {
                    MemberId = member.MemberId,
                    MemberCode = member.MemberCode,
                    FullName = member.FullName,
                    FullNameMarathi = member.FullNameMarathi,
                    ShareExpected = sharePayment?.ExpectedAmount ?? member.MonthlyShareAmount,
                    SharePaid = sharePayment?.PaidAmount ?? 0,
                    ShareStatus = sharePayment?.Status ?? "Pending",
                    TotalShare = cumulativeShares.TryGetValue(member.MemberId, out var total)
                        ? total
                        : sharePayment?.ExpectedAmount ?? member.MonthlyShareAmount,
                    HasActiveLoan = loan is not null,
                    LoanId = loan?.LoanId,
                    LoanNumber = loan?.LoanNumber,
                    OpeningPrincipal = installment?.OpeningPrincipal ?? loan?.OutstandingPrincipal,
                    PrincipalDue = installment?.PrincipalAmount,
                    InterestDue = installment?.InterestAmount,
                    PenaltyDue = installment?.PenaltyAmount,
                    ArrearsDue = installment?.ArrearsAmount,
                    LoanPaidAmount = installment?.PaidAmount,
                    LoanStatus = installment?.Status,
                    // Projected balance for the sheet: opening principal less this cycle's due
                    // installment and arrears, recomputed fresh every time the sheet is built. This is
                    // deliberately independent of LoanInstallment.ClosingPrincipal, which only
                    // updates once a payment is actually collected -- the sheet is handed out
                    // before collection, so it must show what members owe, not what's been paid.
                    RemainingBalance = loan is null
                        ? null
                        : (installment?.OpeningPrincipal ?? loan.OutstandingPrincipal) - (installment?.PrincipalAmount ?? 0) - (installment?.ArrearsAmount ?? 0)
                });
            }
            return rows;
        }
    }
}
