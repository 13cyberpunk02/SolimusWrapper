# SolimusWrapper

[![NuGet](https://img.shields.io/nuget/v/SolimusWrapper.svg)](https://www.nuget.org/packages/SolimusWrapper)
[![License](https://img.shields.io/github/license/13cyberpunk02/SolimusWrapper)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10)](https://dotnet.microsoft.com/)

Лёгкая и оптимизированная библиотека для работы с CLI процессами в .NET. Простой и интуитивный API для запуска внешних команд с поддержкой перенаправления потоков, таймаутов, отмены и кросс-платформенной работы.

## 🚀 Особенности

- **Fluent API** — читаемый и удобный синтаксис
- **Immutable** — каждый вызов создаёт новый экземпляр команды
- **Асинхронность** — полная поддержка async/await и CancellationToken
- **Перенаправление потоков** — stdout, stderr, stdin
- **Таймауты** — автоматическое завершение процессов
- **Кросс-платформенность** — Windows, Linux, macOS
- **Оптимизация памяти** — использование ArrayPool, ValueTask
- **Native AOT** — поддержка trimming

## 📦 Установка
```bash
dotnet add package SolimusWrapper
```

Или через Package Manager:
```powershell
Install-Package SolimusWrapper
```

## 🎯 Быстрый старт
```csharp
using SolimusWrapper;

// Простой запуск команды
var result = await Command.Run("dotnet")
    .WithArguments("--version")
    .ExecuteAsync();

Console.WriteLine($"Exit code: {result.ExitCode}");

// Получить вывод команды
var output = await Command.Run("git")
    .WithArguments("status")
    .ExecuteAndReadOutputAsync();

Console.WriteLine(output);
```

## 📖 Использование

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
```

### Получение вывода
```csharp
// Только stdout
var stdout = await Command.Run("echo")
    .WithArguments("Hello, World!")
    .ExecuteAndReadOutputAsync();

// stdout и stderr отдельно
var (stdOut, stdErr) = await Command.Run("dotnet")
    .WithArguments("build")
    .ExecuteAndReadAllAsync();
```

### Перенаправление потоков
```csharp
var output = new StringBuilder();
var errors = new StringBuilder();

await Command.Run("dotnet")
    .WithArguments("build")
    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(errors))
    .ExecuteAsync();
```

### Вывод в реальном времени
```csharp
await Command.Run("dotnet")
    .WithArguments("test")
    .WithStandardOutputPipe(PipeTarget.ToDelegate(line => 
        Console.WriteLine($"[OUT] {line}")))
    .WithStandardErrorPipe(PipeTarget.ToDelegate(line => 
        Console.WriteLine($"[ERR] {line}")))
    .ExecuteAsync();
```

### Запись в файл
```csharp
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
```

### Рабочая директория
```csharp
await Command.Run("npm")
    .WithArguments("install")
    .WithWorkingDirectory("/path/to/project")
    .ExecuteAsync();
```

### Переменные окружения
```csharp
await Command.Run("node")
    .WithArguments("app.js")
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

### Таймауты
```csharp
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
```

### Отмена операции
```csharp
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
```

### Callback при завершении
```csharp
await Command.Run("task")
    .OnExit(exitCode => 
    {
        if (exitCode != 0)
            Console.WriteLine($"Warning: exit code {exitCode}");
    })
    .WithValidation(false)
    .ExecuteAsync();
```

### Кодировка
```csharp
using System.Text;

// Для Windows консоли (кириллица)
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

await Command.Run("cmd")
    .WithArguments("/c", "dir")
    .WithEncoding(Encoding.GetEncoding(866))
    .ExecuteAsync();
```

## 🔨 CommandBuilder

Альтернативный способ построения команд с условной логикой:
```csharp
using SolimusWrapper.Builders;

var verbose = true;
var configuration = "Release";

var result = await new CommandBuilder("dotnet")
    .AddArgument("build")
    .AddFlag("--no-restore")
    .AddFlag("-v", verbose)
    .AddArgumentIfNotEmpty("-c", configuration)
    .AddArgumentIf(Environment.OSVersion.Platform == PlatformID.Unix, "--runtime", "linux-x64")
    .SetWorkingDirectory("/path/to/project")
    .SetTimeout(TimeSpan.FromMinutes(10))
    .SetStandardOutput(Console.WriteLine)
    .ExecuteAsync();
```

### Методы CommandBuilder

| Метод | Описание |
|-------|----------|
| `SetTarget(string)` | Устанавливает исполняемый файл |
| `AddArgument(string)` | Добавляет один аргумент |
| `AddArgument(string, string)` | Добавляет аргумент с значением |
| `AddArguments(params string[])` | Добавляет несколько аргументов |
| `AddArgumentIf(bool, string)` | Условное добавление аргумента |
| `AddArgumentIfNotEmpty(string, string?)` | Добавляет если значение не пустое |
| `AddFlag(string, bool)` | Добавляет флаг если enabled = true |
| `ClearArguments()` | Очищает все аргументы |
| `SetWorkingDirectory(string)` | Устанавливает рабочую директорию |
| `SetEnvironmentVariable(string, string?)` | Добавляет переменную окружения |
| `SetStandardOutput(StringBuilder)` | Перенаправляет stdout в StringBuilder |
| `SetStandardOutput(Action<string>)` | Перенаправляет stdout в делегат |
| `SetStandardError(StringBuilder)` | Перенаправляет stderr в StringBuilder |
| `MergeStandardOutputAndError(PipeTarget)` | Объединяет stdout и stderr |
| `SetStandardInput(string)` | Устанавливает stdin |
| `SetEncoding(Encoding)` | Устанавливает кодировку |
| `SetValidation(bool)` | Включает/выключает проверку exit code |
| `SetTimeout(TimeSpan)` | Устанавливает таймаут |
| `OnExit(Action<int>)` | Callback при завершении |
| `Build()` | Создаёт объект Command |
| `ExecuteAsync()` | Строит и выполняет команду |

## 🌐 Кросс-платформенные хелперы
```csharp
using SolimusWrapper;

// Shell команда (cmd на Windows, sh на Unix)
var result = await CommandExtensions.Shell("echo Hello").ExecuteAndReadOutputAsync();

// Список файлов
var files = await CommandExtensions.ListFiles().ExecuteAndReadOutputAsync();
var files2 = await CommandExtensions.ListFiles("/path/to/dir").ExecuteAndReadOutputAsync();

// Текущая директория
var pwd = await CommandExtensions.GetCurrentDirectory().ExecuteAndReadOutputAsync();

// Echo
await CommandExtensions.Echo("Hello, World!").ExecuteAsync();

// Sleep
await CommandExtensions.Sleep(5).ExecuteAsync();

// Ping
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

## 📊 PipeTarget

| Метод | Описание |
|-------|----------|
| `PipeTarget.Null` | Отбрасывает вывод |
| `PipeTarget.ToStringBuilder(sb)` | В StringBuilder |
| `PipeTarget.ToDelegate(action)` | Вызывает action для каждой строки |
| `PipeTarget.ToStream(stream)` | В поток |
| `PipeTarget.ToFile(path)` | В файл |

## 📥 PipeSource

| Метод | Описание |
|-------|----------|
| `PipeSource.Null` | Пустой ввод |
| `PipeSource.FromString(text)` | Из строки |
| `PipeSource.FromStream(stream)` | Из потока |
| `PipeSource.FromFile(path)` | Из файла |
| `PipeSource.FromBytes(data)` | Из массива байтов |

## 📈 CommandResult
```csharp
var result = await Command.Run("myapp").ExecuteAsync();

// Свойства
int exitCode = result.ExitCode;           // Код выхода
bool success = result.IsSuccess;          // true если ExitCode == 0
DateTimeOffset start = result.StartTime;  // Время запуска
DateTimeOffset end = result.ExitTime;     // Время завершения
TimeSpan duration = result.RunTime;       // Длительность выполнения

// Методы
result.EnsureSuccess();  // Выбрасывает CommandExecutionException если ExitCode != 0
```

## ⚡ Оптимизации

Библиотека оптимизирована для производительности:

| Аспект | Реализация |
|--------|------------|
| Memory | `ArrayPool<char>` для буферов чтения |
| ValueTask | Меньше аллокаций для sync-path |
| Record struct | `CommandResult` размещается на стеке |
| file sealed | Внутренние классы скрыты от API |
| Trimming | Полная поддержка Native AOT |

## 🧪 Примеры

### Git операции
```csharp
// Статус репозитория
var status = await Command.Run("git")
    .WithArguments("status", "--short")
    .WithWorkingDirectory("/path/to/repo")
    .ExecuteAndReadOutputAsync();

// Коммит
await Command.Run("git")
    .WithArguments("commit", "-m", "feat: add new feature")
    .ExecuteAsync();

// Pull с таймаутом
await Command.Run("git")
    .WithArguments("pull", "--rebase")
    .WithTimeout(TimeSpan.FromMinutes(2))
    .ExecuteAsync();
```

### Docker
```csharp
// Запуск контейнера
await Command.Run("docker")
    .WithArguments("run", "--rm", "-d", "-p", "8080:80", "nginx")
    .ExecuteAsync();

// Логи контейнера
await Command.Run("docker")
    .WithArguments("logs", "-f", "my-container")
    .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
    .WithTimeout(TimeSpan.FromSeconds(30))
    .ExecuteAsync();

// Docker Compose
await Command.Run("docker-compose")
    .WithArguments("up", "-d")
    .WithWorkingDirectory("/path/to/project")
    .WithEnvironmentVariable("COMPOSE_PROJECT_NAME", "myapp")
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
    .ExecuteAsync();

if (result.IsSuccess)
{
    Console.WriteLine("Build succeeded!");
    Console.WriteLine(buildOutput);
}

// Запуск тестов
await Command.Run("dotnet")
    .WithArguments("test", "--logger", "console;verbosity=detailed")
    .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
    {
        if (line.Contains("Passed") || line.Contains("Failed"))
            Console.WriteLine(line);
    }))
    .ExecuteAsync();

// Публикация
await new CommandBuilder("dotnet")
    .AddArgument("publish")
    .AddArgument("-c", "Release")
    .AddArgument("-o", "./publish")
    .AddFlag("--self-contained")
    .AddArgumentIf(OperatingSystem.IsLinux(), "-r", "linux-x64")
    .AddArgumentIf(OperatingSystem.IsWindows(), "-r", "win-x64")
    .ExecuteAsync();
```

### FFmpeg
```csharp
// Конвертация видео
await Command.Run("ffmpeg")
    .WithArguments(
        "-i", "input.mp4",
        "-c:v", "libx264",
        "-crf", "23",
        "-c:a", "aac",
        "-b:a", "128k",
        "output.mp4")
    .WithStandardErrorPipe(PipeTarget.ToDelegate(Console.WriteLine))
    .WithTimeout(TimeSpan.FromHours(1))
    .ExecuteAsync();

// Извлечение аудио
await Command.Run("ffmpeg")
    .WithArguments("-i", "video.mp4", "-vn", "-acodec", "mp3", "audio.mp3")
    .ExecuteAsync();
```

### npm / Node.js
```csharp
// Установка зависимостей
await Command.Run("npm")
    .WithArguments("install")
    .WithWorkingDirectory("/path/to/frontend")
    .WithEnvironmentVariable("NODE_ENV", "development")
    .ExecuteAsync();

// Запуск скрипта
await Command.Run("npm")
    .WithArguments("run", "build")
    .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
    .ExecuteAsync();
```

## 📄 Лицензия

MIT License. См. [LICENSE](LICENSE) для подробностей.

## 🤝 Contributing

Contributions welcome! Пожалуйста, создайте issue или pull request.

1. Fork репозитория
2. Создайте feature branch (`git checkout -b feature/amazing-feature`)
3. Commit изменения (`git commit -m 'Add amazing feature'`)
4. Push в branch (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

## 📞 Связь

- GitHub Issues: [Issues](https://github.com/13cyberpunk02/SolimusWrapper/issues)
- Email: salawat1302@gmail.com
