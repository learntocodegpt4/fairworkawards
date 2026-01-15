using FairWorkAwards.Application.DTOs;

namespace FairWorkAwards.Application.Interfaces;

/// <summary>
/// Service responsible for validating and processing awards data
/// </summary>
public interface IRuleEngineService
{
    /// <summary>
    /// Calculates pay rates based on provided conditions
    /// </summary>
    Task<PayRateCalculationResponse> CalculatePayRatesAsync(PayRateCalculationRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validates if the provided conditions are valid
    /// </summary>
    Task<ValidationResult> ValidateConditionsAsync(PayRateCalculationRequest request, CancellationToken cancellationToken = default);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}
