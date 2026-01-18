namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Represents penalty rates for overtime, weekends, holidays, etc.
/// </summary>
public class PenaltyRate
{
    public int PenaltyRateId { get; set; }
    
    public int AwardId { get; set; }
    
    public required string PenaltyCode { get; set; }
    
    public required string PenaltyName { get; set; }
    
    /// <summary>
    /// Category (WEEKEND, OVERTIME, SHIFT, HOLIDAY)
    /// </summary>
    public required string PenaltyCategory { get; set; }
    
    /// <summary>
    /// Rate multiplier (e.g., 1.5, 2.0, 2.5, 3.0)
    /// </summary>
    public decimal RateMultiplier { get; set; }
    
    /// <summary>
    /// Applicable days (SAT, SUN, MON-FRI, etc.)
    /// </summary>
    public string? ApplicableDays { get; set; }
    
    /// <summary>
    /// Applicable hours (FIRST_4, AFTER_4, ALL, etc.)
    /// </summary>
    public string? ApplicableHours { get; set; }
    
    /// <summary>
    /// Award clause reference
    /// </summary>
    public string? ClauseReference { get; set; }
    
    public DateTime OperativeFrom { get; set; }
    
    public DateTime? OperativeTo { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Award? Award { get; set; }
}
