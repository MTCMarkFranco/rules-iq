using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RulesIQ.IndexerSkill.Services;
using RulesIQ.SharedModels.Models;

namespace RulesIQ.IndexerSkill.Controllers;

[ApiController]
[Route("api")]
public sealed class ExtractRulesController : ControllerBase
{
    private readonly IRuleExtractionService _extractionService;
    private readonly ILogger<ExtractRulesController> _logger;

    public ExtractRulesController(IRuleExtractionService extractionService, ILogger<ExtractRulesController> logger)
    {
        _extractionService = extractionService;
        _logger = logger;
    }

    [HttpPost("extract-rules")]
    public async Task<ActionResult<SkillsetResponse>> ExtractRules(
        [FromBody] SkillsetRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received extraction request with {Count} records", request.Values.Count);

        var response = new SkillsetResponse();

        foreach (var record in request.Values)
        {
            try
            {
                var result = await _extractionService.ExtractRulesAsync(record.Data, cancellationToken);

                var rulesPayload = new IndexRulesPayload
                {
                    HasRules = result.HasRules,
                    WorkflowName = result.Workflow?.WorkflowName,
                    RulesetVersion = result.RulesetVersion,
                    SourceDocumentVersion = result.SourceDocumentVersion,
                    Rules = result.Workflow?.Rules ?? []
                };

                response.Values.Add(new SkillsetResponseRecord
                {
                    RecordId = record.RecordId,
                    Data = new SkillsetResponseData
                    {
                        RulesJson = JsonSerializer.Serialize(rulesPayload),
                        HasRules = result.HasRules,
                        RuleCount = result.Workflow?.Rules.Count ?? 0,
                        WorkflowName = result.Workflow?.WorkflowName
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing record {RecordId}", record.RecordId);
                response.Values.Add(new SkillsetResponseRecord
                {
                    RecordId = record.RecordId,
                    Data = new SkillsetResponseData { HasRules = false, RuleCount = 0 },
                    Errors = [new SkillsetError { Message = ex.Message }]
                });
            }
        }

        return Ok(response);
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow });
}
