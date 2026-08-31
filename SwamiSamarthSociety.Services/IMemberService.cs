using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Services
{
    public interface IMemberService
    {
        Task<List<Member>> GetAllAsync(bool includeInactive = false);
        Task<Member?> GetByIdAsync(int id);
        Task<decimal> GetCumulativeShareAsync(int memberId);
        Task<Member> CreateAsync(Member member);
        Task UpdateAsync(Member member);
        Task<string> GenerateNextMemberCodeAsync();
    }
}
