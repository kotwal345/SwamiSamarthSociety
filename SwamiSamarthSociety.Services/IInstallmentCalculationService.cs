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
        decimal CalculateMonthlyPrincipalInstallment(
            decimal originalLoanAmount,
            decimal outstandingPrincipal,
            IEnumerable<LoanInstallmentRule> rules);
    }

    public class InstallmentCalculationService : IInstallmentCalculationService
    {
        public decimal CalculateMonthlyPrincipalInstallment(
            decimal originalLoanAmount,
            decimal outstandingPrincipal,
            IEnumerable<LoanInstallmentRule> rules)
        {
            if (outstandingPrincipal <= 0) return 0;

            var rule = rules.FirstOrDefault(r =>
                originalLoanAmount >= r.MinimumAmount && originalLoanAmount <= r.MaximumAmount && r.IsActive);

            if (rule is null)
                throw new InvalidOperationException(
                    $"No installment rule configured for loan amount {originalLoanAmount}. " +
                    "Check LoanInstallmentRule table.");

            decimal installment = rule.NumberOfInstallments.HasValue
                ? originalLoanAmount / rule.NumberOfInstallments.Value
                : originalLoanAmount * (rule.InstallmentPercentage!.Value / 100m);

            // DMI-007: never let an installment overshoot the remaining balance.
            return Math.Min(installment, outstandingPrincipal);
        }
    }
}
