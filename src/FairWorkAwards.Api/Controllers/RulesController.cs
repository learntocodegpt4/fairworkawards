using Microsoft.AspNetCore.Mvc;
using FairWorkAwards.Application.Interfaces;

namespace FairWorkAwards.Api.Controllers;

[ApiController]
[Route("api/v1/admin/[controller]")]
public class RulesController : ControllerBase
{
    private readonly IRuleBuilderService _ruleBuilderService;
    private readonly ILogger<RulesController> _logger;
    
    public RulesController(
        IRuleBuilderService ruleBuilderService,
        ILogger<RulesController> logger)
    {
        _ruleBuilderService = ruleBuilderService;
        _logger = logger;
    }
    
    /// <summary>
    /// Generate pay rules for a specific award
    /// </summary>
    [HttpPost("generate/{awardId}")]
    [ProducesResponseType(typeof(RuleGenerationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RuleGenerationResponse>> GenerateRulesForAward(
        int awardId,
        [FromQuery] DateTime? effectiveFrom,
        CancellationToken cancellationToken)
    {
        try
        {
            var effective = effectiveFrom ?? DateTime.Now;
            _logger.LogInformation("Generating rules for Award {AwardId} effective from {EffectiveDate}",
                awardId, effective);
                
            var count = await _ruleBuilderService.GeneratePayRulesForAwardAsync(awardId, effective, cancellationToken);
            
            return Ok(new RuleGenerationResponse
            {
                AwardId = awardId,
                GeneratedRulesCount = count,
                EffectiveFrom = effective,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating rules for award {AwardId}", awardId);
            return BadRequest(new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Regenerate all pay rules for all awards
    /// </summary>
    [HttpPost("regenerate-all")]
    [ProducesResponseType(typeof(RuleGenerationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RuleGenerationResponse>> RegenerateAllRules(
        [FromQuery] DateTime? effectiveFrom,
        CancellationToken cancellationToken)
    {
        try
        {
            var effective = effectiveFrom ?? DateTime.Now;
            _logger.LogInformation("Regenerating all rules effective from {EffectiveDate}", effective);
                
            var count = await _ruleBuilderService.RegenerateAllRulesAsync(effective, cancellationToken);
            
            return Ok(new RuleGenerationResponse
            {
                GeneratedRulesCount = count,
                EffectiveFrom = effective,
                GeneratedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error regenerating all rules");
            return StatusCode(500, new { error = "An error occurred while regenerating rules" });
        }
    }
}

public class RuleGenerationResponse
{
    public int? AwardId { get; set; }
    public int GeneratedRulesCount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime GeneratedAt { get; set; }
}
