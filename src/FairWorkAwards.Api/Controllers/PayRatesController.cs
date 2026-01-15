using Microsoft.AspNetCore.Mvc;
using FairWorkAwards.Application.Interfaces;
using FairWorkAwards.Application.DTOs;

namespace FairWorkAwards.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class PayRatesController : ControllerBase
{
    private readonly IRuleEngineService _ruleEngineService;
    private readonly ILogger<PayRatesController> _logger;
    
    public PayRatesController(
        IRuleEngineService ruleEngineService,
        ILogger<PayRatesController> logger)
    {
        _ruleEngineService = ruleEngineService;
        _logger = logger;
    }
    
    /// <summary>
    /// Calculate pay rates based on provided conditions
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(PayRateCalculationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PayRateCalculationResponse>> CalculatePayRates(
        [FromBody] PayRateCalculationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Calculating pay rates for Award {AwardId}, Employment Type {EmploymentType}, Level {Level}",
                request.AwardId, request.EmploymentTypeCode, request.ClassificationLevel);
                
            var result = await _ruleEngineService.CalculatePayRatesAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid request for pay rate calculation");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating pay rates");
            return StatusCode(500, new { error = "An error occurred while calculating pay rates" });
        }
    }
    
    /// <summary>
    /// Validate pay rate calculation conditions
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ValidationResult>> ValidateConditions(
        [FromBody] PayRateCalculationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ruleEngineService.ValidateConditionsAsync(request, cancellationToken);
        return Ok(result);
    }
}
