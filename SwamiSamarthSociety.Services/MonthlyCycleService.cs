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
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

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
            foreach (var loan in activeLoans)
            {
                var principal = _installmentCalculationService.CalculateMonthlyPrincipalInstallment(
                    loan.OriginalLoanAmount, loan.OutstandingPrincipal, rules);
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
                    PenaltyAmount = 0,
                    TotalAmount = principal + interest + loan.Member.MonthlyShareAmount,
                    PaidAmount = 0,
                    ClosingPrincipal = loan.OutstandingPrincipal,
                    Status = "Pending"
                });
            }

            await _db.SaveChangesAsync();
            return cycle;
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
            var activeMembers = await _db.Members.Where(m => m.Status == "Active").OrderBy(m => m.MemberCode).ToListAsync();
            var sharePayments = await _db.MemberSharePayments.Where(p => p.MonthlyCycleId == cycleId).ToListAsync();
            var activeLoans = await _db.Loans.Where(l => l.Status == "Active").ToListAsync();
            var installments = await _db.LoanInstallments.Where(i => i.MonthlyCycleId == cycleId).ToListAsync();

            var rows = new List<MonthlyCycleRow>();
            foreach (var member in activeMembers)
            {
                var sharePayment = sharePayments.FirstOrDefault(p => p.MemberId == member.MemberId);
                var loan = activeLoans.FirstOrDefault(l => l.MemberId == member.MemberId);
                var installment = loan is not null ? installments.FirstOrDefault(i => i.LoanId == loan.LoanId) : null;

                rows.Add(new MonthlyCycleRow
                {
                    MemberId = member.MemberId,
                    MemberCode = member.MemberCode,
                    FullName = member.FullName,
                    ShareExpected = sharePayment?.ExpectedAmount ?? member.MonthlyShareAmount,
                    SharePaid = sharePayment?.PaidAmount ?? 0,
                    ShareStatus = sharePayment?.Status ?? "Pending",
                    HasActiveLoan = loan is not null,
                    LoanId = loan?.LoanId,
                    LoanNumber = loan?.LoanNumber,
                    OpeningPrincipal = installment?.OpeningPrincipal ?? loan?.OutstandingPrincipal,
                    PrincipalDue = installment?.PrincipalAmount,
                    InterestDue = installment?.InterestAmount,
                    LoanPaidAmount = installment?.PaidAmount,
                    LoanStatus = installment?.Status,
                    RemainingBalance = installment?.ClosingPrincipal ?? loan?.OutstandingPrincipal
                });
            }
            return rows;
        }
    }
}
