using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RulesEngine.Models;
using RulesIQ.SharedModels.Models;

namespace RulesIQ.RuntimeEngine.Services;

public interface IRuleEvaluationService
{
    Task<EvaluationResult> EvaluateAsync(
        NormalizedWorkflow workflow,
        object input,
        string evaluationGoal,
        CancellationToken cancellationToken = default);
}

public sealed class RuleEvaluationService : IRuleEvaluationService
{
    private readonly ILogger<RuleEvaluationService> _logger;

    public RuleEvaluationService(ILogger<RuleEvaluationService> logger)
    {
        _logger = logger;
    }

    public async Task<EvaluationResult> EvaluateAsync(
        NormalizedWorkflow workflow,
        object input,
        string evaluationGoal,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Evaluating workflow {WorkflowName} with {RuleCount} rules",
            workflow.WorkflowName, workflow.Rules.Count);

        var rulesEngineWorkflow = ConvertToRulesEngineWorkflow(workflow);
        var rulesEngine = new RulesEngine.RulesEngine([rulesEngineWorkflow]);

        var results = await rulesEngine.ExecuteAllRulesAsync(workflow.WorkflowName, input);

        var ruleSnapshots = new List<RuleSnapshotEntry>();
        var failedRules = new List<FailedRuleEntry>();
        int passed = 0;
        int failed = 0;

        for (int i = 0; i < results.Count; i++)
        {
            var ruleResult = results[i];
            var normalizedRule = workflow.Rules[i];
            var isPassed = ruleResult.IsSuccess;

            ruleSnapshots.Add(new RuleSnapshotEntry
            {
                RuleName = normalizedRule.RuleName,
                Expression = normalizedRule.Expression,
                Result = isPassed ? "Passed" : "Failed",
                SuccessEvent = isPassed ? normalizedRule.SuccessEvent : null,
                ErrorMessage = isPassed ? null : normalizedRule.ErrorMessage
            });

            if (isPassed)
            {
                passed++;
            }
            else
            {
                failed++;
                failedRules.Add(new FailedRuleEntry
                {
                    RuleName = normalizedRule.RuleName,
                    Expression = normalizedRule.Expression,
                    ErrorMessage = normalizedRule.ErrorMessage
                });
            }
        }

        var total = passed + failed;
        var percentage = total > 0 ? Math.Round((decimal)passed / total * 100, 2) : 0m;

        var sourceDocVersions = workflow.Rules
            .SelectMany(r => r.Metadata.SourceDocuments)
            .GroupBy(sd => sd.SourceDocumentId)
            .Select(g => new SourceDocumentVersionEntry
            {
                SourceDocumentId = g.Key,
                SourceDocumentVersion = g.First().SourceDocumentVersion
            })
            .ToList();

        var evaluationResult = new EvaluationResult
        {
            EvaluationId = $"eval-{DateTimeOffset.UtcNow:yyyy-MM-dd}-{Guid.NewGuid():N}"[..32],
            WorkflowName = workflow.WorkflowName,
            EvaluationGoal = evaluationGoal,
            VersionFingerprint = new VersionFingerprint
            {
                RulesetVersion = workflow.RulesetVersion,
                RulesetPublishedTimestamp = workflow.RulesetPublishedTimestamp,
                SourceDocumentVersions = sourceDocVersions,
                EvaluationTimestamp = DateTimeOffset.UtcNow
            },
            ComplianceScore = new ComplianceScore
            {
                TotalRulesEvaluated = total,
                RulesPassed = passed,
                RulesFailed = failed,
                CompliancePercentage = percentage,
                FailedRules = failedRules
            },
            RulesSnapshot = new RulesSnapshot
            {
                WorkflowName = workflow.WorkflowName,
                RulesetVersion = workflow.RulesetVersion,
                Rules = ruleSnapshots
            }
        };

        _logger.LogInformation("Evaluation complete: {Passed}/{Total} passed ({Percentage}%)",
            passed, total, percentage);

        return evaluationResult;
    }

    private static Workflow ConvertToRulesEngineWorkflow(NormalizedWorkflow normalized)
    {
        return new Workflow
        {
            WorkflowName = normalized.WorkflowName,
            Rules = normalized.Rules.Select(r => new Rule
            {
                RuleName = r.RuleName,
                // Map contract convention (input.) to RulesEngine convention (input1.)
                Expression = Regex.Replace(r.Expression, @"\binput\.", "input1."),
                SuccessEvent = r.SuccessEvent,
                ErrorMessage = r.ErrorMessage,
                RuleExpressionType = RuleExpressionType.LambdaExpression
            }).ToList()
        };
    }
}
