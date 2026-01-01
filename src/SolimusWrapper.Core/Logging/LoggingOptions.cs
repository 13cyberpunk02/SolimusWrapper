namespace SolimusWrapper.Core.Logging;

/// <summary>
/// Настройки логирования команд
/// </summary>
public sealed class LoggingOptions
{
    /// <summary>
    /// Логировать запуск команды
    /// </summary>
    public bool LogCommandStart { get; set; } = true;

    /// <summary>
    /// Логировать завершение команды
    /// </summary>
    public bool LogCommandEnd { get; set; } = true;

    /// <summary>
    /// Уровень логирования для stdout
    /// </summary>
    public CommandLogLevel StandardOutputLevel { get; set; } = CommandLogLevel.Debug;

    /// <summary>
    /// Уровень логирования для stderr
    /// </summary>
    public CommandLogLevel StandardErrorLevel { get; set; } = CommandLogLevel.Warning;

    /// <summary>
    /// Уровень логирования для запуска/завершения команды
    /// </summary>
    public CommandLogLevel CommandLevel { get; set; } = CommandLogLevel.Information;

    /// <summary>
    /// Маскировать чувствительные данные в логах
    /// </summary>
    public bool MaskSensitiveData { get; set; } = true;

    /// <summary>
    /// Паттерны для маскирования (регулярные выражения)
    /// </summary>
    public List<string> SensitivePatterns { get; set; } =
    [
        @"(?i)(password|pwd|secret|token|key|apikey|api_key)[\s:=]+\S+",
        @"(?i)bearer\s+\S+",
        @"(?i)basic\s+\S+"
    ];

    /// <summary>
    /// Максимальная длина логируемой строки
    /// </summary>
    public int MaxLineLength { get; set; } = 1000;

    /// <summary>
    /// Включить timestamp в лог
    /// </summary>
    public bool IncludeTimestamp { get; set; } = true;

    /// <summary>
    /// Формат timestamp
    /// </summary>
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
}