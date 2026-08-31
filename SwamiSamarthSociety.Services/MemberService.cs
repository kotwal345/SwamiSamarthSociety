using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Services
{
    public class MemberService : IMemberService
    {
        private readonly ApplicationDbContext _db;
        public MemberService(ApplicationDbContext db) => _db = db;

        public async Task<List<Member>> GetAllAsync(bool includeInactive = false)
        {
            var query = _db.Members.AsQueryable();
            if (!includeInactive) query = query.Where(m => m.Status == "Active");
            return await query.OrderBy(m => m.MemberId).ToListAsync();
        }

        public Task<Member?> GetByIdAsync(int id) =>
            _db.Members.FirstOrDefaultAsync(m => m.MemberId == id);

        // Cumulative share = sum of every share payment ever recorded for this member.
        // This is why we don't store CumulativeShareAmount as a column (see 02_DATABASE_DESIGN.md).
        public async Task<decimal> GetCumulativeShareAsync(int memberId) =>
            await _db.MemberSharePayments
                .Where(p => p.MemberId == memberId)
                .SumAsync(p => (decimal?)p.PaidAmount) ?? 0m;

        public async Task<Member> CreateAsync(Member member)
        {
            member.MemberCode = await GenerateNextMemberCodeAsync();
            member.CreatedDate = DateTime.UtcNow;
            _db.Members.Add(member);
            await _db.SaveChangesAsync();
            return member;
        }

        public async Task UpdateAsync(Member member)
        {
            member.ModifiedDate = DateTime.UtcNow;
            _db.Members.Update(member);
            await _db.SaveChangesAsync();
        }

        public async Task<string> GenerateNextMemberCodeAsync()
        {
            var count = await _db.Members.CountAsync();
            return $"MEM{(count + 1):D3}"; // MEM001, MEM002, ...
        }
    }
}
