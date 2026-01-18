using Microsoft.EntityFrameworkCore;
using FairWorkAwards.Application.Interfaces;
using FairWorkAwards.Domain.Entities;
using FairWorkAwards.Infrastructure.Data;

namespace FairWorkAwards.Infrastructure.Services;

/// <summary>
/// Implements the rule builder responsible for generating pay rules
/// </summary>
public class RuleBuilderService : IRuleBuilderService
{
    private readonly FairWorkAwardsDbContext _context;
    
    public RuleBuilderService(FairWorkAwardsDbContext context)
    {
        _context = context;
    }
    
    public async Task<int> GeneratePayRulesForAwardAsync(int awardId, DateTime effectiveFrom, CancellationToken cancellationToken = default)
    {
        // Delete existing rules for this award/date combination
        var existingRules = await _context.ComputedPayRules
            .Where(r => r.AwardId == awardId && r.EffectiveFrom == effectiveFrom)
            .ToListAsync(cancellationToken);
            
        if (existingRules.Any())
        {
            _context.ComputedPayRules.RemoveRange(existingRules);
            await _context.SaveChangesAsync(cancellationToken);
        }
        
        // Get all active classifications for this award
        var classifications = await _context.Classifications
            .Where(c => c.AwardId == awardId 
                && c.IsActive 
                && c.OperativeFrom <= effectiveFrom 
                && (c.OperativeTo == null || c.OperativeTo > effectiveFrom))
            .Include(c => c.EmploymentType)
            .ToListAsync(cancellationToken);
            
        if (!classifications.Any())
        {
            return 0;
        }
        
        // Get all active penalty rates for this award
        var penaltyRates = await _context.PenaltyRates
            .Where(p => p.AwardId == awardId 
                && p.IsActive 
                && p.OperativeFrom <= effectiveFrom 
                && (p.OperativeTo == null || p.OperativeTo > effectiveFrom))
            .ToListAsync(cancellationToken);
        
        var newRules = new List<ComputedPayRule>();
        
        // Generate rules for each classification
        foreach (var classification in classifications)
        {
            // Base rate rule (no penalty)
            newRules.Add(new ComputedPayRule
            {
                AwardId = classification.AwardId,
                EmploymentTypeId = classification.EmploymentTypeId,
                ClassificationId = classification.ClassificationId,
                PenaltyRateId = null,
                BaseHourlyRate = classification.BaseHourlyRate,
                PenaltyMultiplier = 1.00m,
                EffectiveFrom = effectiveFrom,
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = "SYSTEM"
            });
            
            // Rules with penalty rates
            foreach (var penalty in penaltyRates)
            {
                newRules.Add(new ComputedPayRule
                {
                    AwardId = classification.AwardId,
                    EmploymentTypeId = classification.EmploymentTypeId,
                    ClassificationId = classification.ClassificationId,
                    PenaltyRateId = penalty.PenaltyRateId,
                    BaseHourlyRate = classification.BaseHourlyRate,
                    PenaltyMultiplier = penalty.RateMultiplier,
                    EffectiveFrom = effectiveFrom,
                    GeneratedAt = DateTime.UtcNow,
                    GeneratedBy = "SYSTEM"
                });
            }
        }
        
        await _context.ComputedPayRules.AddRangeAsync(newRules, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        return newRules.Count;
    }
    
    public async Task<int> RegenerateAllRulesAsync(DateTime effectiveFrom, CancellationToken cancellationToken = default)
    {
        var awards = await _context.Awards
            .Where(a => a.IsActive)
            .Select(a => a.AwardId)
            .ToListAsync(cancellationToken);
            
        int totalGenerated = 0;
        
        foreach (var awardId in awards)
        {
            var count = await GeneratePayRulesForAwardAsync(awardId, effectiveFrom, cancellationToken);
            totalGenerated += count;
        }
        
        return totalGenerated;
    }
}
