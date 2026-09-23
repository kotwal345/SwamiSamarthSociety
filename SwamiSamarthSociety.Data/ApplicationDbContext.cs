using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SwamiSamarthSociety.Data.Entities;

namespace SwamiSamarthSociety.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentSocietyContext _tenant;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentSocietyContext tenant) : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Society> Societies => Set<Society>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MonthlyCycle> MonthlyCycles => Set<MonthlyCycle>();
    public DbSet<MemberSharePayment> MemberSharePayments => Set<MemberSharePayment>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanGuarantor> LoanGuarantors => Set<LoanGuarantor>();
    public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
    public DbSet<LoanPayment> LoanPayments => Set<LoanPayment>();
    public DbSet<LoanInstallmentRule> LoanInstallmentRules => Set<LoanInstallmentRule>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions => Set<BankTransaction>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<MemberExitSettlement> MemberExitSettlements => Set<MemberExitSettlement>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ImportLog> ImportLogs => Set<ImportLog>();
    public DbSet<SmsLog> SmsLogs => Set<SmsLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Map entities to the actual (singular-named) tables that already exist in the database.
        // Without this, EF Core defaults to the DbSet property names (e.g. "Members", "Loans")
        // which don't match your existing schema and cause "Invalid object name" errors.
        b.Entity<Society>().ToTable("Society");
        b.Entity<Member>().ToTable("Member");
        b.Entity<MonthlyCycle>().ToTable("MonthlyCycle");
        b.Entity<MemberSharePayment>().ToTable("MemberSharePayment");
        b.Entity<Loan>().ToTable("Loan");
        b.Entity<LoanGuarantor>().ToTable("LoanGuarantor");
        b.Entity<LoanInstallment>().ToTable("LoanInstallment");
        b.Entity<LoanPayment>().ToTable("LoanPayment");
        b.Entity<LoanInstallmentRule>().ToTable("LoanInstallmentRule");
        b.Entity<AppSetting>().ToTable("AppSetting");
        b.Entity<BankAccount>().ToTable("BankAccount");
        b.Entity<BankTransaction>().ToTable("BankTransaction");
        b.Entity<FinancialTransaction>().ToTable("FinancialTransaction");
        b.Entity<MemberExitSettlement>().ToTable("MemberExitSettlement");
        b.Entity<AuditLog>().ToTable("AuditLog");
        b.Entity<ImportLog>().ToTable("ImportLog");
        b.Entity<SmsLog>().ToTable("SmsLog");
        b.Entity<SmsLog>().HasOne(s => s.Member).WithMany().HasForeignKey(s => s.MemberId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Society>().HasIndex(s => s.Code).IsUnique();

        // A user belongs to exactly one Society (SocietyAdmin/Member) or none (SuperAdmin).
        b.Entity<ApplicationUser>().HasOne(u => u.Society).WithMany()
            .HasForeignKey(u => u.SocietyId).OnDelete(DeleteBehavior.Restrict);

        // Composite-per-tenant uniqueness: the same code/number/key can be reused across
        // different societies, but must be unique within any one society.
        b.Entity<Member>().HasIndex(m => new { m.SocietyId, m.MemberCode }).IsUnique();
        b.Entity<MonthlyCycle>().HasIndex(c => new { c.SocietyId, c.Year, c.Month }).IsUnique();
        b.Entity<Loan>().HasIndex(l => new { l.SocietyId, l.LoanNumber }).IsUnique();
        b.Entity<AppSetting>().HasIndex(s => new { s.SocietyId, s.Key }).IsUnique();

        b.Entity<MemberSharePayment>().HasKey(p => p.SharePaymentId);
        b.Entity<MemberSharePayment>().HasIndex(p => new { p.MemberId, p.MonthlyCycleId }).IsUnique();
        b.Entity<MemberSharePayment>().HasOne(p => p.Member).WithMany(m => m.SharePayments).HasForeignKey(p => p.MemberId);
        b.Entity<MemberSharePayment>().HasOne(p => p.MonthlyCycle).WithMany(c => c.SharePayments).HasForeignKey(p => p.MonthlyCycleId);

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

        // Tenant isolation: every ITenantScoped entity is filtered to the current society on
        // every query. SuperAdmin (no SocietyId) sees everything by default; call sites that
        // need to look at one specific society while signed in as SuperAdmin use
        // IgnoreQueryFilters().Where(x => x.SocietyId == id) explicitly instead of relying on
        // this ambient bypass everywhere.
        foreach (var entityType in b.Model.GetEntityTypes())
        {
            if (!typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType)) continue;

            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var societyIdProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(ITenantScoped.SocietyId));
            var tenantSocietyId = System.Linq.Expressions.Expression.Property(
                System.Linq.Expressions.Expression.Constant(_tenant), nameof(ICurrentSocietyContext.SocietyId));
            var isSuperAdmin = System.Linq.Expressions.Expression.Property(
                System.Linq.Expressions.Expression.Constant(_tenant), nameof(ICurrentSocietyContext.IsSuperAdmin));

            // e => _tenant.IsSuperAdmin || e.SocietyId == _tenant.SocietyId
            var matchesSociety = System.Linq.Expressions.Expression.Equal(
                System.Linq.Expressions.Expression.Convert(societyIdProperty, typeof(int?)), tenantSocietyId);
            var body = System.Linq.Expressions.Expression.OrElse(isSuperAdmin, matchesSociety);
            var lambda = System.Linq.Expressions.Expression.Lambda(body, parameter);

            b.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }

        // Deliberately NOT filtering ApplicationUser by tenant: ASP.NET Core Identity's own
        // internal sign-in/password/claims machinery (SignInManager, UserManager, the scaffolded
        // Manage pages) queries the Users DbSet directly and can't be made to pass
        // IgnoreQueryFilters(). A global filter here would silently break login for every
        // non-SuperAdmin account, since there's no ambient tenant yet at the point a user is
        // being looked up by email/username during sign-in. Anywhere that needs "just this
        // society's users" (e.g. UsersController.Index) filters explicitly instead.

        foreach (var property in b.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetColumnType("decimal(18,2)");
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTenantIds();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTenantIds();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Auto-stamps SocietyId on every newly-added tenant-scoped row from the ambient tenant
    // context, so services don't need to set it manually at every insert call site. Rows
    // created by SuperAdmin flows (e.g. provisioning a new Society's first user) must set
    // SocietyId explicitly themselves, since there's no ambient society to stamp from.
    private void StampTenantIds()
    {
        if (_tenant.SocietyId is not { } societyId) return;

        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added && entry.Entity.SocietyId == 0)
                entry.Entity.SocietyId = societyId;
        }
    }
}
