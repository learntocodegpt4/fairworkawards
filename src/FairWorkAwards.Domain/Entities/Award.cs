namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Represents a Fair Work Award that defines employment conditions for an industry
/// </summary>
public class Award
{
    public int AwardId { get; set; }
    
    /// <summary>
    /// Award code (e.g., MA000004)
    /// </summary>
    public required string AwardCode { get; set; }
    
    /// <summary>
    /// Full name of the award
    /// </summary>
    public required string AwardName { get; set; }
    
    public int IndustryId { get; set; }
    
    public DateTime OperativeFrom { get; set; }
    
    public DateTime? OperativeTo { get; set; }
    
    public int VersionNumber { get; set; } = 1;
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Industry? Industry { get; set; }
    public ICollection<Classification> Classifications { get; set; } = new List<Classification>();
    public ICollection<Allowance> Allowances { get; set; } = new List<Allowance>();
    public ICollection<PenaltyRate> PenaltyRates { get; set; } = new List<PenaltyRate>();
}
