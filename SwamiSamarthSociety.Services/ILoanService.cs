using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Services
{
    public interface ILoanService
    {
        Task<List<Loan>> GetAllAsync();
        Task<Loan?> GetByIdAsync(int id);
        Task<List<Member>> GetBorrowerCandidatesAsync();
        Task<List<Member>> GetGuarantorCandidatesAsync(int borrowerMemberId);
        Task<List<string>> ValidateNewLoanAsync(int memberId, decimal amount, List<int> guarantorMemberIds);
        Task<Loan> CreateAsync(Loan loan, List<int> guarantorMemberIds);
        Task<int> GetGuarantorsRequiredAsync();
    }
}
