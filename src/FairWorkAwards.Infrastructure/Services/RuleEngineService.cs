using Microsoft.EntityFrameworkCore;
using FairWorkAwards.Application.Interfaces;
using FairWorkAwards.Application.DTOs;
using FairWorkAwards.Infrastructure.Data;

namespace FairWorkAwards.Infrastructure.Services;

/// <summary>
/// Implements the rule engine for processing and validating awards data
/// </summary>
public class RuleEngineService : IRuleEngineService
{
    private readonly FairWorkAwardsDbContext _context;
    
    public RuleEngineService(FairWorkAwardsDbContext context)
    {
        _context = context;
    }
    
    public async Task<PayRateCalculationResponse> CalculatePayRatesAsync(
        PayRateCalculationRequest request, 
        CancellationToken cancellationToken = default)
    {
        var effectiveDate = request.EffectiveDate ?? DateTime.Now;
        
        // Validate the request
        var validation = await ValidateConditionsAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Invalid request: {string.Join(", ", validation.Errors)}");
        }
        
        // Get award details
        var award = await _context.Awards
            .FirstOrDefaultAsync(a => a.AwardId == request.AwardId, cancellationToken);
            
        if (award == null)
        {
            throw new InvalidOperationException($"Award {request.AwardId} not found");
        }
        
        // Get employment type
        var employmentType = await _context.EmploymentTypes
            .FirstOrDefaultAsync(et => et.Code == request.EmploymentTypeCode, cancellationToken);
            
        if (employmentType == null)
        {
            throw new InvalidOperationException($"Employment type {request.EmploymentTypeCode} not found");
        }
        
        // Get classification
        var classification = await _context.Classifications
            .FirstOrDefaultAsync(c => 
                c.AwardId == request.AwardId 
                && c.ClassificationLevel == request.ClassificationLevel
                && c.EmploymentTypeId == employmentType.EmploymentTypeId
                && c.OperativeFrom <= effectiveDate
                && (c.OperativeTo == null || c.OperativeTo > effectiveDate),
                cancellationToken);
                
        if (classification == null)
        {
            throw new InvalidOperationException(
                $"Classification level {request.ClassificationLevel} not found for award {request.AwardId}");
        }
        
        // Get penalty rates
        var penaltyRates = await _context.PenaltyRates
            .Where(p => p.AwardId == request.AwardId 
                && p.IsActive
                && p.OperativeFrom <= effectiveDate
                && (p.OperativeTo == null || p.OperativeTo > effectiveDate))
            .ToListAsync(cancellationToken);
            
        // Filter penalty rates by tags if tags are provided
        if (request.TagIds.Any())
        {
            var tagPenaltyIds = await _context.TagPenaltyMappings
                .Where(tp => request.TagIds.Contains(tp.TagId))
                .Select(tp => tp.PenaltyRateId)
                .Distinct()
                .ToListAsync(cancellationToken);
                
            penaltyRates = penaltyRates
                .Where(p => tagPenaltyIds.Contains(p.PenaltyRateId))
                .ToList();
        }
        
        // Get allowances
        var allowances = await _context.Allowances
            .Where(a => request.AllowanceIds.Contains(a.AllowanceId) 
                && a.AwardId == request.AwardId
                && a.IsActive
                && a.OperativeFrom <= effectiveDate
                && (a.OperativeTo == null || a.OperativeTo > effectiveDate))
            .ToListAsync(cancellationToken);
            
        // Get applied tags
        var appliedTags = await _context.Tags
            .Where(t => request.TagIds.Contains(t.TagId))
            .Select(t => t.TagName)
            .ToListAsync(cancellationToken);
        
        // Build response
        var response = new PayRateCalculationResponse
        {
            AwardCode = award.AwardCode,
            AwardName = award.AwardName,
            EmploymentType = new EmploymentTypeDto
            {
                Code = employmentType.Code,
                Name = employmentType.Name
            },
            Classification = new ClassificationDto
            {
                Level = classification.ClassificationLevel,
                Name = classification.ClassificationName
            },
            BasePay = new BasePayDto
            {
                HourlyRate = classification.BaseHourlyRate,
                WeeklyRate = classification.BaseWeeklyRate,
                AnnualRate = classification.BaseAnnualRate ?? 0
            },
            PenaltyRates = penaltyRates.Select(p => new PenaltyRateDto
            {
                Name = p.PenaltyName,
                Multiplier = p.RateMultiplier,
                HourlyRate = classification.BaseHourlyRate * p.RateMultiplier,
                WeeklyRate = classification.BaseHourlyRate * p.RateMultiplier * 38
            }).ToList(),
            Allowances = allowances.Select(a => new AllowanceDto
            {
                Name = a.AllowanceName,
                Amount = a.Amount,
                Unit = a.Unit,
                WeeklyEquivalent = CalculateWeeklyEquivalent(a.Amount, a.Unit)
            }).ToList(),
            AppliedTags = appliedTags,
            EffectiveDate = effectiveDate
        };
        
        // Calculate totals
        response.TotalAllowancesPerWeek = response.Allowances.Sum(a => a.WeeklyEquivalent ?? 0);
        response.TotalWeeklyPay = response.BasePay.WeeklyRate + response.TotalAllowancesPerWeek;
        
        return response;
    }
    
    public async Task<ValidationResult> ValidateConditionsAsync(
        PayRateCalculationRequest request, 
        CancellationToken cancellationToken = default)
    {
        var result = new ValidationResult { IsValid = true };
        
        // Validate award exists
        var awardExists = await _context.Awards
            .AnyAsync(a => a.AwardId == request.AwardId && a.IsActive, cancellationToken);
            
        if (!awardExists)
        {
            result.IsValid = false;
            result.Errors.Add($"Award {request.AwardId} not found or inactive");
        }
        
        // Validate employment type
        if (string.IsNullOrEmpty(request.EmploymentTypeCode))
        {
            result.IsValid = false;
            result.Errors.Add("Employment type code is required");
        }
        else
        {
            var employmentTypeExists = await _context.EmploymentTypes
                .AnyAsync(et => et.Code == request.EmploymentTypeCode && et.IsActive, cancellationToken);
                
            if (!employmentTypeExists)
            {
                result.IsValid = false;
                result.Errors.Add($"Employment type {request.EmploymentTypeCode} not found or inactive");
            }
        }
        
        // Validate classification level
        if (request.ClassificationLevel < 1)
        {
            result.IsValid = false;
            result.Errors.Add("Classification level must be greater than 0");
        }
        
        return result;
    }
    
    private decimal? CalculateWeeklyEquivalent(decimal amount, string unit)
    {
        return unit.ToLower() switch
        {
            "per_hour" => amount * 38,
            "per_week" => amount,
            "per_occasion" => null, // Cannot calculate weekly equivalent
            "per_km" => null, // Cannot calculate weekly equivalent
            _ => null
        };
    }
}
