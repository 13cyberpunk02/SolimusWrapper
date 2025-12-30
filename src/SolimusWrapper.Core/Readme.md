# SolimusWrapper Core Library

Ядро библиотеки для работы с CLI процессами в .NET.

## 📦 О библиотеке

SolimusWrapper — это лёгкая и оптимизированная библиотека для запуска внешних процессов из .NET приложений. Она предоставляет удобный Fluent API для построения и выполнения команд с полной поддержкой асинхронности.

## 📁 Структура файлов
```
SolimusWrapper.Core/
├── SolimusWrapper.Core.csproj         # Файл проекта с NuGet метаданными
├── Command.cs                  # Основной класс для выполнения команд
├── CommandResult.cs            # Результат выполнения команды
├── CommandExtensions.cs        # Кросс-платформенные хелперы
├── PipeTarget.cs               # Цели для перенаправления stdout/stderr
├── PipeSource.cs               # Источники для stdin
├── Optional.cs                 # Вспомогательная структура для Clone()
└── Builders/
    └── CommandBuilder.cs       # Builder pattern для построения команд
```

## 🏗️ Архитектура

### Immutable Command

Основной класс `Command` использует immutable pattern — каждый fluent-метод возвращает новый экземпляр:
```csharp
var cmd1 = Command.Run("git");
var cmd2 = cmd1.WithArguments("status");      // cmd1 не изменился
var cmd3 = cmd2.WithWorkingDirectory("/repo"); // cmd2 не изменился
```

Это обеспечивает:
- Потокобезопасность
- Возможность переиспользования базовых команд
- Предсказуемое поведение

### Clone Pattern

Для избежания дублирования кода в fluent-методах используется приватный метод `Clone()`:
```csharp
private Command Clone(
    string? targetFilePath = null,
    IReadOnlyList? arguments = null,
    Optional workingDirectory = default,
    // ... остальные параметры
)
{
    return new Command(
        targetFilePath ?? _targetFilePath,
        arguments ?? _arguments,
        workingDirectory.HasValue ? workingDirectory.Value : _workingDirectory,
        // ...
    );
}

// Fluent-методы становятся простыми:
public Command WithArguments(params string[] args) => Clone(arguments: args);
public Command WithTimeout(TimeSpan timeout) => Clone(timeout: timeout);
```

### Optional<T> структура

Структура `Optional<T>` решает проблему различения "не передано" и "передано null":
```csharp
// Без Optional:
Clone(timeout: null)  // Это "сбросить timeout" или "не менять"?

// С Optional:
Clone(timeout: (TimeSpan?)null)  // Явно передаём null через implicit conversion
Clone()                           // HasValue = false, значение не меняется
```

### PipeTarget / PipeSource

Абстрактные классы с внутренними реализациями (`file sealed class`):
```csharp
public abstract class PipeTarget
{
    public static PipeTarget Null { get; } = new NullPipeTarget();
    public static PipeTarget ToStringBuilder(StringBuilder sb) => new StringBuilderPipeTarget(sb);
    // ...
    
    internal abstract Task CopyFromAsync(StreamReader reader, CancellationToken ct);
}

// Реализации скрыты от внешнего API
file sealed class NullPipeTarget : PipeTarget { /* ... */ }
file sealed class StringBuilderPipeTarget : PipeTarget { /* ... */ }
```

## 📝 Основные классы

### Command

Главный класс библиотеки. Представляет команду для выполнения.

**Создание:**
```csharp
var command = Command.Run("dotnet");
```

**Fluent-методы:**

| Метод | Описание |
|-------|----------|
| `WithArguments(params string[])` | Устанавливает аргументы команды |
| `WithArguments(IEnumerable<string>)` | Устанавливает аргументы из коллекции |
| `WithWorkingDirectory(string)` | Устанавливает рабочую директорию |
| `WithEnvironmentVariable(string, string?)` | Добавляет переменную окружения |
| `WithEnvironmentVariables(IEnumerable<KeyValuePair>)` | Добавляет несколько переменных |
| `WithStandardOutputPipe(PipeTarget)` | Перенаправляет stdout |
| `WithStandardErrorPipe(PipeTarget)` | Перенаправляет stderr |
| `WithStandardInputPipe(PipeSource)` | Устанавливает stdin |
| `WithEncoding(Encoding)` | Устанавливает кодировку |
| `WithValidation(bool)` | Включает/выключает проверку exit code |
| `WithTimeout(TimeSpan)` | Устанавливает таймаут выполнения |
| `OnExit(Action<int>)` | Устанавливает callback при завершении |

**Методы выполнения:**

| Метод | Возвращает | Описание |
|-------|------------|----------|
| `ExecuteAsync(CancellationToken)` | `ValueTask<CommandResult>` | Выполняет команду |
| `ExecuteAndReadOutputAsync(CancellationToken)` | `ValueTask<string>` | Выполняет и возвращает stdout |
| `ExecuteAndReadAllAsync(CancellationToken)` | `ValueTask<(string, string)>` | Выполняет и возвращает stdout + stderr |

### CommandResult

Record struct с результатом выполнения:
```csharp
public readonly record struct CommandResult(
    int ExitCode,
    DateTimeOffset StartTime,
    DateTimeOffset ExitTime)
{
    public TimeSpan RunTime => ExitTime - StartTime;
    public bool IsSuccess => ExitCode == 0;
    
    public void EnsureSuccess()
    {
        if (!IsSuccess)
            throw new CommandExecutionException(ExitCode);
    }
}
```

### CommandExecutionException

Исключение при ненулевом exit code:
```csharp
public class CommandExecutionException(int exitCode) 
    : Exception($"Command failed with exit code {exitCode}")
{
    public int ExitCode { get; } = exitCode;
}
```

### PipeTarget

Цели для перенаправления вывода:

| Фабричный метод | Описание |
|-----------------|----------|
| `PipeTarget.Null` | Отбрасывает вывод |
| `PipeTarget.ToStringBuilder(sb)` | Записывает в StringBuilder |
| `PipeTarget.ToDelegate(action)` | Вызывает делегат для каждой строки |
| `PipeTarget.ToStream(stream)` | Записывает в поток |
| `PipeTarget.ToFile(path)` | Записывает в файл |

### PipeSource

Источники для stdin:

| Фабричный метод | Описание |
|-----------------|----------|
| `PipeSource.Null` | Пустой ввод |
| `PipeSource.FromString(text)` | Из строки |
| `PipeSource.FromStream(stream)` | Из потока |
| `PipeSource.FromFile(path)` | Из файла |
| `PipeSource.FromBytes(data)` | Из массива байтов |

### CommandBuilder

Альтернативный способ построения команд:
```csharp
var command = new CommandBuilder("dotnet")
    .AddArgument("build")
    .AddArgumentIf(verbose, "-v")
    .AddArgumentIfNotEmpty("-c", configuration)
    .SetTimeout(TimeSpan.FromMinutes(5))
    .Build();
```

### CommandExtensions

Статические методы для кросс-платформенных команд:

| Метод | Описание |
|-------|----------|
| `Shell(string)` | Выполняет shell команду (cmd/sh) |
| `ListFiles(string?)` | Список файлов (dir/ls) |
| `GetCurrentDirectory()` | Текущая директория (cd/pwd) |
| `Echo(string)` | Вывод текста |
| `Sleep(int)` | Ожидание |
| `Ping(string, int)` | Ping хоста |
| `GetEnvironmentVariable(string)` | Получить переменную окружения |
| `FindFiles(string, string?)` | Поиск файлов |
| `FileExists(string)` | Проверка существования файла |

## ⚡ Оптимизации

### ArrayPool для буферов
```csharp
file sealed class StringBuilderPipeTarget(StringBuilder sb) : PipeTarget
{
    internal override async Task CopyFromAsync(StreamReader reader, CancellationToken ct)
    {
        var buffer = ArrayPool.Shared.Rent(4096);
        try
        {
            int bytesRead;
            while ((bytesRead = await reader.ReadAsync(buffer.AsMemory(), ct)) > 0)
            {
                lock (sb)
                {
                    sb.Append(buffer, 0, bytesRead);
                }
            }
        }
        finally
        {
            ArrayPool.Shared.Return(buffer);
        }
    }
}
```

### ValueTask для меньших аллокаций
```csharp
public async ValueTask ExecuteAsync(CancellationToken ct = default)
{
    // ValueTask эффективнее Task для sync-completion paths
}
```

### Record struct на стеке
```csharp
// Размещается на стеке, без аллокаций в куче
public readonly record struct CommandResult(int ExitCode, ...);
```

### file sealed классы
```csharp
// Скрыты от внешнего API, компилятор может лучше оптимизировать
file sealed class NullPipeTarget : PipeTarget { }
```

## 📦 NuGet Package

### Метаданные в .csproj
```xml

    net10.0
    latest
    enable
    enable
    
    
    SolimusWrapper
    1.0.0
    13cyberpunk02
    Lightweight and optimized CLI wrapper for .NET
    cli;process;command;shell;wrapper
    MIT
    https://github.com/13cyberpunk02/SolimusWrapper
    README.md
    
    
    true
    true
```

### Сборка пакета
```bash
# Debug сборка
dotnet build

# Release сборка
dotnet build -c Release

# Создание NuGet пакета
dotnet pack -c Release

# Пакет будет в bin/Release/SolimusWrapper.1.0.0.nupkg
```

### Публикация
```bash
# Локальный feed (для тестирования)
dotnet nuget push bin/Release/SolimusWrapper.1.0.0.nupkg -s ~/local-nuget

# NuGet.org
dotnet nuget push bin/Release/SolimusWrapper.1.0.0.nupkg \
    -k YOUR_API_KEY \
    -s https://api.nuget.org/v3/index.json
```

## 🔧 Зависимости

- **Target Frameworks:** .NET 8.0, .NET 9.0
- **External Dependencies:** None (zero dependencies)
- **Language Version:** C# 12+

## 📖 Примеры использования

### Базовый пример
```csharp
using SolimusWrapper;

var result = await Command.Run("dotnet")
    .WithArguments("--version")
    .ExecuteAsync();

Console.WriteLine($"Exit: {result.ExitCode}, Time: {result.RunTime}");
```

### С перенаправлением вывода
```csharp
var output = new StringBuilder();

await Command.Run("git")
    .WithArguments("log", "--oneline", "-10")
    .WithWorkingDirectory("/path/to/repo")
    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
    .ExecuteAsync();

Console.WriteLine(output);
```

### С таймаутом и отменой
```csharp
using var cts = new CancellationTokenSource();

try
{
    await Command.Run("long-process")
        .WithTimeout(TimeSpan.FromSeconds(30))
        .ExecuteAsync(cts.Token);
}
catch (TimeoutException)
{
    Console.WriteLine("Timeout!");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Cancelled!");
}
```

### CommandBuilder с условиями
```csharp
using SolimusWrapper.Builders;

bool isRelease = true;
bool runTests = false;

await new CommandBuilder("dotnet")
    .AddArgument("build")
    .AddArgumentIf(isRelease, "-c", "Release")
    .AddFlag("--no-restore")
    .AddArgumentIf(runTests, "--target", "Test")
    .SetStandardOutput(Console.WriteLine)
    .ExecuteAsync();
```
