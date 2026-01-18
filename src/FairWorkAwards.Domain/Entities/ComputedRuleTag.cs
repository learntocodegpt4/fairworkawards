namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Links tags to computed rules
/// </summary>
public class ComputedRuleTag
{
    public long RuleId { get; set; }
    public int TagId { get; set; }
    
    // Navigation properties
    public ComputedPayRule? Rule { get; set; }
    public Tag? Tag { get; set; }
}
