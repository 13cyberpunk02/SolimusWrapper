namespace SolimusWrapper.Core;

/// <summary>
/// Настройки повторных попыток выполнения команды
/// </summary>
public sealed class RetryOptions
{
    /// <summary>
    /// Максимальное количество попыток (включая первую)
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Начальная задержка между попытками
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Множитель для экспоненциального увеличения задержки
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Максимальная задержка между попытками
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Предикат для определения, нужно ли повторять при данном исключении
    /// </summary>
    public Func<Exception, bool>? ShouldRetry { get; set; }

    /// <summary>
    /// Предикат для определения, нужно ли повторять при данном exit code
    /// </summary>
    public Func<int, bool>? ShouldRetryOnExitCode { get; set; }

    /// <summary>
    /// Callback перед каждой повторной попыткой
    /// </summary>
    public Action<RetryAttempt>? OnRetry { get; set; }

    /// <summary>
    /// Добавить случайный jitter к задержке (для избежания thundering herd)
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Создаёт настройки по умолчанию
    /// </summary>
    public static RetryOptions Default => new();

    /// <summary>
    /// Создаёт настройки для экспоненциального backoff
    /// </summary>
    public static RetryOptions Exponential(int maxAttempts = 3, TimeSpan? initialDelay = null)
    {
        return new RetryOptions
        {
            MaxAttempts = maxAttempts,
            Delay = initialDelay ?? TimeSpan.FromSeconds(1),
            BackoffMultiplier = 2.0,
            UseJitter = true
        };
    }

    /// <summary>
    /// Создаёт настройки для линейного backoff
    /// </summary>
    public static RetryOptions Linear(int maxAttempts = 3, TimeSpan? delay = null)
    {
        return new RetryOptions
        {
            MaxAttempts = maxAttempts,
            Delay = delay ?? TimeSpan.FromSeconds(1),
            BackoffMultiplier = 1.0,
            UseJitter = false
        };
    }

    /// <summary>
    /// Создаёт настройки без задержки между попытками
    /// </summary>
    public static RetryOptions Immediate(int maxAttempts = 3)
    {
        return new RetryOptions
        {
            MaxAttempts = maxAttempts,
            Delay = TimeSpan.Zero,
            BackoffMultiplier = 1.0,
            UseJitter = false
        };
    }
}

/// <summary>
/// Информация о попытке выполнения
/// </summary>
public readonly record struct RetryAttempt(
    int AttemptNumber,
    int MaxAttempts,
    Exception? LastException,
    int? LastExitCode,
    TimeSpan NextDelay)
{
    public bool IsLastAttempt => AttemptNumber >= MaxAttempts;
}