namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Represents a tenant organization using the system
/// </summary>
public class Tenant
{
    public int TenantId { get; set; }
    
    public required string Name { get; set; }
    
    public string? ABN { get; set; }
    
    public int IndustryId { get; set; }
    
    public string? PrimaryContactName { get; set; }
    
    public string? PrimaryContactEmail { get; set; }
    
    public string? PhoneNumber { get; set; }
    
    public string? BillingAddress { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Industry? Industry { get; set; }
    public ICollection<TenantAward> TenantAwards { get; set; } = new List<TenantAward>();
}
