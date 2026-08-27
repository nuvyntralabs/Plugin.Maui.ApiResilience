using System.Collections.Concurrent;
using System.Net.Sockets;

namespace Plugin.Maui.ApiResilience;

internal sealed class ResiliencePipelineFactory
{
    private readonly ConcurrentDictionary<string, ResiliencePipeline<HttpResponseMessage>> _pipelines = new();

    public ResiliencePipeline<HttpResponseMessage> GetPipeline(ApiResilienceOptions options, string scopeKey)
    {
        return _pipelines.GetOrAdd(scopeKey, key => Build(options, key));
    }

    public static string GetScopeKey(ApiResilienceOptions options, HttpRequestMessage request)
    {
        if (options.CircuitBreaker.Scope == CircuitScope.PerHost && request.RequestUri?.Host is { Length: > 0 } host)
        {
            return host;
        }

        return "global";
    }

    private static ResiliencePipeline<HttpResponseMessage> Build(ApiResilienceOptions options, string scopeKey)
    {
        var builder = new ResiliencePipelineBuilder<HttpResponseMessage>();

        if (options.Timeout.Enabled)
        {
            builder.AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = options.Timeout.RequestTimeout,
                OnTimeout = args =>
                {
                    options.Events.OnTimeout?.Invoke(new TimeoutEvent(options.Timeout.RequestTimeout));
                    return default;
                }
            });
        }

        if (options.Retry.Enabled)
        {
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = Math.Max(0, options.Retry.MaxRetryAttempts),
                Delay = options.Retry.Delay,
                MaxDelay = options.Retry.MaxDelay,
                BackoffType = options.Retry.BackoffType,
                UseJitter = options.Retry.UseJitter,
                ShouldHandle = args => ValueTask.FromResult(ShouldRetry(args, options)),
                OnRetry = args =>
                {
                    options.Events.OnRetry?.Invoke(new RetryEvent(
                        args.AttemptNumber,
                        args.RetryDelay,
                        args.Outcome.Result?.StatusCode,
                        args.Outcome.Exception));
                    return default;
                }
            });
        }

        if (options.CircuitBreaker.Enabled)
        {
            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
            {
                FailureRatio = options.CircuitBreaker.FailureRatio,
                MinimumThroughput = options.CircuitBreaker.MinimumThroughput,
                SamplingDuration = options.CircuitBreaker.SamplingDuration,
                BreakDuration = options.CircuitBreaker.BreakDuration,
                ShouldHandle = args => ValueTask.FromResult(ShouldTripCircuit(args, options)),
                OnOpened = args =>
                {
                    options.Events.OnCircuitOpened?.Invoke(new CircuitEvent(scopeKey, options.CircuitBreaker.BreakDuration));
                    return default;
                },
                OnClosed = _ =>
                {
                    options.Events.OnCircuitClosed?.Invoke(new CircuitEvent(scopeKey, options.CircuitBreaker.BreakDuration));
                    return default;
                },
                OnHalfOpened = _ =>
                {
                    options.Events.OnCircuitHalfOpened?.Invoke(new CircuitEvent(scopeKey, options.CircuitBreaker.BreakDuration));
                    return default;
                }
            });
        }

        return builder.Build();
    }

    internal static bool ShouldRetry(RetryPredicateArguments<HttpResponseMessage> args, ApiResilienceOptions options)
    {
        if (args.Outcome.Exception is BrokenCircuitException)
        {
            return false;
        }

        if (args.Outcome.Exception is not null)
        {
            return IsTransientException(args.Outcome.Exception);
        }

        return args.Outcome.Result is not null &&
               options.Retry.TransientStatusCodes.Contains(args.Outcome.Result.StatusCode);
    }

    internal static bool ShouldTripCircuit(CircuitBreakerPredicateArguments<HttpResponseMessage> args, ApiResilienceOptions options)
    {
        if (args.Outcome.Exception is not null)
        {
            return IsTransientException(args.Outcome.Exception);
        }

        return args.Outcome.Result is not null &&
               options.CircuitBreaker.FailureStatusCodes.Contains(args.Outcome.Result.StatusCode);
    }

    internal static bool IsTransientException(Exception exception)
    {
        return exception is HttpRequestException
               or IOException
               or SocketException
               or TimeoutRejectedException
               or TimeoutException
               or TaskCanceledException { InnerException: TimeoutException };
    }
}
