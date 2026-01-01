using System.Diagnostics;
using System.Text;
using SolimusWrapper.Core.Logging;

namespace SolimusWrapper.Core;

/// <summary>
/// Представляет команду для выполнения
/// </summary>
public sealed class Command
{
    private readonly string _targetFilePath;
    private readonly IReadOnlyList<string> _arguments;
    private readonly string? _workingDirectory;
    private readonly IReadOnlyDictionary<string, string?>? _environmentVariables;
    private readonly PipeTarget _stdOutTarget;
    private readonly PipeTarget _stdErrTarget;
    private readonly PipeSource? _stdInSource;
    private readonly Encoding _encoding;
    private readonly bool _throwOnNonZeroExitCode;
    private readonly TimeSpan? _timeout;
    private readonly Action<int>? _onExitCode;
    private readonly ICommandLogger? _logger;  // ← Новое поле

    private Command(
        string targetFilePath,
        IReadOnlyList<string>? arguments = null,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string?>? environmentVariables = null,
        PipeTarget? stdOutTarget = null,
        PipeTarget? stdErrTarget = null,
        PipeSource? stdInSource = null,
        Encoding? encoding = null,
        bool throwOnNonZeroExitCode = true,
        TimeSpan? timeout = null,
        Action<int>? onExitCode = null,
        ICommandLogger? logger = null)  // ← Новый параметр
    {
        _targetFilePath = targetFilePath;
        _arguments = arguments ?? [];
        _workingDirectory = workingDirectory;
        _environmentVariables = environmentVariables;
        _stdOutTarget = stdOutTarget ?? PipeTarget.Null;
        _stdErrTarget = stdErrTarget ?? PipeTarget.Null;
        _stdInSource = stdInSource;
        _encoding = encoding ?? Encoding.UTF8;
        _throwOnNonZeroExitCode = throwOnNonZeroExitCode;
        _timeout = timeout;
        _onExitCode = onExitCode;
        _logger = logger;
    }

    /// <summary>
    /// Создаёт новую команду
    /// </summary>
    public static Command Run(string targetFilePath) => new(targetFilePath);

    /// <summary>
    /// Создаёт команду с полными параметрами (для CommandBuilder)
    /// </summary>
    internal static Command Create(
        string targetFilePath,
        IReadOnlyList<string>? arguments = null,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string?>? environmentVariables = null,
        PipeTarget? stdOutTarget = null,
        PipeTarget? stdErrTarget = null,
        PipeSource? stdInSource = null,
        Encoding? encoding = null,
        bool throwOnNonZeroExitCode = true,
        TimeSpan? timeout = null,
        Action<int>? onExitCode = null,
        ICommandLogger? logger = null)
    {
        return new Command(
            targetFilePath,
            arguments,
            workingDirectory,
            environmentVariables,
            stdOutTarget,
            stdErrTarget,
            stdInSource,
            encoding,
            throwOnNonZeroExitCode,
            timeout,
            onExitCode,
            logger);
    }

    #region Clone Helper

    private Command Clone(
        string? targetFilePath = null,
        IReadOnlyList<string>? arguments = null,
        Optional<string?> workingDirectory = default,
        Optional<IReadOnlyDictionary<string, string?>?> environmentVariables = default,
        PipeTarget? stdOutTarget = null,
        PipeTarget? stdErrTarget = null,
        Optional<PipeSource?> stdInSource = default,
        Encoding? encoding = null,
        bool? throwOnNonZeroExitCode = null,
        Optional<TimeSpan?> timeout = default,
        Optional<Action<int>?> onExitCode = default,
        Optional<ICommandLogger?> logger = default)
    {
        return new Command(
            targetFilePath ?? _targetFilePath,
            arguments ?? _arguments,
            workingDirectory.HasValue ? workingDirectory.Value : _workingDirectory,
            environmentVariables.HasValue ? environmentVariables.Value : _environmentVariables,
            stdOutTarget ?? _stdOutTarget,
            stdErrTarget ?? _stdErrTarget,
            stdInSource.HasValue ? stdInSource.Value : _stdInSource,
            encoding ?? _encoding,
            throwOnNonZeroExitCode ?? _throwOnNonZeroExitCode,
            timeout.HasValue ? timeout.Value : _timeout,
            onExitCode.HasValue ? onExitCode.Value : _onExitCode,
            logger.HasValue ? logger.Value : _logger);
    }

    #endregion

    #region Fluent Builder Methods

    public Command WithArguments(params string[] args) =>
        Clone(arguments: args);

    public Command WithArguments(IEnumerable<string> args) =>
        Clone(arguments: args.ToArray());

    public Command WithWorkingDirectory(string path) =>
        Clone(workingDirectory: path);

    public Command WithEnvironmentVariable(string name, string? value)
    {
        var env = _environmentVariables is null
            ? new Dictionary<string, string?>()
            : new Dictionary<string, string?>(_environmentVariables);

        env[name] = value;

        return Clone(environmentVariables: env);
    }

    public Command WithEnvironmentVariables(IEnumerable<KeyValuePair<string, string?>> variables)
    {
        var env = _environmentVariables is null
            ? new Dictionary<string, string?>()
            : new Dictionary<string, string?>(_environmentVariables);

        foreach (var (key, value) in variables)
            env[key] = value;

        return Clone(environmentVariables: env);
    }

    public Command WithStandardOutputPipe(PipeTarget target) =>
        Clone(stdOutTarget: target);

    public Command WithStandardErrorPipe(PipeTarget target) =>
        Clone(stdErrTarget: target);

    public Command WithStandardInputPipe(PipeSource source) =>
        Clone(stdInSource: source);

    public Command WithEncoding(Encoding encoding) =>
        Clone(encoding: encoding);

    public Command WithValidation(bool throwOnNonZero) =>
        Clone(throwOnNonZeroExitCode: throwOnNonZero);

    public Command WithTimeout(TimeSpan timeout) =>
        Clone(timeout: timeout);

    public Command OnExit(Action<int> callback) =>
        Clone(onExitCode: callback);

    #endregion

    #region Safe Arguments

    /// <summary>
    /// Устанавливает аргументы с автоматическим экранированием
    /// </summary>
    public Command WithSafeArguments(params string[] args)
    {
        var escapedArgs = args.Select(ShellEscaper.Escape).ToArray();
        return Clone(arguments: escapedArgs);
    }

    /// <summary>
    /// Устанавливает аргументы с автоматическим экранированием
    /// </summary>
    public Command WithSafeArguments(IEnumerable<string> args)
    {
        var escapedArgs = args.Select(ShellEscaper.Escape).ToArray();
        return Clone(arguments: escapedArgs);
    }

    #endregion

    #region Logging

    /// <summary>
    /// Устанавливает логгер для команды
    /// </summary>
    public Command WithLogger(ICommandLogger logger) =>
        Clone(logger: new Optional<ICommandLogger?>(logger));

    /// <summary>
    /// Устанавливает консольный логгер
    /// </summary>
    public Command WithConsoleLogging() =>
        Clone(logger: new Optional<ICommandLogger?>(new ConsoleCommandLogger()));

    /// <summary>
    /// Устанавливает консольный логгер с настройками
    /// </summary>
    public Command WithConsoleLogging(Action<LoggingOptions> configure)
    {
        var options = new LoggingOptions();
        configure(options);
        return Clone(logger: new Optional<ICommandLogger?>(new ConsoleCommandLogger(options)));
    }

    /// <summary>
    /// Устанавливает файловый логгер
    /// </summary>
    public Command WithFileLogging(string filePath) =>
        Clone(logger: new Optional<ICommandLogger?>(new FileCommandLogger(filePath)));

    /// <summary>
    /// Устанавливает файловый логгер с настройками
    /// </summary>
    public Command WithFileLogging(string filePath, Action<LoggingOptions> configure)
    {
        var options = new LoggingOptions();
        configure(options);
        return Clone(logger: new Optional<ICommandLogger?>(new FileCommandLogger(filePath, options)));
    }

    /// <summary>
    /// Отключает логирование
    /// </summary>
    public Command WithoutLogging() =>
        Clone(logger: new Optional<ICommandLogger?>(null));

    #endregion

    #region Retry Methods

    /// <summary>
    /// Выполняет команду с повторными попытками
    /// </summary>
    public ValueTask<CommandResult> ExecuteWithRetryAsync(
        RetryOptions options,
        CancellationToken ct = default)
        => RetryExecutor.ExecuteWithRetryAsync(this, options, ct);

    /// <summary>
    /// Выполняет команду с повторными попытками (настройка через делегат)
    /// </summary>
    public ValueTask<CommandResult> ExecuteWithRetryAsync(
        Action<RetryOptions> configure,
        CancellationToken ct = default)
    {
        var options = new RetryOptions();
        configure(options);
        return ExecuteWithRetryAsync(options, ct);
    }

    /// <summary>
    /// Выполняет команду с повторными попытками и возвращает вывод
    /// </summary>
    public ValueTask<string> ExecuteWithRetryAndReadOutputAsync(
        RetryOptions options,
        CancellationToken ct = default)
        => RetryExecutor.ExecuteWithRetryAndReadOutputAsync(this, options, ct);

    #endregion

    #region Execution

    /// <summary>
    /// Выполняет команду асинхронно
    /// </summary>
    public async ValueTask<CommandResult> ExecuteAsync(CancellationToken ct = default)
    {
        using var process = CreateProcess();

        using var timeoutCts = _timeout.HasValue
            ? new CancellationTokenSource(_timeout.Value)
            : null;

        using var linkedCts = timeoutCts is not null
            ? CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token)
            : null;

        var effectiveCt = linkedCts?.Token ?? ct;
        var startTime = DateTimeOffset.Now;

        // Логируем запуск
        _logger?.LogCommandStart(new CommandStartInfo(
            _targetFilePath,
            _arguments,
            _workingDirectory,
            startTime));

        if (!process.Start())
        {
            var ex = new InvalidOperationException($"Failed to start process: {_targetFilePath}");
            _logger?.LogError(ex);
            throw ex;
        }

        try
        {
            // Создаём targets с поддержкой логирования
            var stdOutTarget = CreateLoggingTarget(_stdOutTarget, isStdErr: false);
            var stdErrTarget = CreateLoggingTarget(_stdErrTarget, isStdErr: true);

            var tasks = new List<Task>(3)
            {
                stdOutTarget.CopyFromAsync(
                    new StreamReader(process.StandardOutput.BaseStream, _encoding), effectiveCt),
                stdErrTarget.CopyFromAsync(
                    new StreamReader(process.StandardError.BaseStream, _encoding), effectiveCt)
            };

            if (_stdInSource is not null)
            {
                tasks.Add(WriteStandardInputAsync(process, effectiveCt));
            }
            else
            {
                process.StandardInput.Close();
            }

            await Task.WhenAll(tasks);
            await process.WaitForExitAsync(effectiveCt);
        }
        catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true)
        {
            TryKillProcess(process);
            var ex = new TimeoutException($"Command timed out after {_timeout}");
            _logger?.LogError(ex);
            throw ex;
        }
        catch (OperationCanceledException)
        {
            TryKillProcess(process);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex);
            throw;
        }

        if (timeoutCts?.IsCancellationRequested == true)
        {
            var ex = new TimeoutException($"Command timed out after {_timeout}");
            _logger?.LogError(ex);
            throw ex;
        }

        var endTime = DateTimeOffset.Now;

        var result = new CommandResult(
            process.ExitCode,
            startTime,
            endTime);

        // Логируем завершение
        _logger?.LogCommandEnd(new CommandEndInfo(
            _targetFilePath,
            result.ExitCode,
            result.IsSuccess,
            startTime,
            endTime,
            result.RunTime));

        _onExitCode?.Invoke(result.ExitCode);

        if (_throwOnNonZeroExitCode)
            result.EnsureSuccess();

        return result;
    }

    /// <summary>
    /// Выполняет и возвращает stdout как строку
    /// </summary>
    public async ValueTask<string> ExecuteAndReadOutputAsync(CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        await Clone(stdOutTarget: PipeTarget.ToStringBuilder(sb))
            .ExecuteAsync(ct);

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Выполняет и возвращает stdout и stderr
    /// </summary>
    public async ValueTask<(string StdOut, string StdErr)> ExecuteAndReadAllAsync(CancellationToken ct = default)
    {
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        await Clone(
                stdOutTarget: PipeTarget.ToStringBuilder(stdOut),
                stdErrTarget: PipeTarget.ToStringBuilder(stdErr))
            .ExecuteAsync(ct);

        return (stdOut.ToString().TrimEnd(), stdErr.ToString().TrimEnd());
    }

    private PipeTarget CreateLoggingTarget(PipeTarget inner, bool isStdErr)
    {
        if (_logger is null)
            return inner;

        return new LoggingPipeTarget(inner, _logger, isStdErr);
    }

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private async Task WriteStandardInputAsync(Process process, CancellationToken ct)
    {
        await using var writer = new StreamWriter(
            process.StandardInput.BaseStream,
            Utf8NoBom,
            leaveOpen: false);

        await _stdInSource!.CopyToAsync(writer, ct);
    }

    private static void TryKillProcess(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Игнорируем ошибки при убийстве процесса
        }
    }

    #endregion

    #region Process Creation

    private Process CreateProcess()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _targetFilePath,
            WorkingDirectory = _workingDirectory ?? Environment.CurrentDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = _encoding,
            StandardErrorEncoding = _encoding
        };

        foreach (var arg in _arguments)
            startInfo.ArgumentList.Add(arg);

        if (_environmentVariables is not null)
        {
            foreach (var (key, value) in _environmentVariables)
            {
                if (value is null)
                    startInfo.Environment.Remove(key);
                else
                    startInfo.Environment[key] = value;
            }
        }

        return new Process { StartInfo = startInfo };
    }

    #endregion

    #region ToString

    public override string ToString()
    {
        var args = string.Join(" ", _arguments.Select(EscapeArgument));
        return string.IsNullOrEmpty(args) ? _targetFilePath : $"{_targetFilePath} {args}";
    }

    private static string EscapeArgument(string arg) =>
        arg.Contains(' ') || arg.Contains('"') ? $"\"{arg.Replace("\"", "\\\"")}\"" : arg;

    #endregion
}