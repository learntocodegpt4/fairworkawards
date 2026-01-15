using FairWorkAwards.Domain.Entities;

namespace FairWorkAwards.Application.DTOs;

/// <summary>
/// Request for calculating pay rates
/// </summary>
public class PayRateCalculationRequest
{
    public int AwardId { get; set; }
    public string EmploymentTypeCode { get; set; } = string.Empty;
    public int ClassificationLevel { get; set; }
    public List<int> AllowanceIds { get; set; } = new();
    public List<int> TagIds { get; set; } = new();
    public int? TenantId { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

/// <summary>
/// Response with calculated pay rates
/// </summary>
public class PayRateCalculationResponse
{
    public string AwardCode { get; set; } = string.Empty;
    public string AwardName { get; set; } = string.Empty;
    public EmploymentTypeDto EmploymentType { get; set; } = new();
    public ClassificationDto Classification { get; set; } = new();
    public BasePayDto BasePay { get; set; } = new();
    public List<PenaltyRateDto> PenaltyRates { get; set; } = new();
    public List<AllowanceDto> Allowances { get; set; } = new();
    public decimal TotalAllowancesPerWeek { get; set; }
    public decimal TotalWeeklyPay { get; set; }
    public List<string> AppliedTags { get; set; } = new();
    public DateTime EffectiveDate { get; set; }
}

public class EmploymentTypeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class ClassificationDto
{
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class BasePayDto
{
    public decimal HourlyRate { get; set; }
    public decimal WeeklyRate { get; set; }
    public decimal AnnualRate { get; set; }
}

public class PenaltyRateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Multiplier { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal WeeklyRate { get; set; }
}

public class AllowanceDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? WeeklyEquivalent { get; set; }
}
