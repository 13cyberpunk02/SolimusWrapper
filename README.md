# SolimusWrapper

[![NuGet](https://img.shields.io/nuget/v/SolimusWrapper.svg)](https://www.nuget.org/packages/SolimusWrapper)
[![Downloads](https://img.shields.io/nuget/dt/SolimusWrapper.svg)](https://www.nuget.org/packages/SolimusWrapper)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build and Test](https://github.com/13cyberpunk02/SolimusWrapper/actions/workflows/dotnet.yml/badge.svg)](https://github.com/13cyberpunk02/SolimusWrapper/actions/workflows/dotnet.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)

Лёгкая и оптимизированная библиотека для работы с CLI процессами в .NET. Простой и интуитивный API для запуска внешних команд с поддержкой перенаправления потоков, таймаутов, повторных попыток, логирования и кросс-платформенной работы.

## ✨ Особенности

- 🚀 **Fluent API** — читаемый и удобный синтаксис
- 🔒 **Immutable** — потокобезопасные команды
- ⚡ **Async/Await** — полная асинхронность с CancellationToken
- 🔄 **Pipes** — перенаправление stdout, stderr, stdin
- ⏱️ **Таймауты** — автоматическое завершение процессов
- 🔁 **Retry Logic** — повторные попытки с exponential backoff
- 🛡️ **Shell Escaping** — защита от инъекций команд
- 📝 **Structured Logging** — гибкое логирование в консоль и файлы
- 🌍 **Кросс-платформенность** — Windows, Linux, macOS
- 📦 **Zero Dependencies** — никаких внешних зависимостей
- 🎯 **Native AOT** — поддержка trimming

## 📁 Структура проекта
```
SolimusWrapper/
├── README.md                              # Главная документация
├── LICENSE                                # MIT License
├── SolimusWrapper.sln                       # Solution файл
│
├── src/
│   └── SolimusWrapper.Core/                      # 📦 Основная библиотека
│       ├── Command.cs                     # Основной класс
│       ├── CommandResult.cs               # Результат выполнения
│       ├── CommandExtensions.cs           # Кросс-платформенные хелперы
│       ├── PipeTarget.cs                  # Перенаправление вывода
│       ├── PipeSource.cs                  # Перенаправление ввода
│       ├── RetryOptions.cs                # Настройки повторов
│       ├── RetryExecutor.cs               # Логика повторов
│       ├── ShellEscaper.cs                # Экранирование аргументов
│       ├── SafeCommand.cs                 # Безопасные команды
│       ├── Builders/
│       │   └── CommandBuilder.cs          # Builder pattern
│       └── Logging/
│           ├── ICommandLogger.cs          # Интерфейс логгера
│           ├── LoggingOptions.cs          # Настройки логирования
│           ├── ConsoleCommandLogger.cs    # Логгер в консоль
│           ├── FileCommandLogger.cs       # Логгер в файл
│           ├── CompositeCommandLogger.cs  # Композитный логгер
│           └── NullCommandLogger.cs       # Пустой логгер
│
├── samples/
│   └── SolimusWrapper.Samples/                 # 🎮 Примеры использования
│
└── tests/
    └── SolimusWrapper.Tests/                # 🧪 Unit-тесты
```

## 📦 Установка
```bash
dotnet add package SolimusWrapper
```

Или через Package Manager:
```powershell
Install-Package SolimusWrapper
```

## 🚀 Быстрый старт
```csharp
using SolimusWrapper;

// Простой запуск
var result = await Command.Run("dotnet")
    .WithArguments("--version")
    .ExecuteAsync();

Console.WriteLine($"Exit code: {result.ExitCode}");

// Получить вывод
var output = await Command.Run("git")
    .WithArguments("status")
    .ExecuteAndReadOutputAsync();

Console.WriteLine(output);
```

## 📖 Основные возможности

### Базовые операции
```csharp
// Запуск с аргументами
await Command.Run("dotnet")
    .WithArguments("build", "-c", "Release")
    .ExecuteAsync();

// Аргументы как коллекция
var args = new[] { "test", "--no-build", "--logger", "console" };
await Command.Run("dotnet")
    .WithArguments(args)
    .ExecuteAsync();

// Получить stdout и stderr отдельно
var (stdOut, stdErr) = await Command.Run("dotnet")
    .WithArguments("build")
    .ExecuteAndReadAllAsync();
```

### Перенаправление потоков
```csharp
// В StringBuilder
var output = new StringBuilder();
var errors = new StringBuilder();

await Command.Run("dotnet")
    .WithArguments("build")
    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(errors))
    .ExecuteAsync();

// Вывод в реальном времени
await Command.Run("dotnet")
    .WithArguments("test")
    .WithStandardOutputPipe(PipeTarget.ToDelegate(line => 
        Console.WriteLine($"[OUT] {line}")))
    .WithStandardErrorPipe(PipeTarget.ToDelegate(line => 
        Console.WriteLine($"[ERR] {line}")))
    .ExecuteAsync();

// В файл
await Command.Run("dotnet")
    .WithArguments("build", "-v", "detailed")
    .WithStandardOutputPipe(PipeTarget.ToFile("build.log"))
    .ExecuteAsync();
```

### Передача данных в stdin
```csharp
// Из строки
await Command.Run("grep")
    .WithArguments("error")
    .WithStandardInputPipe(PipeSource.FromString("line1\nerror here\nline3"))
    .ExecuteAsync();

// Из файла
await Command.Run("cat")
    .WithStandardInputPipe(PipeSource.FromFile("input.txt"))
    .ExecuteAsync();

// Из потока
using var stream = File.OpenRead("data.bin");
await Command.Run("processor")
    .WithStandardInputPipe(PipeSource.FromStream(stream))
    .ExecuteAsync();

// Из байтов
var bytes = Encoding.UTF8.GetBytes("data");
await Command.Run("consumer")
    .WithStandardInputPipe(PipeSource.FromBytes(bytes))
    .ExecuteAsync();
```

### Рабочая директория и переменные окружения
```csharp
await Command.Run("npm")
    .WithArguments("install")
    .WithWorkingDirectory("/path/to/project")
    .WithEnvironmentVariable("NODE_ENV", "production")
    .WithEnvironmentVariable("PORT", "3000")
    .ExecuteAsync();

// Несколько переменных сразу
var envVars = new Dictionary<string, string?>
{
    ["API_KEY"] = "secret",
    ["DEBUG"] = "true"
};

await Command.Run("myapp")
    .WithEnvironmentVariables(envVars)
    .ExecuteAsync();
```

### Таймауты и отмена
```csharp
// Таймаут
try
{
    await Command.Run("long-process")
        .WithTimeout(TimeSpan.FromSeconds(30))
        .ExecuteAsync();
}
catch (TimeoutException)
{
    Console.WriteLine("Процесс превысил лимит времени");
}

// Отмена через CancellationToken
using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

try
{
    await Command.Run("long-task")
        .ExecuteAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Операция отменена");
}
```

### Обработка кодов выхода
```csharp
// По умолчанию выбрасывает исключение при ненулевом коде
try
{
    await Command.Run("failing-command").ExecuteAsync();
}
catch (CommandExecutionException ex)
{
    Console.WriteLine($"Команда завершилась с кодом: {ex.ExitCode}");
}

// Отключить проверку
var result = await Command.Run("might-fail")
    .WithValidation(false)
    .ExecuteAsync();

if (!result.IsSuccess)
{
    Console.WriteLine($"Exit code: {result.ExitCode}");
}

// Callback при завершении
await Command.Run("task")
    .OnExit(exitCode => Console.WriteLine($"Finished with: {exitCode}"))
    .WithValidation(false)
    .ExecuteAsync();
```

---

## 🔁 Retry Logic (Повторные попытки)

Автоматические повторы при ошибках с поддержкой exponential backoff:
```csharp
using SolimusWrapper;

// Простой retry с настройками по умолчанию
var result = await Command.Run("curl")
    .WithArguments("https://api.example.com/data")
    .ExecuteWithRetryAsync(RetryOptions.Default);

// Exponential backoff
var result = await Command.Run("flaky-api")
    .ExecuteWithRetryAsync(RetryOptions.Exponential(
        maxAttempts: 5, 
        initialDelay: TimeSpan.FromMilliseconds(500)));

// Linear retry (постоянная задержка)
var result = await Command.Run("service")
    .ExecuteWithRetryAsync(RetryOptions.Linear(
        maxAttempts: 3, 
        delay: TimeSpan.FromSeconds(2)));

// Немедленные повторы без задержки
var result = await Command.Run("quick-check")
    .ExecuteWithRetryAsync(RetryOptions.Immediate(maxAttempts: 3));
```

### Расширенная настройка Retry
```csharp
var result = await Command.Run("network-command")
    .ExecuteWithRetryAsync(options =>
    {
        options.MaxAttempts = 5;
        options.Delay = TimeSpan.FromSeconds(1);
        options.BackoffMultiplier = 2.0;        // Exponential: 1s, 2s, 4s, 8s...
        options.MaxDelay = TimeSpan.FromSeconds(30);  // Максимальная задержка
        options.UseJitter = true;               // Случайное отклонение для избежания thundering herd
        
        // Повторять только при определённых exit codes
        options.ShouldRetryOnExitCode = code => code == 1 || code == 2;
        
        // Повторять только при определённых исключениях
        options.ShouldRetry = ex => ex is CommandExecutionException;
        
        // Callback перед каждым повтором
        options.OnRetry = attempt =>
        {
            Console.WriteLine($"Попытка {attempt.AttemptNumber}/{attempt.MaxAttempts} не удалась.");
            Console.WriteLine($"Причина: {attempt.LastException?.Message ?? $"Exit code: {attempt.LastExitCode}"}");
            Console.WriteLine($"Повтор через: {attempt.NextDelay.TotalMilliseconds}ms");
        };
    });
```

### Retry с получением вывода
```csharp
var output = await Command.Run("curl")
    .WithArguments("-s", "https://api.example.com/data")
    .ExecuteWithRetryAndReadOutputAsync(RetryOptions.Exponential(3));
```

---

## 🛡️ Shell Escaping (Защита от инъекций)

### Автоматическое экранирование аргументов
```csharp
// Безопасные аргументы — автоматическое экранирование
var userInput = "file with spaces; rm -rf /";

await Command.Run("cat")
    .WithSafeArguments(userInput)  // Безопасно экранируется
    .ExecuteAsync();

// Несколько аргументов
await Command.Run("echo")
    .WithSafeArguments("hello", "world; malicious", "test")
    .ExecuteAsync();
```

### Утилиты ShellEscaper
```csharp
using SolimusWrapper;

// Ручное экранирование
var escaped = ShellEscaper.Escape("file; rm -rf /");
// Windows: "file; rm -rf /"
// Unix: 'file; rm -rf /'

// Экранирование для конкретной платформы
var windowsEscaped = ShellEscaper.EscapeWindows("test%PATH%");
var unixEscaped = ShellEscaper.EscapeUnix("$HOME/file");
var psEscaped = ShellEscaper.EscapePowerShell("it's a test");

// Проверка на опасные символы
if (ShellEscaper.ContainsDangerousCharacters(userInput))
{
    Console.WriteLine("Входные данные содержат опасные символы!");
}

// Проверка на паттерны инъекций
if (ShellEscaper.LooksLikeInjection(userInput))
{
    Console.WriteLine("Возможная попытка инъекции команды!");
}

// Построение безопасной командной строки
var cmdLine = ShellEscaper.BuildCommandLine("echo", "hello", "world; rm -rf /");
```

### SafeCommand — безопасный запуск с валидацией
```csharp
using SolimusWrapper;

// Валидирует путь и аргументы перед выполнением
var command = SafeCommand.RunWithSafeArgs("echo", userInput);

// Выбрасывает исключение при подозрительных паттернах
try
{
    var cmd = SafeCommand.Shell("echo hello && rm -rf /");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Отклонено: {ex.Message}");
}

// Валидация пути к исполняемому файлу
try
{
    var cmd = SafeCommand.Run("../../../etc/passwd");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Path traversal заблокирован: {ex.Message}");
}
```

---

## 📝 Structured Logging (Логирование)

### Консольное логирование
```csharp
using SolimusWrapper;

// Простое консольное логирование
await Command.Run("dotnet")
    .WithArguments("build")
    .WithConsoleLogging()
    .ExecuteAsync();

// С настройками
await Command.Run("dotnet")
    .WithArguments("test")
    .WithConsoleLogging(options =>
    {
        options.LogCommandStart = true;
        options.LogCommandEnd = true;
        options.StandardOutputLevel = CommandLogLevel.Debug;
        options.StandardErrorLevel = CommandLogLevel.Warning;
        options.IncludeTimestamp = true;
        options.TimestampFormat = "HH:mm:ss.fff";
    })
    .ExecuteAsync();
```

### Файловое логирование
```csharp
using SolimusWrapper;
using SolimusWrapper.Logging;

// Простое файловое логирование
using var logger = new FileCommandLogger("commands.log");

await Command.Run("dotnet")
    .WithArguments("build")
    .WithLogger(logger)
    .ExecuteAsync();

// С настройками
var options = new LoggingOptions
{
    MaskSensitiveData = true,
    MaxLineLength = 500,
    StandardOutputLevel = CommandLogLevel.Information
};

using var fileLogger = new FileCommandLogger("app.log", options);

await Command.Run("deploy")
    .WithArguments("--token", "secret123")
    .WithLogger(fileLogger)
    .ExecuteAsync();

// В логе: --token ****
```

### Маскирование чувствительных данных
```csharp
var options = new LoggingOptions
{
    MaskSensitiveData = true,
    SensitivePatterns = 
    [
        @"(?i)(password|pwd|secret|token|key|apikey|api_key)[\s:=]+\S+",
        @"(?i)bearer\s+\S+",
        @"(?i)basic\s+\S+",
        @"--token\s+\S+"  // Кастомный паттерн
    ]
};

using var logger = new FileCommandLogger("audit.log", options);

await Command.Run("curl")
    .WithArguments("-H", "Authorization: Bearer secret_token_123", "https://api.example.com")
    .WithLogger(logger)
    .ExecuteAsync();

// В логе: Authorization: Bearer ****
```

### Композитный логгер (несколько целей)
```csharp
using SolimusWrapper.Logging;

// Логирование одновременно в консоль и файл
using var fileLogger = new FileCommandLogger("commands.log");
var compositeLogger = new CompositeCommandLogger(
    new ConsoleCommandLogger(),
    fileLogger
);

await Command.Run("important-task")
    .WithLogger(compositeLogger)
    .ExecuteAsync();
```

### Кастомный логгер
```csharp
using SolimusWrapper.Logging;

public class SerilogCommandLogger : ICommandLogger
{
    private readonly ILogger _logger;

    public SerilogCommandLogger(ILogger logger) => _logger = logger;

    public void LogCommandStart(CommandStartInfo info)
        => _logger.Information("Starting command: {Command}", info.CommandLine);

    public void LogStandardOutput(string line)
        => _logger.Debug("[stdout] {Line}", line);

    public void LogStandardError(string line)
        => _logger.Warning("[stderr] {Line}", line);

    public void LogCommandEnd(CommandEndInfo info)
        => _logger.Information("Command completed: {ExitCode} in {Duration}ms", 
            info.ExitCode, info.Duration.TotalMilliseconds);

    public void LogError(Exception exception)
        => _logger.Error(exception, "Command failed");

    public void LogRetry(RetryAttempt attempt)
        => _logger.Warning("Retry {Attempt}/{Max}: {Reason}", 
            attempt.AttemptNumber, attempt.MaxAttempts, 
            attempt.LastException?.Message ?? $"Exit code: {attempt.LastExitCode}");
}

// Использование
var serilogLogger = new SerilogCommandLogger(Log.Logger);

await Command.Run("task")
    .WithLogger(serilogLogger)
    .ExecuteAsync();
```

### Уровни логирования
```csharp
public enum CommandLogLevel
{
    Trace,       // Максимальная детализация
    Debug,       // Отладочная информация
    Information, // Общая информация
    Warning,     // Предупреждения
    Error,       // Ошибки
    None         // Отключено
}
```

---

## 🔨 CommandBuilder

Альтернативный способ построения команд с условной логикой:
```csharp
using SolimusWrapper.Builders;

var verbose = true;
var configuration = "Release";
var outputPath = "./publish";

var result = await new CommandBuilder("dotnet")
    .AddArgument("publish")
    .AddFlag("--no-restore")
    .AddFlag("-v", verbose)                                    // Условный флаг
    .AddArgumentIfNotEmpty("-c", configuration)                // Если не пустой
    .AddArgumentIfNotEmpty("-o", outputPath)
    .AddArgumentIf(OperatingSystem.IsLinux(), "-r", "linux-x64")     // Платформо-зависимый
    .AddArgumentIf(OperatingSystem.IsWindows(), "-r", "win-x64")
    .AddSafeArgument(userProvidedPath)                         // Безопасный аргумент
    .SetWorkingDirectory("/path/to/project")
    .SetTimeout(TimeSpan.FromMinutes(10))
    .SetValidation(true)
    .UseConsoleLogging()                                       // Логирование
    .ExecuteAsync();
```

### CommandBuilder с Retry и Logging
```csharp
var result = await new CommandBuilder("curl")
    .AddArguments("-s", "https://api.example.com")
    .SetTimeout(TimeSpan.FromSeconds(30))
    .UseConsoleLogging(options =>
    {
        options.MaskSensitiveData = true;
        options.StandardOutputLevel = CommandLogLevel.Debug;
    })
    .ExecuteWithRetryAsync(options =>
    {
        options.MaxAttempts = 3;
        options.Delay = TimeSpan.FromSeconds(1);
        options.BackoffMultiplier = 2.0;
    });
```

### Методы CommandBuilder

| Метод | Описание |
|-------|----------|
| `SetTarget(string)` | Устанавливает исполняемый файл |
| `AddArgument(string)` | Добавляет один аргумент |
| `AddArgument(string, string)` | Добавляет аргумент с значением |
| `AddArguments(params string[])` | Добавляет несколько аргументов |
| `AddArgumentIf(bool, string)` | Условное добавление аргумента |
| `AddArgumentIf(bool, string, string)` | Условное добавление с значением |
| `AddArgumentIfNotEmpty(string, string?)` | Добавляет если значение не пустое |
| `AddFlag(string, bool)` | Добавляет флаг если enabled = true |
| `AddSafeArgument(string)` | Добавляет с экранированием |
| `AddSafeArguments(params string[])` | Добавляет несколько с экранированием |
| `ClearArguments()` | Очищает все аргументы |
| `SetWorkingDirectory(string)` | Устанавливает рабочую директорию |
| `SetEnvironmentVariable(string, string?)` | Добавляет переменную окружения |
| `SetStandardOutput(PipeTarget)` | Перенаправляет stdout |
| `SetStandardOutput(Action<string>)` | stdout в делегат |
| `SetStandardError(PipeTarget)` | Перенаправляет stderr |
| `SetStandardInput(PipeSource)` | Устанавливает stdin |
| `SetEncoding(Encoding)` | Устанавливает кодировку |
| `SetValidation(bool)` | Проверка exit code |
| `SetTimeout(TimeSpan)` | Устанавливает таймаут |
| `OnExit(Action<int>)` | Callback при завершении |
| `SetLogger(ICommandLogger)` | Устанавливает логгер |
| `UseConsoleLogging()` | Консольное логирование |
| `UseFileLogging(string)` | Файловое логирование |
| `Build()` | Создаёт объект Command |
| `ExecuteAsync()` | Строит и выполняет |
| `ExecuteWithRetryAsync(RetryOptions)` | Выполняет с повторами |

---

## 🌐 Кросс-платформенные хелперы
```csharp
using SolimusWrapper;

// Shell команда (cmd на Windows, sh на Unix)
var result = await CommandExtensions.Shell("echo Hello").ExecuteAndReadOutputAsync();

// Список файлов (dir на Windows, ls на Unix)
var files = await CommandExtensions.ListFiles().ExecuteAndReadOutputAsync();
var files2 = await CommandExtensions.ListFiles("/path/to/dir").ExecuteAndReadOutputAsync();

// Текущая директория (cd на Windows, pwd на Unix)
var pwd = await CommandExtensions.GetCurrentDirectory().ExecuteAndReadOutputAsync();

// Echo
await CommandExtensions.Echo("Hello, World!").ExecuteAsync();

// Sleep (timeout на Windows, sleep на Unix)
await CommandExtensions.Sleep(5).ExecuteAsync();

// Ping (-n на Windows, -c на Unix)
await CommandExtensions.Ping("google.com", count: 4)
    .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
    .ExecuteAsync();

// Переменная окружения
var path = await CommandExtensions.GetEnvironmentVariable("PATH").ExecuteAndReadOutputAsync();

// Поиск файлов
var found = await CommandExtensions.FindFiles("*.cs", "/path/to/search").ExecuteAndReadOutputAsync();

// Проверка существования файла
var exists = await CommandExtensions.FileExists("myfile.txt").ExecuteAndReadOutputAsync();
```

---

## 📊 API Reference

### Command

| Метод | Описание |
|-------|----------|
| `Run(string)` | Создаёт команду |
| `WithArguments(...)` | Устанавливает аргументы |
| `WithSafeArguments(...)` | Аргументы с экранированием |
| `WithWorkingDirectory(string)` | Рабочая директория |
| `WithEnvironmentVariable(string, string?)` | Переменная окружения |
| `WithEnvironmentVariables(...)` | Несколько переменных |
| `WithStandardOutputPipe(PipeTarget)` | Перенаправление stdout |
| `WithStandardErrorPipe(PipeTarget)` | Перенаправление stderr |
| `WithStandardInputPipe(PipeSource)` | Источник stdin |
| `WithEncoding(Encoding)` | Кодировка |
| `WithValidation(bool)` | Проверка exit code |
| `WithTimeout(TimeSpan)` | Таймаут |
| `OnExit(Action<int>)` | Callback при завершении |
| `WithLogger(ICommandLogger)` | Устанавливает логгер |
| `WithConsoleLogging()` | Консольное логирование |
| `WithFileLogging(string)` | Файловое логирование |
| `WithoutLogging()` | Отключает логирование |
| `ExecuteAsync()` | Выполнить команду |
| `ExecuteAndReadOutputAsync()` | Выполнить и получить stdout |
| `ExecuteAndReadAllAsync()` | Получить stdout + stderr |
| `ExecuteWithRetryAsync(...)` | Выполнить с повторами |

### PipeTarget

| Метод | Описание |
|-------|----------|
| `Null` | Отбрасывает вывод |
| `ToStringBuilder(sb)` | В StringBuilder |
| `ToDelegate(action)` | Построчный callback |
| `ToStream(stream)` | В поток |
| `ToFile(path)` | В файл |

### PipeSource

| Метод | Описание |
|-------|----------|
| `Null` | Пустой ввод |
| `FromString(text)` | Из строки |
| `FromStream(stream)` | Из потока |
| `FromFile(path)` | Из файла |
| `FromBytes(data)` | Из массива байтов |

### CommandResult

| Свойство/Метод | Описание |
|----------------|----------|
| `ExitCode` | Код выхода процесса |
| `IsSuccess` | true если ExitCode == 0 |
| `StartTime` | Время запуска |
| `ExitTime` | Время завершения |
| `RunTime` | Длительность выполнения |
| `EnsureSuccess()` | Выбрасывает исключение если не успех |

### RetryOptions

| Свойство | Описание |
|----------|----------|
| `MaxAttempts` | Максимум попыток (по умолчанию 3) |
| `Delay` | Начальная задержка |
| `BackoffMultiplier` | Множитель для exponential backoff |
| `MaxDelay` | Максимальная задержка |
| `UseJitter` | Добавить случайность к задержке |
| `ShouldRetry` | Предикат для исключений |
| `ShouldRetryOnExitCode` | Предикат для exit codes |
| `OnRetry` | Callback перед повтором |

### LoggingOptions

| Свойство | Описание |
|----------|----------|
| `LogCommandStart` | Логировать запуск |
| `LogCommandEnd` | Логировать завершение |
| `StandardOutputLevel` | Уровень для stdout |
| `StandardErrorLevel` | Уровень для stderr |
| `MaskSensitiveData` | Маскировать пароли и токены |
| `SensitivePatterns` | Регулярные выражения для маскирования |
| `MaxLineLength` | Максимальная длина строки в логе |
| `IncludeTimestamp` | Включать timestamp |
| `TimestampFormat` | Формат timestamp |

---

## 🧪 Примеры

### Git операции
```csharp
// Статус репозитория
var status = await Command.Run("git")
    .WithArguments("status", "--short")
    .WithWorkingDirectory("/path/to/repo")
    .ExecuteAndReadOutputAsync();

// Коммит с retry
await Command.Run("git")
    .WithArguments("push", "origin", "main")
    .WithTimeout(TimeSpan.FromMinutes(2))
    .ExecuteWithRetryAsync(RetryOptions.Exponential(3));
```

### Docker
```csharp
// Запуск контейнера с логированием
await Command.Run("docker")
    .WithArguments("run", "--rm", "-d", "-p", "8080:80", "nginx")
    .WithConsoleLogging()
    .ExecuteAsync();

// Docker Compose с переменными окружения
await Command.Run("docker-compose")
    .WithArguments("up", "-d")
    .WithWorkingDirectory("/path/to/project")
    .WithEnvironmentVariable("COMPOSE_PROJECT_NAME", "myapp")
    .WithConsoleLogging()
    .ExecuteAsync();
```

### .NET CLI
```csharp
// Сборка проекта
var buildOutput = new StringBuilder();

var result = await Command.Run("dotnet")
    .WithArguments("build", "-c", "Release", "--no-restore")
    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(buildOutput))
    .WithWorkingDirectory("/path/to/solution")
    .WithConsoleLogging()
    .ExecuteAsync();

// Публикация с условной логикой
await new CommandBuilder("dotnet")
    .AddArgument("publish")
    .AddArgument("-c", "Release")
    .AddArgument("-o", "./publish")
    .AddFlag("--self-contained")
    .AddArgumentIf(OperatingSystem.IsLinux(), "-r", "linux-x64")
    .AddArgumentIf(OperatingSystem.IsWindows(), "-r", "win-x64")
    .UseConsoleLogging()
    .ExecuteAsync();
```

### HTTP запросы с curl
```csharp
// GET запрос с retry
var response = await Command.Run("curl")
    .WithArguments("-s", "https://api.github.com/users/octocat")
    .ExecuteWithRetryAndReadOutputAsync(RetryOptions.Exponential(3));

// POST запрос с безопасными данными
using var logger = new FileCommandLogger("api.log", new LoggingOptions { MaskSensitiveData = true });

await Command.Run("curl")
    .WithArguments(
        "-X", "POST",
        "-H", "Authorization: Bearer secret_token",
        "-d", "{\"name\": \"test\"}",
        "https://api.example.com/data")
    .WithLogger(logger)
    .ExecuteAsync();
```

---

## ⚡ Оптимизации

Библиотека оптимизирована для производительности:

| Аспект | Реализация |
|--------|------------|
| Memory | `ArrayPool<char>` для буферов чтения |
| ValueTask | Меньше аллокаций для sync-path |
| Record struct | `CommandResult` размещается на стеке |
| file sealed | Внутренние классы скрыты от API |
| UTF8 без BOM | Корректная работа stdin |
| Trimming | Полная поддержка Native AOT |

---

## 🛠️ Сборка из исходников
```bash
# Клонировать репозиторий
git clone https://github.com/yourname/SolimusWrapper.git
cd SolimusWrapper

# Восстановить зависимости
dotnet restore

# Собрать
dotnet build

# Запустить тесты
dotnet test

# Запустить демо
dotnet run --project samples/SolimusWrapper.Demo

# Создать NuGet пакет
dotnet pack -c Release
```

---

## 🔧 Требования

- .NET 10.0 или выше
- Поддерживаемые платформы: Windows, Linux, macOS

---

## 📄 Лицензия

MIT License. См. [LICENSE](LICENSE) для подробностей.

---

## 🤝 Contributing

Contributions welcome! Пожалуйста, создайте issue или pull request.

1. Fork репозитория
2. Создайте feature branch (`git checkout -b feature/amazing-feature`)
3. Commit изменения (`git commit -m 'Add amazing feature'`)
4. Push в branch (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

---

## 📞 Связь

- GitHub Issues: [Issues](https://github.com/13cyberpunk02/SolimusWrapper/issues)
- Email: salawat1302@gmail.com

---
