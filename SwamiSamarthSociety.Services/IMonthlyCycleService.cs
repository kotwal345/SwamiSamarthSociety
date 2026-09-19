using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Services
{
    public interface IMonthlyCycleService
    {
        Task<List<MonthlyCycle>> GetAllAsync();
        Task<MonthlyCycle?> GetByIdAsync(int id);
        Task<MonthlyCycle?> GetOpenCycleAsync();
        Task<(int Year, int Month)> GetNextCyclePeriodAsync();
        Task<MonthlyCycle> OpenNextCycleAsync(decimal openingBankBalance);
        Task CloseCycleAsync(int cycleId, decimal closingBankBalance);
        Task<List<MonthlyCycleRow>> GetCollectionSheetRowsAsync(int cycleId);

        // Re-evaluates rule (b) -- penalty and arrears -- for every not-yet-paid installment in the
        // currently open cycle, against each loan's previous installment. Needed because an
        // installment's penalty/arrears are normally fixed once at OpenNextCycleAsync time -- this lets
        // the chairman re-run the same rule against an already-open cycle (e.g. right after correcting
        // the previous one) without having to close and reopen it. Returns how many rows changed.
        Task<int> RecalculateOpenCycleDueAsync();
    }
}
