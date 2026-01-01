using System.Text;
using SolimusWrapper.Core.Logging;

namespace SolimusWrapper.Core.Builders;

/// <summary>
/// Builder команды с расширенными возможностями
/// </summary>
public sealed class CommandBuilder
{
    private string _targetFilePath = string.Empty;
    private readonly List<string> _arguments = [];
    private string? _workingDirectory;
    private readonly Dictionary<string, string?> _environmentVariables = [];
    private PipeTarget _stdOutTarget = PipeTarget.Null;
    private PipeTarget _stdErrTarget = PipeTarget.Null;
    private PipeSource? _stdInSource;
    private Encoding _encoding = Encoding.UTF8;
    private bool _throwOnNonZeroExitCode = true;
    private TimeSpan? _timeout;
    private Action<int>? _onExitCode;
    private ICommandLogger? _logger;

    public CommandBuilder() { }

    public CommandBuilder(string targetFilePath)
    {
        _targetFilePath = targetFilePath;
    }

    #region Target

    public CommandBuilder SetTarget(string targetFilePath)
    {
        _targetFilePath = targetFilePath;
        return this;
    }

    #endregion

    #region Arguments

    public CommandBuilder AddArgument(string argument)
    {
        _arguments.Add(argument);
        return this;
    }

    public CommandBuilder AddArgumentIf(bool condition, string argument)
    {
        if (condition)
            _arguments.Add(argument);
        return this;
    }

    public CommandBuilder AddArgument(string name, string value)
    {
        _arguments.Add(name);
        _arguments.Add(value);
        return this;
    }

    public CommandBuilder AddArgumentIf(bool condition, string name, string value)
    {
        if (condition)
        {
            _arguments.Add(name);
            _arguments.Add(value);
        }
        return this;
    }

    public CommandBuilder AddArgumentIfNotEmpty(string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _arguments.Add(name);
            _arguments.Add(value);
        }
        return this;
    }

    public CommandBuilder AddArguments(params string[] arguments)
    {
        _arguments.AddRange(arguments);
        return this;
    }

    public CommandBuilder AddArguments(IEnumerable<string> arguments)
    {
        _arguments.AddRange(arguments);
        return this;
    }

    public CommandBuilder AddFlag(string flag, bool enabled = true)
    {
        if (enabled)
            _arguments.Add(flag);
        return this;
    }

    public CommandBuilder ClearArguments()
    {
        _arguments.Clear();
        return this;
    }

    /// <summary>
    /// Добавляет аргумент с автоматическим экранированием
    /// </summary>
    public CommandBuilder AddSafeArgument(string argument)
    {
        _arguments.Add(ShellEscaper.Escape(argument));
        return this;
    }

    /// <summary>
    /// Добавляет аргументы с автоматическим экранированием
    /// </summary>
    public CommandBuilder AddSafeArguments(params string[] arguments)
    {
        _arguments.AddRange(arguments.Select(ShellEscaper.Escape));
        return this;
    }

    #endregion

    #region Working Directory

    public CommandBuilder SetWorkingDirectory(string path)
    {
        _workingDirectory = path;
        return this;
    }

    #endregion

    #region Environment Variables

    public CommandBuilder SetEnvironmentVariable(string name, string? value)
    {
        _environmentVariables[name] = value;
        return this;
    }

    public CommandBuilder SetEnvironmentVariables(IEnumerable<KeyValuePair<string, string?>> variables)
    {
        foreach (var (key, value) in variables)
            _environmentVariables[key] = value;
        return this;
    }

    public CommandBuilder RemoveEnvironmentVariable(string name)
    {
        _environmentVariables[name] = null;
        return this;
    }

    #endregion

    #region Pipes

    public CommandBuilder SetStandardOutput(PipeTarget target)
    {
        _stdOutTarget = target;
        return this;
    }

    public CommandBuilder SetStandardOutput(StringBuilder sb)
    {
        _stdOutTarget = PipeTarget.ToStringBuilder(sb);
        return this;
    }

    public CommandBuilder SetStandardOutput(Action<string> handler)
    {
        _stdOutTarget = PipeTarget.ToDelegate(handler);
        return this;
    }

    public CommandBuilder SetStandardOutputToFile(string filePath)
    {
        _stdOutTarget = PipeTarget.ToFile(filePath);
        return this;
    }

    public CommandBuilder SetStandardError(PipeTarget target)
    {
        _stdErrTarget = target;
        return this;
    }

    public CommandBuilder SetStandardError(StringBuilder sb)
    {
        _stdErrTarget = PipeTarget.ToStringBuilder(sb);
        return this;
    }

    public CommandBuilder SetStandardError(Action<string> handler)
    {
        _stdErrTarget = PipeTarget.ToDelegate(handler);
        return this;
    }

    public CommandBuilder MergeStandardOutputAndError(PipeTarget target)
    {
        _stdOutTarget = target;
        _stdErrTarget = target;
        return this;
    }

    public CommandBuilder SetStandardInput(PipeSource source)
    {
        _stdInSource = source;
        return this;
    }

    public CommandBuilder SetStandardInput(string input)
    {
        _stdInSource = PipeSource.FromString(input);
        return this;
    }

    #endregion

    #region Options

    public CommandBuilder SetEncoding(Encoding encoding)
    {
        _encoding = encoding;
        return this;
    }

    public CommandBuilder SetValidation(bool throwOnNonZeroExitCode)
    {
        _throwOnNonZeroExitCode = throwOnNonZeroExitCode;
        return this;
    }

    public CommandBuilder SetTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    public CommandBuilder OnExit(Action<int> callback)
    {
        _onExitCode = callback;
        return this;
    }

    #endregion

    #region Logging

    /// <summary>
    /// Устанавливает логгер
    /// </summary>
    public CommandBuilder SetLogger(ICommandLogger logger)
    {
        _logger = logger;
        return this;
    }

    /// <summary>
    /// Включает консольное логирование
    /// </summary>
    public CommandBuilder UseConsoleLogging()
    {
        _logger = new ConsoleCommandLogger();
        return this;
    }

    /// <summary>
    /// Включает консольное логирование с настройками
    /// </summary>
    public CommandBuilder UseConsoleLogging(Action<LoggingOptions> configure)
    {
        var options = new LoggingOptions();
        configure(options);
        _logger = new ConsoleCommandLogger(options);
        return this;
    }

    /// <summary>
    /// Включает файловое логирование
    /// </summary>
    public CommandBuilder UseFileLogging(string filePath)
    {
        _logger = new FileCommandLogger(filePath);
        return this;
    }

    /// <summary>
    /// Включает файловое логирование с настройками
    /// </summary>
    public CommandBuilder UseFileLogging(string filePath, Action<LoggingOptions> configure)
    {
        var options = new LoggingOptions();
        configure(options);
        _logger = new FileCommandLogger(filePath, options);
        return this;
    }

    #endregion

    #region Build

    public Command Build()
    {
        Validate();

        return Command.Create(
            _targetFilePath,
            _arguments.ToArray(),
            _workingDirectory,
            _environmentVariables.Count > 0 ? _environmentVariables : null,
            _stdOutTarget,
            _stdErrTarget,
            _stdInSource,
            _encoding,
            _throwOnNonZeroExitCode,
            _timeout,
            _onExitCode,
            _logger);
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(_targetFilePath))
            throw new InvalidOperationException("Target file path is required");

        if (_timeout.HasValue && _timeout.Value <= TimeSpan.Zero)
            throw new InvalidOperationException("Timeout must be positive");
    }

    #endregion

    #region Direct Execution

    public ValueTask<CommandResult> ExecuteAsync(CancellationToken ct = default)
        => Build().ExecuteAsync(ct);

    public ValueTask<string> ExecuteAndReadOutputAsync(CancellationToken ct = default)
        => Build().ExecuteAndReadOutputAsync(ct);

    public ValueTask<CommandResult> ExecuteWithRetryAsync(RetryOptions options, CancellationToken ct = default)
        => Build().ExecuteWithRetryAsync(options, ct);

    public ValueTask<CommandResult> ExecuteWithRetryAsync(Action<RetryOptions> configure, CancellationToken ct = default)
        => Build().ExecuteWithRetryAsync(configure, ct);

    #endregion
}