using System.Text;

namespace SolimusWrapper.Core;

/// <summary>
/// Исполнитель команд с поддержкой повторных попыток
/// </summary>
public static class RetryExecutor
{
    private static readonly Random Jitter = new();

    public static async ValueTask<CommandResult> ExecuteWithRetryAsync(
        this Command command,
        RetryOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxAttempts < 1)
            throw new ArgumentException("MaxAttempts must be at least 1", nameof(options));

        var delay = options.Delay;
        Exception? lastException = null;
        int? lastExitCode = null;

        for (int attempt = 1; attempt <= options.MaxAttempts; attempt++)
        {
            try
            {
                var result = await command
                    .WithValidation(false)
                    .ExecuteAsync(ct);

                if (result.IsSuccess)
                    return result;

                lastExitCode = result.ExitCode;

                var shouldRetryExitCode = options.ShouldRetryOnExitCode?.Invoke(result.ExitCode) ?? true;

                if (!shouldRetryExitCode || attempt >= options.MaxAttempts)
                {
                    result.EnsureSuccess();
                }

                NotifyRetry(options, attempt, options.MaxAttempts, null, lastExitCode, delay);

                await WaitBeforeRetryAsync(delay, options.UseJitter, ct);
                delay = CalculateNextDelay(delay, options);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (CommandExecutionException ex)
            {
                lastException = ex;
                lastExitCode = ex.ExitCode;

                var shouldRetryException = options.ShouldRetry?.Invoke(ex) ?? true;
                var shouldRetryExitCode = options.ShouldRetryOnExitCode?.Invoke(ex.ExitCode) ?? true;

                if (!shouldRetryException || !shouldRetryExitCode || attempt >= options.MaxAttempts)
                    throw;

                NotifyRetry(options, attempt, options.MaxAttempts, ex, lastExitCode, delay);

                await WaitBeforeRetryAsync(delay, options.UseJitter, ct);
                delay = CalculateNextDelay(delay, options);
            }
            catch (Exception ex)
            {
                lastException = ex;

                var shouldRetry = options.ShouldRetry?.Invoke(ex) ?? false;

                if (!shouldRetry || attempt >= options.MaxAttempts)
                    throw;

                NotifyRetry(options, attempt, options.MaxAttempts, ex, null, delay);

                await WaitBeforeRetryAsync(delay, options.UseJitter, ct);
                delay = CalculateNextDelay(delay, options);
            }
        }

        throw lastException ?? new InvalidOperationException("Retry failed");
    }

    public static async ValueTask<string> ExecuteWithRetryAndReadOutputAsync(
        this Command command,
        RetryOptions options,
        CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        await command
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(sb))
            .ExecuteWithRetryAsync(options, ct);

        return sb.ToString().TrimEnd();
    }

    private static void NotifyRetry(
        RetryOptions options,
        int attempt,
        int maxAttempts,
        Exception? exception,
        int? exitCode,
        TimeSpan nextDelay)
    {
        options.OnRetry?.Invoke(new RetryAttempt(
            attempt,
            maxAttempts,
            exception,
            exitCode,
            nextDelay));
    }

    private static async Task WaitBeforeRetryAsync(TimeSpan delay, bool useJitter, CancellationToken ct)
    {
        if (delay <= TimeSpan.Zero)
            return;

        var actualDelay = useJitter
            ? TimeSpan.FromMilliseconds(delay.TotalMilliseconds * (0.5 + Jitter.NextDouble()))
            : delay;

        await Task.Delay(actualDelay, ct);
    }

    private static TimeSpan CalculateNextDelay(TimeSpan currentDelay, RetryOptions options)
    {
        var nextDelay = TimeSpan.FromTicks((long)(currentDelay.Ticks * options.BackoffMultiplier));

        return nextDelay > options.MaxDelay ? options.MaxDelay : nextDelay;
    }
}