namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Maps tags to allowances
/// </summary>
public class TagAllowanceMapping
{
    public int TagId { get; set; }
    public int AllowanceId { get; set; }
    
    // Navigation properties
    public Tag? Tag { get; set; }
    public Allowance? Allowance { get; set; }
}
