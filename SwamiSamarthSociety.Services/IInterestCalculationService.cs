using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwamiSamarthSociety.Services
{
    public interface IInterestCalculationService
    {
        /// Interest = PreviousOutstandingPrincipal * (InterestRatePercent / 100)
        /// Rounded to nearest rupee. AMB-1 (rounding mode) unresolved — see DMI-006.
        /// Using MidpointRounding.AwayFromZero as the safe interim default;
        /// swap to .ToEven if the treasurer confirms banker's rounding.
        decimal CalculateMonthlyInterest(decimal previousOutstandingPrincipal, decimal interestRatePercent = 2m);
    }

    public class InterestCalculationService : IInterestCalculationService
    {
        public decimal CalculateMonthlyInterest(decimal previousOutstandingPrincipal, decimal interestRatePercent = 2m)
        {
            var raw = previousOutstandingPrincipal * (interestRatePercent / 100m);
            return Math.Round(raw, 0, MidpointRounding.AwayFromZero);
        }
    }
}
