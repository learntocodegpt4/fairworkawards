namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Represents a classification level within an award (e.g., Retail Employee Level 1)
/// </summary>
public class Classification
{
    public int ClassificationId { get; set; }
    
    public int AwardId { get; set; }
    
    /// <summary>
    /// Classification level (1-8)
    /// </summary>
    public int ClassificationLevel { get; set; }
    
    /// <summary>
    /// Full classification name
    /// </summary>
    public required string ClassificationName { get; set; }
    
    public int EmploymentTypeId { get; set; }
    
    /// <summary>
    /// Base hourly rate
    /// </summary>
    public decimal BaseHourlyRate { get; set; }
    
    /// <summary>
    /// Base weekly rate (usually 38 hours)
    /// </summary>
    public decimal BaseWeeklyRate { get; set; }
    
    /// <summary>
    /// Base annual rate
    /// </summary>
    public decimal? BaseAnnualRate { get; set; }
    
    public DateTime OperativeFrom { get; set; }
    
    public DateTime? OperativeTo { get; set; }
    
    public int VersionNumber { get; set; } = 1;
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Award? Award { get; set; }
    public EmploymentType? EmploymentType { get; set; }
}
