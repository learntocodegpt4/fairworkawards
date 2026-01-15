namespace FairWorkAwards.Domain.Entities;

/// <summary>
/// Represents employment types (Full-time, Part-time, Casual, Apprentice, Junior)
/// </summary>
public class EmploymentType
{
    public int EmploymentTypeId { get; set; }
    
    /// <summary>
    /// Code (e.g., FT, PT, CAS, APP, JUN)
    /// </summary>
    public required string Code { get; set; }
    
    /// <summary>
    /// Display name
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// Rate type code (AD, JN, AA, AP)
    /// </summary>
    public required string RateTypeCode { get; set; }
    
    /// <summary>
    /// Casual loading percentage (e.g., 25% for casuals)
    /// </summary>
    public decimal? CasualLoadingPercent { get; set; }
    
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Classification> Classifications { get; set; } = new List<Classification>();
}
