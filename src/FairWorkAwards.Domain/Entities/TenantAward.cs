namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Represents the assignment of an award to a tenant
/// </summary>
public class TenantAward
{
    public int TenantAwardId { get; set; }
    
    public int TenantId { get; set; }
    
    public int AwardId { get; set; }
    
    public DateTime EffectiveFrom { get; set; }
    
    public DateTime? EffectiveTo { get; set; }
    
    /// <summary>
    /// JSON configuration for enabled classifications, allowances, etc.
    /// </summary>
    public string? Configuration { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Tenant? Tenant { get; set; }
    public Award? Award { get; set; }
}
