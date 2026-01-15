using FairWorkAwards.Application.DTOs;

namespace FairWorkAwards.Application.Interfaces;

/// <summary>
/// Service responsible for building and generating pay rules
/// </summary>
public interface IRuleBuilderService
{
    /// <summary>
    /// Generates all pay rule combinations for an award
    /// </summary>
    Task<int> GeneratePayRulesForAwardAsync(int awardId, DateTime effectiveFrom, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Regenerates all rules for all awards
    /// </summary>
    Task<int> RegenerateAllRulesAsync(DateTime effectiveFrom, CancellationToken cancellationToken = default);
}
