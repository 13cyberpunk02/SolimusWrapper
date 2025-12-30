using System.Text;

namespace SolimusWrapper.Core.Builders;

/// <summary>
/// Построитель команды с расширенными возможностями
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

    public CommandBuilder() { }

    public CommandBuilder(string targetFilePath)
    {
        _targetFilePath = targetFilePath;
    }

    #region Target

    /// <summary>
    /// Устанавливает исполняемый файл
    /// </summary>
    public CommandBuilder SetTarget(string targetFilePath)
    {
        _targetFilePath = targetFilePath;
        return this;
    }

    #endregion

    #region Arguments

    /// <summary>
    /// Добавляет один аргумент
    /// </summary>
    public CommandBuilder AddArgument(string argument)
    {
        _arguments.Add(argument);
        return this;
    }

    /// <summary>
    /// Добавляет аргумент условно
    /// </summary>
    public CommandBuilder AddArgumentIf(bool condition, string argument)
    {
        if (condition)
            _arguments.Add(argument);
        return this;
    }

    /// <summary>
    /// Добавляет аргумент с значением: --name value
    /// </summary>
    public CommandBuilder AddArgument(string name, string value)
    {
        _arguments.Add(name);
        _arguments.Add(value);
        return this;
    }

    /// <summary>
    /// Добавляет аргумент с значением условно
    /// </summary>
    public CommandBuilder AddArgumentIf(bool condition, string name, string value)
    {
        if (condition)
        {
            _arguments.Add(name);
            _arguments.Add(value);
        }
        return this;
    }

    /// <summary>
    /// Добавляет аргумент если значение не null/empty: --name value
    /// </summary>
    public CommandBuilder AddArgumentIfNotEmpty(string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _arguments.Add(name);
            _arguments.Add(value);
        }
        return this;
    }

    /// <summary>
    /// Добавляет несколько аргументов
    /// </summary>
    public CommandBuilder AddArguments(params string[] arguments)
    {
        _arguments.AddRange(arguments);
        return this;
    }

    /// <summary>
    /// Добавляет аргументы из коллекции
    /// </summary>
    public CommandBuilder AddArguments(IEnumerable<string> arguments)
    {
        _arguments.AddRange(arguments);
        return this;
    }

    /// <summary>
    /// Добавляет флаг (--verbose, -v и т.д.)
    /// </summary>
    public CommandBuilder AddFlag(string flag, bool enabled = true)
    {
        if (enabled)
            _arguments.Add(flag);
        return this;
    }

    /// <summary>
    /// Очищает все аргументы
    /// </summary>
    public CommandBuilder ClearArguments()
    {
        _arguments.Clear();
        return this;
    }

    #endregion

    #region Working Directory

    /// <summary>
    /// Устанавливает рабочую директорию
    /// </summary>
    public CommandBuilder SetWorkingDirectory(string path)
    {
        _workingDirectory = path;
        return this;
    }

    #endregion

    #region Environment Variables

    /// <summary>
    /// Добавляет переменную окружения
    /// </summary>
    public CommandBuilder SetEnvironmentVariable(string name, string? value)
    {
        _environmentVariables[name] = value;
        return this;
    }

    /// <summary>
    /// Добавляет несколько переменных окружения
    /// </summary>
    public CommandBuilder SetEnvironmentVariables(IEnumerable<KeyValuePair<string, string?>> variables)
    {
        foreach (var (key, value) in variables)
            _environmentVariables[key] = value;
        return this;
    }

    /// <summary>
    /// Удаляет переменную окружения (устанавливает null)
    /// </summary>
    public CommandBuilder RemoveEnvironmentVariable(string name)
    {
        _environmentVariables[name] = null;
        return this;
    }

    #endregion

    #region Pipes

    /// <summary>
    /// Устанавливает цель для stdout
    /// </summary>
    public CommandBuilder SetStandardOutput(PipeTarget target)
    {
        _stdOutTarget = target;
        return this;
    }

    /// <summary>
    /// Перенаправляет stdout в StringBuilder
    /// </summary>
    public CommandBuilder SetStandardOutput(StringBuilder sb)
    {
        _stdOutTarget = PipeTarget.ToStringBuilder(sb);
        return this;
    }

    /// <summary>
    /// Перенаправляет stdout в делегат (построчно)
    /// </summary>
    public CommandBuilder SetStandardOutput(Action<string> handler)
    {
        _stdOutTarget = PipeTarget.ToDelegate(handler);
        return this;
    }

    /// <summary>
    /// Перенаправляет stdout в файл
    /// </summary>
    public CommandBuilder SetStandardOutputToFile(string filePath)
    {
        _stdOutTarget = PipeTarget.ToFile(filePath);
        return this;
    }

    /// <summary>
    /// Устанавливает цель для stderr
    /// </summary>
    public CommandBuilder SetStandardError(PipeTarget target)
    {
        _stdErrTarget = target;
        return this;
    }

    /// <summary>
    /// Перенаправляет stderr в StringBuilder
    /// </summary>
    public CommandBuilder SetStandardError(StringBuilder sb)
    {
        _stdErrTarget = PipeTarget.ToStringBuilder(sb);
        return this;
    }

    /// <summary>
    /// Перенаправляет stderr в делегат
    /// </summary>
    public CommandBuilder SetStandardError(Action<string> handler)
    {
        _stdErrTarget = PipeTarget.ToDelegate(handler);
        return this;
    }

    /// <summary>
    /// Объединяет stdout и stderr в одну цель
    /// </summary>
    public CommandBuilder MergeStandardOutputAndError(PipeTarget target)
    {
        _stdOutTarget = target;
        _stdErrTarget = target;
        return this;
    }

    /// <summary>
    /// Устанавливает источник для stdin
    /// </summary>
    public CommandBuilder SetStandardInput(PipeSource source)
    {
        _stdInSource = source;
        return this;
    }

    /// <summary>
    /// Передаёт строку в stdin
    /// </summary>
    public CommandBuilder SetStandardInput(string input)
    {
        _stdInSource = PipeSource.FromString(input);
        return this;
    }

    #endregion

    #region Options

    /// <summary>
    /// Устанавливает кодировку
    /// </summary>
    public CommandBuilder SetEncoding(Encoding encoding)
    {
        _encoding = encoding;
        return this;
    }

    /// <summary>
    /// Включает/выключает выброс исключения при ненулевом exit code
    /// </summary>
    public CommandBuilder SetValidation(bool throwOnNonZeroExitCode)
    {
        _throwOnNonZeroExitCode = throwOnNonZeroExitCode;
        return this;
    }

    /// <summary>
    /// Устанавливает таймаут выполнения
    /// </summary>
    public CommandBuilder SetTimeout(TimeSpan timeout)
    {
        _timeout = timeout;
        return this;
    }

    /// <summary>
    /// Callback при завершении с определённым кодом
    /// </summary>
    public CommandBuilder OnExit(Action<int> callback)
    {
        _onExitCode = callback;
        return this;
    }

    #endregion

    #region Build

    /// <summary>
    /// Валидирует и создаёт команду
    /// </summary>
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
            _onExitCode);
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(_targetFilePath))
            throw new InvalidOperationException("Target file path is required");

        if (_timeout.HasValue && _timeout.Value <= TimeSpan.Zero)
            throw new InvalidOperationException("Timeout must be positive");
    }

    #endregion

    #region Direct Execution (shortcuts)

    /// <summary>
    /// Строит и выполняет команду
    /// </summary>
    public ValueTask<CommandResult> ExecuteAsync(CancellationToken ct = default)
        => Build().ExecuteAsync(ct);

    /// <summary>
    /// Строит, выполняет и возвращает вывод
    /// </summary>
    public ValueTask<string> ExecuteAndReadOutputAsync(CancellationToken ct = default)
        => Build().ExecuteAndReadOutputAsync(ct);

    #endregion
}