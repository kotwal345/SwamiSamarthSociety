using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Services
{
    public class PaymentCollectionService : IPaymentCollectionService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInterestCalculationService _interestCalculationService;
        private readonly IInstallmentCalculationService _installmentCalculationService;

        public PaymentCollectionService(
            ApplicationDbContext db,
            IInterestCalculationService interestCalculationService,
            IInstallmentCalculationService installmentCalculationService)
        {
            _db = db;
            _interestCalculationService = interestCalculationService;
            _installmentCalculationService = installmentCalculationService;
        }

        public async Task<CollectionResult> RecordPaymentAsync(
            int monthlyCycleId,
            int memberId,
            decimal shareAmountPaid,
            decimal? loanAmountPaid,
            DateTime paymentDate,
            string? paymentMethod,
            string? receiptNumber,
            string? createdBy)
        {
            var cycle = await _db.MonthlyCycles.FindAsync(monthlyCycleId);
            if (cycle is null) return CollectionResult.Fail("Cycle not found.");
            if (cycle.Status != "Open") return CollectionResult.Fail("This cycle is closed. Payments can no longer be recorded.");

            var member = await _db.Members.FindAsync(memberId);
            if (member is null || member.Status != "Active") return CollectionResult.Fail("Member not found or not active.");

            if (shareAmountPaid < 0) return CollectionResult.Fail("Share amount cannot be negative.");
            if (loanAmountPaid is < 0) return CollectionResult.Fail("Loan amount cannot be negative.");

            // --- Share payment (upsert, overwrite) ---
            var sharePayment = await _db.MemberSharePayments
                .FirstOrDefaultAsync(p => p.MemberId == memberId && p.MonthlyCycleId == monthlyCycleId);
            if (sharePayment is null)
            {
                sharePayment = new MemberSharePayment
                {
                    MemberId = memberId,
                    MonthlyCycleId = monthlyCycleId,
                    ExpectedAmount = member.MonthlyShareAmount
                };
                _db.MemberSharePayments.Add(sharePayment);
            }
            sharePayment.PaidAmount = shareAmountPaid;
            sharePayment.PaymentDate = paymentDate;
            sharePayment.PaymentMethod = paymentMethod;
            sharePayment.ReceiptNumber = receiptNumber;
            sharePayment.CreatedBy = createdBy;
            sharePayment.Status = shareAmountPaid >= sharePayment.ExpectedAmount && sharePayment.ExpectedAmount > 0
                ? "Paid"
                : shareAmountPaid > 0 ? "PartiallyPaid" : "Pending";

            // --- Loan installment (only if a loan payment amount was submitted) ---
            int? loanId = null;
            if (loanAmountPaid is > 0)
            {
                var loan = await _db.Loans.FirstOrDefaultAsync(l => l.MemberId == memberId && l.Status == "Active");
                if (loan is null) return CollectionResult.Fail("This member has no active loan.");
                loanId = loan.LoanId;

                var installment = await _db.LoanInstallments
                    .FirstOrDefaultAsync(i => i.LoanId == loan.LoanId && i.MonthlyCycleId == monthlyCycleId);
                if (installment is null)
                {
                    // Defensive fallback: loan was created after the cycle opened, so no placeholder exists yet.
                    var rules = await _db.LoanInstallmentRules.Where(r => r.IsActive).ToListAsync();
                    var principalDue = _installmentCalculationService.CalculateMonthlyPrincipalInstallment(
                        loan.OriginalLoanAmount, loan.OutstandingPrincipal, rules);
                    var interestDue = _interestCalculationService.CalculateMonthlyInterest(loan.OutstandingPrincipal, loan.InterestRate);
                    var installmentNumber = await _db.LoanInstallments.CountAsync(i => i.LoanId == loan.LoanId) + 1;

                    installment = new LoanInstallment
                    {
                        LoanId = loan.LoanId,
                        MonthlyCycleId = monthlyCycleId,
                        InstallmentNumber = installmentNumber,
                        DueDate = cycle.EndDate,
                        OpeningPrincipal = loan.OutstandingPrincipal,
                        PrincipalAmount = principalDue,
                        InterestAmount = interestDue,
                        ShareAmount = member.MonthlyShareAmount,
                        TotalAmount = principalDue + interestDue + member.MonthlyShareAmount,
                        ClosingPrincipal = loan.OutstandingPrincipal
                    };
                    _db.LoanInstallments.Add(installment);
                }

                installment.PaidAmount = loanAmountPaid.Value;
                installment.PaymentDate = paymentDate;

                // Delete-then-reinsert keeps LoanPayment rows always summing to installment.PaidAmount,
                // so resubmitting the same row never double-counts.
                var existingPayments = await _db.LoanPayments
                    .Where(p => p.LoanInstallmentId == installment.LoanInstallmentId)
                    .ToListAsync();
                _db.LoanPayments.RemoveRange(existingPayments);
                _db.LoanPayments.Add(new LoanPayment
                {
                    LoanInstallmentId = installment.LoanInstallmentId,
                    Amount = loanAmountPaid.Value,
                    PaymentDate = paymentDate,
                    PaymentMethod = paymentMethod,
                    ReceiptNumber = receiptNumber,
                    CreatedBy = createdBy
                });

                // Interest-first split.
                var interestPaid = Math.Min(installment.PaidAmount, installment.InterestAmount);
                var principalPaid = Math.Min(installment.PaidAmount - interestPaid, installment.PrincipalAmount);
                installment.ClosingPrincipal = installment.OpeningPrincipal - principalPaid;
                installment.Status = interestPaid + principalPaid >= installment.PrincipalAmount + installment.InterestAmount
                    ? "Paid"
                    : installment.PaidAmount > 0 ? "PartiallyPaid" : "Pending";
            }

            await _db.SaveChangesAsync(); // ensure new installment/payment rows have IDs before recompute queries below

            if (loanId is not null)
                await RecalculateLoanTotalsAsync(loanId.Value);

            await RecalculateCycleTotalsAsync(monthlyCycleId);

            await _db.SaveChangesAsync();
            return CollectionResult.Ok();
        }

        private async Task RecalculateLoanTotalsAsync(int loanId)
        {
            var loan = await _db.Loans.FirstAsync(l => l.LoanId == loanId);
            var installments = await _db.LoanInstallments.Where(i => i.LoanId == loanId).ToListAsync();

            decimal totalPrincipalPaid = 0, totalInterestPaid = 0;
            foreach (var installment in installments)
            {
                var interestPaid = Math.Min(installment.PaidAmount, installment.InterestAmount);
                var principalPaid = Math.Min(installment.PaidAmount - interestPaid, installment.PrincipalAmount);
                totalInterestPaid += interestPaid;
                totalPrincipalPaid += principalPaid;
            }

            loan.TotalPrincipalPaid = totalPrincipalPaid;
            loan.TotalInterestPaid = totalInterestPaid;
            loan.OutstandingPrincipal = Math.Max(0, loan.OriginalLoanAmount - totalPrincipalPaid);
            if (loan.OutstandingPrincipal <= 0 && loan.Status == "Active")
                loan.Status = "Completed";
        }

        private async Task RecalculateCycleTotalsAsync(int cycleId)
        {
            var cycle = await _db.MonthlyCycles.FirstAsync(c => c.MonthlyCycleId == cycleId);

            var collectedShare = await _db.MemberSharePayments
                .Where(p => p.MonthlyCycleId == cycleId)
                .SumAsync(p => (decimal?)p.PaidAmount) ?? 0m;

            var installments = await _db.LoanInstallments.Where(i => i.MonthlyCycleId == cycleId).ToListAsync();
            decimal totalPrincipal = 0, totalInterest = 0;
            foreach (var installment in installments)
            {
                var interestPaid = Math.Min(installment.PaidAmount, installment.InterestAmount);
                var principalPaid = Math.Min(installment.PaidAmount - interestPaid, installment.PrincipalAmount);
                totalInterest += interestPaid;
                totalPrincipal += principalPaid;
            }

            cycle.CollectedShareAmount = collectedShare;
            cycle.PendingShareAmount = cycle.ExpectedShareAmount - collectedShare;
            cycle.TotalPrincipalCollected = totalPrincipal;
            cycle.TotalInterestCollected = totalInterest;
        }
    }
}
