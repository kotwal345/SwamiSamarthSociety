using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Services
{
    public interface IInstallmentCalculationService
    {
        /// Returns this month's principal installment given the loan's rule band
        /// and how much principal is still outstanding.
        /// Rule (from society rules doc + confirmed by August 2026 data, DMI-007):
        ///   - Percentage-band loans: installment = OriginalLoanAmount * Percentage%,
        ///     capped at OutstandingPrincipal (last installment closes the loan exactly).
        ///   - Flat "2 installments" band (loan <= 5,000): installment = OriginalLoanAmount / 2,
        ///     same cap rule applies to the 2nd installment.
        /// A loan's own Loan.InstallmentAmount, when set, always wins over the rule-band
        /// calculation -- it's how a chairman records a manually renegotiated EMI (e.g. a
        /// loan restructured outside the standard bands).
        decimal CalculateMonthlyPrincipalInstallment(
            decimal originalLoanAmount,
            decimal outstandingPrincipal,
            decimal? customInstallmentAmount,
            IEnumerable<LoanInstallmentRule> rules);
    }

    public class InstallmentCalculationService : IInstallmentCalculationService
    {
        public decimal CalculateMonthlyPrincipalInstallment(
            decimal originalLoanAmount,
            decimal outstandingPrincipal,
            decimal? customInstallmentAmount,
            IEnumerable<LoanInstallmentRule> rules)
        {
            if (outstandingPrincipal <= 0) return 0;

            decimal installment;
            if (customInstallmentAmount is > 0)
            {
                installment = customInstallmentAmount.Value;
            }
            else
            {
                var rule = rules.FirstOrDefault(r =>
                    originalLoanAmount >= r.MinimumAmount && originalLoanAmount <= r.MaximumAmount && r.IsActive);

                if (rule is null)
                    throw new InvalidOperationException(
                        $"No installment rule configured for loan amount {originalLoanAmount}. " +
                        "Check LoanInstallmentRule table.");

                installment = rule.NumberOfInstallments.HasValue
                    ? originalLoanAmount / rule.NumberOfInstallments.Value
                    : originalLoanAmount * (rule.InstallmentPercentage!.Value / 100m);
            }

            // DMI-007: never let an installment overshoot the remaining balance.
            return Math.Min(installment, outstandingPrincipal);
        }
    }
}
