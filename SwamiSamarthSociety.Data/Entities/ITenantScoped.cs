namespace SwamiSamarthSociety.Data.Entities
{
    // Marker interface for every entity that belongs to exactly one Society.
    // ApplicationDbContext.OnModelCreating loops over all ITenantScoped entity types
    // to apply the tenant-isolation query filter in one place instead of one per entity.
    public interface ITenantScoped
    {
        int SocietyId { get; set; }
    }
}
