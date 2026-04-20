// Application/Abstractions/HanaResiliencePipeline.cs
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.Data.Odbc;

public static class HanaResiliencePipeline
{
    public static ResiliencePipeline<T> Create<T>(ILogger logger)
    {
        var retry = new RetryStrategyOptions<T>
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,

            ShouldHandle = new PredicateBuilder<T>()
                .Handle<OdbcException>()
                .Handle<TimeoutException>()
                .Handle<InvalidOperationException>(),

            OnRetry = args =>
            {
                logger.LogWarning(
                    $"HANA Retry {args.AttemptNumber} after {args.RetryDelay}. Error: {args.Outcome.Exception?.Message}");

                return ValueTask.CompletedTask;
            }
        };

        var timeout = new TimeoutStrategyOptions
        {
            Timeout = TimeSpan.FromSeconds(30),
            OnTimeout = args =>
            {
                logger.LogError(
                    "HANA Timeout after {Timeout}. Operation: {Operation}",
                    args.Timeout,
                    args.Context.OperationKey);
                return ValueTask.CompletedTask;
            }
        };

        var circuitBreaker = new CircuitBreakerStrategyOptions<T>
        {

            FailureRatio = 1,
            MinimumThroughput = 3,
            SamplingDuration = TimeSpan.FromSeconds(10),
            BreakDuration = TimeSpan.FromSeconds(3),

            ShouldHandle = new PredicateBuilder<T>()
                //.Handle<OdbcException>()
                .Handle<Exception>(),

            OnOpened = args =>
            {
                logger.LogError(
                    $"HANA Circuit OPEN for {args.BreakDuration}. Error: {args.Outcome.Exception?.Message}");
                return ValueTask.CompletedTask;
            },

            OnHalfOpened = args =>
            {
                logger.LogWarning("HANA Circuit HALF-OPEN");
                return ValueTask.CompletedTask;
            },

            OnClosed = args =>
            {
                logger.LogInformation("HANA Circuit CLOSED");
                return ValueTask.CompletedTask;
            }
        };

        return new ResiliencePipelineBuilder<T>()
            .AddRetry(retry)
            .AddTimeout(timeout)
            .AddCircuitBreaker(circuitBreaker)
            .Build();
    }
}