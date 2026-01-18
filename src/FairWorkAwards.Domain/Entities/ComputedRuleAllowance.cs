namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Links allowances to computed rules
/// </summary>
public class ComputedRuleAllowance
{
    public long RuleId { get; set; }
    public int AllowanceId { get; set; }
    public decimal AllowanceAmount { get; set; }
    
    // Navigation properties
    public ComputedPayRule? Rule { get; set; }
    public Allowance? Allowance { get; set; }
}
