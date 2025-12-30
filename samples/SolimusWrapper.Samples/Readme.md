# SolimusWrapper Samples

Консольное приложение с примерами использования библиотеки SolimusWrapper.

## 🚀 Запуск
```bash
# Из корня решения
dotnet run --project samples/SolimusWrapper.Samples

# Или из папки проекта
cd samples/SolimusWrapper.Samples
dotnet run
```

## 📁 Структура
```
SolimusWrapper.Samples/
├── SolimusWrapper.Samples.csproj # Файл проекта
├── Program.cs                    # Точка входа с примерами
└── README.md                     # Эта документация
```

## 📋 Примеры в приложении

Приложение демонстрирует все основные возможности библиотеки.

### 1. Базовое выполнение команды
```csharp
var result = await Command.Run("dotnet")
    .WithArguments("--version")
    .ExecuteAsync();

Console.WriteLine($"Exit code: {result.ExitCode}");
Console.WriteLine($"Duration: {result.RunTime}");
Console.WriteLine($"Success: {result.IsSuccess}");
```

### 2. Получение вывода команды
```csharp
var version = await Command.Run("dotnet")
    .WithArguments("--version")
    .ExecuteAndReadOutputAsync();

Console.WriteLine($"Version: {version}");
```

### 3. Получение stdout и stderr
```csharp
var (stdOut, stdErr) = await Command.Run("dotnet")
    .WithArguments("build")
    .WithValidation(false)
    .ExecuteAndReadAllAsync();

Console.WriteLine($"Output: {stdOut}");
Console.WriteLine($"Errors: {stdErr}");
```

### 4. Вывод в реальном времени
```csharp
await Command.Run("dotnet")
    .WithArguments("build")
    .WithStandardOutputPipe(PipeTarget.ToDelegate(line => 
        Console.WriteLine($"[OUT] {line}")))
    .WithStandardErrorPipe(PipeTarget.ToDelegate(line => 
        Console.WriteLine($"[ERR] {line}")))
    .ExecuteAsync();
```

### 5. Кросс-платформенные команды
```csharp
// Работает на Windows, Linux и macOS
var files = await CommandExtensions.ListFiles()
    .WithEncoding(encoding)
    .ExecuteAndReadOutputAsync();

Console.WriteLine(files);

var pwd = await CommandExtensions.GetCurrentDirectory()
    .ExecuteAndReadOutputAsync();

Console.WriteLine($"Current directory: {pwd}");
```

### 6. Ping с выводом в реальном времени
```csharp
await CommandExtensions.Ping("google.com", 4)
    .WithEncoding(encoding)
    .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
    .ExecuteAsync();
```

### 7. Обработка ошибок
```csharp
try
{
    await Command.Run("non-existent-command")
        .ExecuteAsync();
}
catch (CommandExecutionException ex)
{
    Console.WriteLine($"Command failed with exit code: {ex.ExitCode}");
}

// Или без исключений
var result = await Command.Run("might-fail")
    .WithValidation(false)
    .ExecuteAsync();

if (!result.IsSuccess)
{
    Console.WriteLine($"Exit code: {result.ExitCode}");
}
```

### 8. Таймауты
```csharp
try
{
    await CommandExtensions.Sleep(60)
        .WithTimeout(TimeSpan.FromSeconds(2))
        .ExecuteAsync();
}
catch (TimeoutException)
{
    Console.WriteLine("Command timed out!");
}
```

### 9. Отмена операции
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    await CommandExtensions.Sleep(60)
        .ExecuteAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation was cancelled!");
}
```

### 10. CommandBuilder с условной логикой
```csharp
var verbose = true;
var configuration = "Release";
var outputPath = "./publish";

await new CommandBuilder("dotnet")
    .AddArgument("publish")
    .AddFlag("-v", verbose)
    .AddArgumentIfNotEmpty("-c", configuration)
    .AddArgumentIfNotEmpty("-o", outputPath)
    .AddArgumentIf(OperatingSystem.IsWindows(), "-r", "win-x64")
    .AddArgumentIf(OperatingSystem.IsLinux(), "-r", "linux-x64")
    .AddArgumentIf(OperatingSystem.IsMacOS(), "-r", "osx-x64")
    .SetStandardOutput(Console.WriteLine)
    .ExecuteAsync();
```

### 11. Запись в файл
```csharp
await Command.Run("dotnet")
    .WithArguments("build", "-v", "detailed")
    .WithStandardOutputPipe(PipeTarget.ToFile("build.log"))
    .WithStandardErrorPipe(PipeTarget.ToFile("errors.log"))
    .ExecuteAsync();

Console.WriteLine("Output saved to build.log");
```

### 12. Работа с stdin
```csharp
var input = "apple\nbanana\napricot\ncherry";

// На Windows используем findstr, на Unix — grep
var cmd = OperatingSystem.IsWindows() ? "findstr" : "grep";
var args = new[] { "ap" };

var output = await Command.Run(cmd)
    .WithArguments(args)
    .WithStandardInputPipe(PipeSource.FromString(input))
    .ExecuteAndReadOutputAsync();

Console.WriteLine(output);
// Результат: apple, apricot
```

### 13. Переменные окружения
```csharp
var output = await CommandExtensions.GetEnvironmentVariable("PATH")
    .ExecuteAndReadOutputAsync();

Console.WriteLine($"PATH: {output}");

// Установка своих переменных
await Command.Run("myapp")
    .WithEnvironmentVariable("MY_VAR", "my_value")
    .WithEnvironmentVariable("DEBUG", "true")
    .ExecuteAsync();
```

### 14. Рабочая директория
```csharp
var gitStatus = await Command.Run("git")
    .WithArguments("status", "--short")
    .WithWorkingDirectory("/path/to/repo")
    .ExecuteAndReadOutputAsync();

Console.WriteLine(gitStatus);
```

### 15. Callback при завершении
```csharp
await Command.Run("dotnet")
    .WithArguments("build")
    .OnExit(exitCode =>
    {
        if (exitCode == 0)
            Console.WriteLine("✅ Build succeeded!");
        else
            Console.WriteLine($"❌ Build failed with code {exitCode}");
    })
    .WithValidation(false)
    .ExecuteAsync();
```

## 🖥️ Кодировка Windows

На Windows для корректного отображения кириллицы требуется настройка кодировки:
```csharp
using System.Text;

// Регистрация дополнительных кодировок (в начале программы)
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Определение кодировки для текущей ОС
var encoding = OperatingSystem.IsWindows()
    ? Encoding.GetEncoding(866)  // CP866 для DOS/консоли Windows
    : Encoding.UTF8;

// Использование
await Command.Run("cmd")
    .WithArguments("/c", "dir")
    .WithEncoding(encoding)
    .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
    .ExecuteAsync();
```

## 🔗 Зависимости проекта
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\SolimusWrapper.Core\SolimusWrapper.Core.csproj" />
  </ItemGroup>

</Project>
```

## 📝 Полный пример Program.cs
```csharp
using System.Text;
using SolimusWrapper.Core;
using SolimusWrapper.Core.Builders;

// Настройка кодировки для Windows
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var encoding = OperatingSystem.IsWindows()
    ? Encoding.GetEncoding(866)
    : Encoding.UTF8;

Console.WriteLine("╔════════════════════════════════════════╗");
Console.WriteLine("║       SolimusWrapper Samples           ║");
Console.WriteLine("╚════════════════════════════════════════╝");
Console.WriteLine();

// 1. Базовая команда
Console.WriteLine("1. dotnet version:");
var result = await Command.Run("dotnet")
    .WithArguments("--version")
    .ExecuteAsync();
var version = await Command.Run("dotnet")
    .WithArguments("--version")
    .ExecuteAndReadOutputAsync();
Console.WriteLine($"   Version: {version}");
Console.WriteLine($"   Exit code: {result.ExitCode}");
Console.WriteLine($"   Duration: {result.RunTime}");
Console.WriteLine();

// 2. Список файлов
Console.WriteLine("2. Files in current directory:");
var files = await CommandExtensions.ListFiles()
    .WithEncoding(encoding)
    .ExecuteAndReadOutputAsync();
Console.WriteLine(files);
Console.WriteLine();

// 3. Текущая директория
Console.WriteLine("3. Current directory:");
var pwd = await CommandExtensions.GetCurrentDirectory()
    .WithEncoding(encoding)
    .ExecuteAndReadOutputAsync();
Console.WriteLine($"   {pwd}");
Console.WriteLine();

// 4. Ping
Console.WriteLine("4. Ping google.com:");
await CommandExtensions.Ping("google.com", 2)
    .WithEncoding(encoding)
    .WithStandardOutputPipe(PipeTarget.ToDelegate(line => Console.WriteLine($"   {line}")))
    .WithValidation(false)
    .ExecuteAsync();
Console.WriteLine();

// 5. CommandBuilder
Console.WriteLine("5. CommandBuilder example:");
await new CommandBuilder("dotnet")
    .AddArgument("--info")
    .SetStandardOutput(line => Console.WriteLine($"   {line}"))
    .SetValidation(false)
    .ExecuteAsync();
Console.WriteLine();

Console.WriteLine("╔════════════════════════════════════════╗");
Console.WriteLine("║           Samples Complete             ║");
Console.WriteLine("╚════════════════════════════════════════╝");
```
