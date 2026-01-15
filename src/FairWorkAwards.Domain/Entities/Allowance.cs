namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Represents an allowance (expense or wage-based)
/// </summary>
public class Allowance
{
    public int AllowanceId { get; set; }
    
    public int AwardId { get; set; }
    
    public required string AllowanceCode { get; set; }
    
    public required string AllowanceName { get; set; }
    
    /// <summary>
    /// Type of allowance (EXPENSE, WAGE, FLAT)
    /// </summary>
    public required string AllowanceType { get; set; }
    
    /// <summary>
    /// Amount of the allowance
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Unit (per_hour, per_week, per_occasion, per_km)
    /// </summary>
    public required string Unit { get; set; }
    
    /// <summary>
    /// For wage-based allowances (percentage)
    /// </summary>
    public decimal? RatePercent { get; set; }
    
    /// <summary>
    /// Whether this is an all-purpose allowance
    /// </summary>
    public bool IsAllPurpose { get; set; }
    
    /// <summary>
    /// Award clause reference (e.g., 19.10(b))
    /// </summary>
    public string? ClauseReference { get; set; }
    
    public DateTime OperativeFrom { get; set; }
    
    public DateTime? OperativeTo { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Award? Award { get; set; }
}
