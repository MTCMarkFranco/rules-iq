using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Microsoft.Extensions.Logging;

namespace RulesIQ.Infrastructure.Resilience;

public static class PollyPolicies
{
    public static ResiliencePipeline<HttpResponseMessage> GetOpenAIResiliencePipeline(ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("OpenAIResilience");
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode == 429 || (int)r.StatusCode >= 500),
                OnRetry = args =>
                {
                    logger.LogWarning("Retry {AttemptNumber} for OpenAI call after {Delay}ms",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(60)
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    public static ResiliencePipeline<HttpResponseMessage> GetSearchResiliencePipeline(ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("SearchResilience");
        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => (int)r.StatusCode == 429 || (int)r.StatusCode >= 500),
                OnRetry = args =>
                {
                    logger.LogWarning("Retry {AttemptNumber} for Search call after {Delay}ms",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .AddTimeout(TimeSpan.FromSeconds(15))
            .Build();
    }
}
