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
    }
}
