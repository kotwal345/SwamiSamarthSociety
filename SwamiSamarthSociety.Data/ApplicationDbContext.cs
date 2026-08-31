using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Data;

public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<MonthlyCycle> MonthlyCycles => Set<MonthlyCycle>();
    public DbSet<MemberSharePayment> MemberSharePayments => Set<MemberSharePayment>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanGuarantor> LoanGuarantors => Set<LoanGuarantor>();
    public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<LoanInstallmentRule> LoanInstallmentRules => Set<LoanInstallmentRule>();
    public DbSet<SocietySetting> SocietySettings => Set<SocietySetting>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<MemberExitSettlement> MemberExitSettlements => Set<MemberExitSettlement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ImportLog> ImportLogs => Set<ImportLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Map entities to the actual (singular-named) tables that already exist in the database.
        // Without this, EF Core defaults to the DbSet property names (e.g. "Members", "Loans")
        // which don't match your existing schema and cause "Invalid object name" errors.
        b.Entity<Member>().ToTable("Member");
        b.Entity<MonthlyCycle>().ToTable("MonthlyCycle");
        b.Entity<MemberSharePayment>().ToTable("MemberSharePayment");
        b.Entity<Loan>().ToTable("Loan");
        b.Entity<LoanGuarantor>().ToTable("LoanGuarantor");
        b.Entity<LoanInstallment>().ToTable("LoanInstallment");
        b.Entity<LoanPayment>().ToTable("LoanPayment");
        b.Entity<LoanInstallmentRule>().ToTable("LoanInstallmentRule");
        b.Entity<SocietySetting>().ToTable("SocietySetting");
        b.Entity<BankAccount>().ToTable("BankAccount");
        b.Entity<BankTransaction>().ToTable("BankTransaction");
        b.Entity<FinancialTransaction>().ToTable("FinancialTransaction");
        b.Entity<MemberExitSettlement>().ToTable("MemberExitSettlement");
        b.Entity<AuditLog>().ToTable("AuditLog");
        b.Entity<ImportLog>().ToTable("ImportLog");

        b.Entity<Member>().HasIndex(m => m.MemberCode).IsUnique();
        b.Entity<MonthlyCycle>().HasIndex(c => new { c.Year, c.Month }).IsUnique();

        b.Entity<MemberSharePayment>().HasKey(p => p.SharePaymentId);
        b.Entity<MemberSharePayment>().HasIndex(p => new { p.MemberId, p.MonthlyCycleId }).IsUnique();
        b.Entity<MemberSharePayment>().HasOne(p => p.Member).WithMany(m => m.SharePayments).HasForeignKey(p => p.MemberId);
        b.Entity<MemberSharePayment>().HasOne(p => p.MonthlyCycle).WithMany(c => c.SharePayments).HasForeignKey(p => p.MonthlyCycleId);

        b.Entity<Loan>().HasIndex(l => l.LoanNumber).IsUnique();
        b.Entity<Loan>().HasOne(l => l.Member).WithMany(m => m.Loans).HasForeignKey(l => l.MemberId);

        b.Entity<LoanGuarantor>().HasIndex(g => new { g.LoanId, g.GuarantorMemberId }).IsUnique();
        b.Entity<LoanGuarantor>().HasOne(g => g.Loan).WithMany(l => l.Guarantors).HasForeignKey(g => g.LoanId);
        b.Entity<LoanGuarantor>().HasOne(g => g.GuarantorMember).WithMany(m => m.GuarantorFor)
            .HasForeignKey(g => g.GuarantorMemberId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<LoanInstallment>().HasIndex(i => new { i.LoanId, i.MonthlyCycleId }).IsUnique();
        b.Entity<LoanInstallment>().HasOne(i => i.Loan).WithMany(l => l.Installments).HasForeignKey(i => i.LoanId);
        b.Entity<LoanInstallment>().HasOne(i => i.MonthlyCycle).WithMany(c => c.LoanInstallments).HasForeignKey(i => i.MonthlyCycleId);

        b.Entity<BankTransaction>().HasOne(t => t.BankAccount).WithMany(a => a.Transactions).HasForeignKey(t => t.BankAccountId);
        b.Entity<MemberExitSettlement>().HasOne(s => s.Member).WithMany().HasForeignKey(s => s.MemberId);

        foreach (var property in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
    }
}