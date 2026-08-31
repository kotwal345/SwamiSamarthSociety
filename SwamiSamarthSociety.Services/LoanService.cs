using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Services
{
    public class LoanService : ILoanService
    {
        private readonly ApplicationDbContext _db;
        private readonly IInstallmentCalculationService _installmentCalculationService;

        public LoanService(ApplicationDbContext db, IInstallmentCalculationService installmentCalculationService)
        {
            _db = db;
            _installmentCalculationService = installmentCalculationService;
        }

        public Task<List<Loan>> GetAllAsync() =>
            _db.Loans.Include(l => l.Member)
                .OrderByDescending(l => l.LoanDate)
                .ToListAsync();

        public Task<Loan?> GetByIdAsync(int id) =>
            _db.Loans
                .Include(l => l.Member)
                .Include(l => l.Guarantors).ThenInclude(g => g.GuarantorMember)
                .Include(l => l.Installments).ThenInclude(i => i.MonthlyCycle)
                .FirstOrDefaultAsync(l => l.LoanId == id);

        public async Task<List<Member>> GetBorrowerCandidatesAsync()
        {
            var membersWithActiveLoan = _db.Loans.Where(l => l.Status == "Active").Select(l => l.MemberId);
            return await _db.Members
                .Where(m => m.Status == "Active" && !membersWithActiveLoan.Contains(m.MemberId))
                .OrderBy(m => m.FullName)
                .ToListAsync();
        }

        public Task<List<Member>> GetGuarantorCandidatesAsync(int borrowerMemberId) =>
            _db.Members
                .Where(m => m.Status == "Active" && m.MemberId != borrowerMemberId)
                .OrderBy(m => m.FullName)
                .ToListAsync();

        public async Task<List<string>> ValidateNewLoanAsync(int memberId, decimal amount, List<int> guarantorMemberIds)
        {
            var errors = new List<string>();

            var member = await _db.Members.FindAsync(memberId);
            if (member is null || member.Status != "Active")
            {
                errors.Add("Selected member is not an active member.");
                return errors; // nothing else meaningful to check without a valid member
            }

            var hasActiveLoan = await _db.Loans.AnyAsync(l => l.MemberId == memberId && l.Status == "Active");
            if (hasActiveLoan)
                errors.Add($"{member.FullName} already has an active loan. It must be fully repaid before a new loan can be created.");

            var maxLoanAmount = await GetSettingDecimalAsync("MaxLoanAmount", 50000m);
            if (amount <= 0)
                errors.Add("Loan amount must be greater than zero.");
            else if (amount > maxLoanAmount)
                errors.Add($"Loan amount cannot exceed the society maximum of ₹{maxLoanAmount:N0}.");

            var rules = await _db.LoanInstallmentRules.Where(r => r.IsActive).ToListAsync();
            if (amount > 0 && !rules.Any(r => amount >= r.MinimumAmount && amount <= r.MaximumAmount))
                errors.Add($"No installment rule is configured for a loan of ₹{amount:N0}. Check the installment rule bands.");

            var requiredGuarantors = await GetSettingIntAsync("GuarantorsRequired", 2);
            var distinctGuarantors = guarantorMemberIds.Distinct().ToList();
            if (distinctGuarantors.Count != guarantorMemberIds.Count)
                errors.Add("The same guarantor was selected more than once.");
            if (distinctGuarantors.Count != requiredGuarantors)
                errors.Add($"Exactly {requiredGuarantors} guarantor(s) must be selected (selected: {distinctGuarantors.Count}).");
            if (distinctGuarantors.Contains(memberId))
                errors.Add("A member cannot guarantee their own loan.");

            if (distinctGuarantors.Count > 0)
            {
                var validGuarantorIds = await _db.Members
                    .Where(m => distinctGuarantors.Contains(m.MemberId) && m.Status == "Active")
                    .Select(m => m.MemberId)
                    .ToListAsync();
                var invalidGuarantors = distinctGuarantors.Except(validGuarantorIds).Except(new[] { memberId });
                if (invalidGuarantors.Any())
                    errors.Add("One or more selected guarantors are not active members.");
            }

            return errors;
        }

        public async Task<Loan> CreateAsync(Loan loan, List<int> guarantorMemberIds)
        {
            var rules = await _db.LoanInstallmentRules.Where(r => r.IsActive).ToListAsync();
            var rule = rules.First(r => loan.OriginalLoanAmount >= r.MinimumAmount && loan.OriginalLoanAmount <= r.MaximumAmount);

            loan.LoanNumber = await GenerateNextLoanNumberAsync();
            loan.InstallmentPercentage = rule.InstallmentPercentage;
            loan.NumberOfInstallments = rule.NumberOfInstallments;
            loan.OutstandingPrincipal = loan.OriginalLoanAmount;
            loan.TotalPrincipalPaid = 0;
            loan.TotalInterestPaid = 0;
            loan.Status = "Active";

            loan.InstallmentAmount = _installmentCalculationService.CalculateMonthlyPrincipalInstallment(
                loan.OriginalLoanAmount, loan.OriginalLoanAmount, rules);

            loan.Guarantors = guarantorMemberIds.Distinct()
                .Select(id => new LoanGuarantor { GuarantorMemberId = id, Status = "Active" })
                .ToList();

            _db.Loans.Add(loan);
            await _db.SaveChangesAsync();
            return loan;
        }

        public Task<int> GetGuarantorsRequiredAsync() => GetSettingIntAsync("GuarantorsRequired", 2);

        private async Task<string> GenerateNextLoanNumberAsync()
        {
            var count = await _db.Loans.CountAsync();
            return $"LN{(count + 1):D3}";
        }

        private async Task<decimal> GetSettingDecimalAsync(string key, decimal fallback)
        {
            var value = await _db.SocietySettings.Where(s => s.Key == key).Select(s => s.Value).FirstOrDefaultAsync();
            return value is not null && decimal.TryParse(value, out var parsed) ? parsed : fallback;
        }

        private async Task<int> GetSettingIntAsync(string key, int fallback)
        {
            var value = await _db.SocietySettings.Where(s => s.Key == key).Select(s => s.Value).FirstOrDefaultAsync();
            return value is not null && int.TryParse(value, out var parsed) ? parsed : fallback;
        }
    }
}
