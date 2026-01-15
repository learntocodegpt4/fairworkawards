namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Pre-computed pay rules for fast lookups
/// </summary>
public class ComputedPayRule
{
    public long RuleId { get; set; }
    
    public int AwardId { get; set; }
    
    public int EmploymentTypeId { get; set; }
    
    public int ClassificationId { get; set; }
    
    public int? PenaltyRateId { get; set; }
    
    public decimal BaseHourlyRate { get; set; }
    
    public decimal PenaltyMultiplier { get; set; } = 1.00m;
    
    public decimal CalculatedHourlyRate => BaseHourlyRate * PenaltyMultiplier;
    
    public decimal CalculatedWeeklyRate => BaseHourlyRate * PenaltyMultiplier * 38;
    
    public decimal CalculatedAnnualRate => BaseHourlyRate * PenaltyMultiplier * 38 * 52;
    
    public DateTime EffectiveFrom { get; set; }
    
    public DateTime? EffectiveTo { get; set; }
    
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    public string GeneratedBy { get; set; } = "SYSTEM";
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public Award? Award { get; set; }
    public EmploymentType? EmploymentType { get; set; }
    public Classification? Classification { get; set; }
    public PenaltyRate? PenaltyRate { get; set; }
    public ICollection<ComputedRuleAllowance> RuleAllowances { get; set; } = new List<ComputedRuleAllowance>();
    public ICollection<ComputedRuleTag> RuleTags { get; set; } = new List<ComputedRuleTag>();
}
