namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Represents an industry type (e.g., Retail, Hospitality)
/// </summary>
public class Industry
{
    public int IndustryId { get; set; }
    
    public required string IndustryCode { get; set; }
    
    public required string Name { get; set; }
    
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Award> Awards { get; set; } = new List<Award>();
}
