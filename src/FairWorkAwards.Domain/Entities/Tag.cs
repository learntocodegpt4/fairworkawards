namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Represents custom tags for grouping penalties and allowances (created by System Admin)
/// </summary>
public class Tag
{
    public int TagId { get; set; }
    
    public required string TagCode { get; set; }
    
    public required string TagName { get; set; }
    
    /// <summary>
    /// Category (SHIFT, ROSTER, ELIGIBILITY)
    /// </summary>
    public required string TagCategory { get; set; }
    
    public string? Description { get; set; }
    
    public bool AffectsPenalties { get; set; }
    
    public bool AffectsAllowances { get; set; }
    
    public int CreatedBy { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<TagPenaltyMapping> TagPenalties { get; set; } = new List<TagPenaltyMapping>();
    public ICollection<TagAllowanceMapping> TagAllowances { get; set; } = new List<TagAllowanceMapping>();
}
