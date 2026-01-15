namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Maps tags to penalty rates
/// </summary>
public class TagPenaltyMapping
{
    public int TagId { get; set; }
    public int PenaltyRateId { get; set; }
    
    // Navigation properties
    public Tag? Tag { get; set; }
    public PenaltyRate? PenaltyRate { get; set; }
}
