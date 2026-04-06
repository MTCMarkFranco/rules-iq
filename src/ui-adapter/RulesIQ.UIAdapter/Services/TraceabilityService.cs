using RulesIQ.SharedModels.Models;

namespace RulesIQ.UIAdapter.Services;

public interface ITraceabilityService
{
    List<RuleTraceability> BuildTraceability(
        NormalizedWorkflow workflow,
        string uiBaseUrl = "https://contoso.com/docs/view");
}

public sealed class TraceabilityService : ITraceabilityService
{
    public List<RuleTraceability> BuildTraceability(
        NormalizedWorkflow workflow,
        string uiBaseUrl = "https://contoso.com/docs/view")
    {
        return workflow.Rules.Select(rule =>
        {
            var sourceLinks = rule.Metadata.SourceDocuments.Select(sd =>
            {
                var urlParams = new List<string> { $"docId={Uri.EscapeDataString(sd.SourceDocumentId)}" };
                if (sd.PageNumber.HasValue) urlParams.Add($"page={sd.PageNumber}");
                if (sd.CharRange is not null)
                {
                    urlParams.Add($"start={sd.CharRange.Start}");
                    urlParams.Add($"end={sd.CharRange.End}");
                }

                return new SourceLink
                {
                    Label = $"{sd.SourceDocumentId} - Page {sd.PageNumber ?? 0}",
                    Url = $"{uiBaseUrl}?{string.Join("&", urlParams)}",
                    PageNumber = sd.PageNumber,
                    Snippet = "[Source text available via document link]"
                };
            }).ToList();

            var docCount = rule.Metadata.SourceDocuments.Count;
            var summary = $"This rule was derived from {docCount} source document(s).";

            return new RuleTraceability
            {
                RuleName = rule.RuleName,
                DisplayLabel = FormatDisplayLabel(rule.RuleName, rule.Expression),
                SourceLinks = sourceLinks,
                TraceabilitySummary = summary,
                RulesetVersion = workflow.RulesetVersion,
                RulesetPublishedTimestamp = workflow.RulesetPublishedTimestamp
            };
        }).ToList();
    }

    private static string FormatDisplayLabel(string ruleName, string expression)
    {
        var words = System.Text.RegularExpressions.Regex.Replace(ruleName, "(\\B[A-Z])", " $1");
        return words;
    }
}
