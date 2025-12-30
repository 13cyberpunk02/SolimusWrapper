# SolimusWrapper Tests

Unit-тесты для библиотеки SolimusWrapper на базе xUnit.

## 🚀 Запуск тестов
```bash
# Все тесты
dotnet test

# С подробным выводом
dotnet test --logger "console;verbosity=detailed"

# С фильтром по классу
dotnet test --filter "FullyQualifiedName~CommandTests"

# С фильтром по имени теста
dotnet test --filter "DisplayName~ExecuteAsync_SimpleCommand"

# С покрытием кода
dotnet test --collect:"XPlat Code Coverage"

# Параллельное выполнение
dotnet test --parallel
```

## 📁 Структура
```
SolimusWrapper.Tests/
├── SolimusWrapper.Tests.csproj       # Файл проекта
├── CommandTests.cs                 # Тесты класса Command
├── CommandBuilderTests.cs          # Тесты CommandBuilder
├── CommandResultTests.cs           # Тесты CommandResult
├── PipeTargetTests.cs              # Тесты перенаправления вывода
├── PipeSourceTests.cs              # Тесты перенаправления ввода
└── Fixtures/
    └── TestHelper.cs               # Вспомогательные методы
```

## 📦 Зависимости
```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <IsPackable>false</IsPackable>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="coverlet.collector" Version="6.0.4">
            <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
            <PrivateAssets>all</PrivateAssets>
        </PackageReference>
        <PackageReference Include="FluentAssertions" Version="8.8.0" />
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1"/>
        <PackageReference Include="xunit" Version="2.9.3"/>
        <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4">
            <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
            <PrivateAssets>all</PrivateAssets>
        </PackageReference>
        <PackageReference Include="Xunit.SkippableFact" Version="1.5.61" />
    </ItemGroup>

    <ItemGroup>
        <Using Include="Xunit"/>
    </ItemGroup>

    <ItemGroup>
      <ProjectReference Include="..\..\src\SolimusWrapper.Core\SolimusWrapper.Core.csproj" />
    </ItemGroup>

</Project>
```

## 🧪 Категории тестов

### CommandTests

Тесты основного класса `Command`:

| Тест | Описание |
|------|----------|
| `ExecuteAsync_SimpleCommand_ReturnsSuccessResult` | Базовое выполнение команды возвращает успешный результат |
| `ExecuteAndReadOutputAsync_EchoCommand_ReturnsOutput` | Получение stdout работает корректно |
| `ExecuteAndReadAllAsync_ReturnsStdOutAndStdErr` | Получение stdout и stderr раздельно |
| `ExecuteAsync_NonZeroExitCode_ThrowsException` | Выбрасывается исключение при ненулевом exit code |
| `ExecuteAsync_NonZeroExitCode_WithValidationDisabled_DoesNotThrow` | Отключение проверки exit code |
| `ExecuteAsync_VariousExitCodes_ReturnsCorrectCode` | Корректная обработка различных exit codes |
| `ExecuteAsync_WithTimeout_ThrowsTimeoutException` | Таймаут выбрасывает TimeoutException |
| `ExecuteAsync_WithSufficientTimeout_Completes` | Команда завершается при достаточном таймауте |
| `ExecuteAsync_WithCancellation_ThrowsOperationCanceled` | Отмена выбрасывает OperationCanceledException |
| `ExecuteAsync_WithWorkingDirectory_UsesCorrectDirectory` | Рабочая директория применяется корректно |
| `ExecuteAsync_WithEnvironmentVariable_PassesVariable` | Переменные окружения передаются процессу |
| `ExecuteAsync_WithMultipleEnvironmentVariables_PassesAllVariables` | Несколько переменных передаются корректно |
| `FluentMethods_ReturnNewInstance` | Fluent-методы возвращают новый экземпляр |
| `FluentMethods_ChainCorrectly` | Цепочки методов работают корректно |
| `ExecuteAsync_OnExitCallback_IsCalled` | Callback OnExit вызывается |
| `ToString_ReturnsFormattedCommand` | ToString форматирует команду правильно |
| `ToString_EscapesArgumentsWithSpaces` | Аргументы с пробелами экранируются |

### CommandBuilderTests

Тесты класса `CommandBuilder`:

| Тест | Описание |
|------|----------|
| `Build_WithTarget_CreatesCommand` | Создание команды с целью |
| `Build_WithoutTarget_ThrowsException` | Исключение при отсутствии цели |
| `AddArgument_SingleArgument_AddsToCommand` | Добавление одного аргумента |
| `AddArguments_MultipleArguments_AddsAllToCommand` | Добавление нескольких аргументов |
| `AddArgumentIf_ConditionTrue_AddsArgument` | Условное добавление (true) |
| `AddArgumentIf_ConditionFalse_DoesNotAddArgument` | Условное добавление (false) |
| `AddArgumentIfNotEmpty_WithValue_AddsArgument` | Добавление если не пустое |
| `AddArgumentIfNotEmpty_WithNullValue_DoesNotAddArgument` | Пропуск null значений |
| `AddFlag_Enabled_AddsFlag` | Добавление флага (enabled) |
| `AddFlag_Disabled_DoesNotAddFlag` | Пропуск флага (disabled) |
| `ClearArguments_RemovesAllArguments` | Очистка аргументов |
| `ExecuteAsync_DirectExecution_Works` | Прямое выполнение через builder |
| `ExecuteAndReadOutputAsync_DirectExecution_ReturnsOutput` | Получение вывода через builder |
| `SetTimeout_AppliesTimeout` | Установка таймаута |
| `SetTimeout_NegativeValue_ThrowsOnBuild` | Валидация отрицательного таймаута |
| `SetValidation_False_DoesNotThrowOnNonZeroExit` | Отключение валидации |
| `SetStandardOutput_StringBuilder_CapturesOutput` | Перенаправление в StringBuilder |
| `SetStandardOutput_Delegate_CallsHandler` | Перенаправление в делегат |
| `MergeStandardOutputAndError_CapturesBoth` | Объединение stdout и stderr |

### CommandResultTests

Тесты структуры `CommandResult`:

| Тест | Описание |
|------|----------|
| `IsSuccess_ExitCodeZero_ReturnsTrue` | IsSuccess = true при ExitCode = 0 |
| `IsSuccess_NonZeroExitCode_ReturnsFalse` | IsSuccess = false при ExitCode != 0 |
| `RunTime_CalculatesCorrectDuration` | Корректный расчёт длительности |
| `EnsureSuccess_ExitCodeZero_DoesNotThrow` | EnsureSuccess не выбрасывает при успехе |
| `EnsureSuccess_NonZeroExitCode_ThrowsException` | EnsureSuccess выбрасывает при ошибке |

### PipeTargetTests

Тесты перенаправления вывода:

| Тест | Описание |
|------|----------|
| `ToStringBuilder_CapturesOutput` | Запись в StringBuilder |
| `ToDelegate_CallsHandlerForEachLine` | Построчный вызов делегата |
| `ToFile_WritesOutputToFile` | Запись в файл |
| `ToStream_WritesToStream` | Запись в поток |
| `Null_DiscardsOutput` | Отбрасывание вывода |

### PipeSourceTests

Тесты перенаправления ввода:

| Тест | Описание |
|------|----------|
| `FromString_SendsInputToProcess` | Ввод из строки |
| `FromString_MultilineInput_SendsAllLines` | Многострочный ввод |
| `FromStream_SendsStreamContents` | Ввод из потока |
| `FromFile_SendsFileContents` | Ввод из файла |
| `FromBytes_SendsBytesToProcess` | Ввод из байтов |
| `Grep_WithStdin_FiltersInput` | Фильтрация через stdin |

## 🛠️ TestHelper

Вспомогательный класс для кросс-платформенных тестов:
```csharp
using System.Runtime.InteropServices;

namespace SolimusWrapper.Tests.Fixtures;

public static class TestHelper
{
    // Определение платформы
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsUnix => IsLinux || IsMacOS;

    /// <summary>
    /// Возвращает команду echo для текущей платформы
    /// </summary>
    public static (string Command, string[] Args) GetEchoCommand(string text)
    {
        if (IsWindows)
            return ("cmd", ["/c", $"echo {text}"]);
        
        return ("echo", [text]);
    }

    /// <summary>
    /// Возвращает команду sleep для текущей платформы
    /// </summary>
    public static (string Command, string[] Args) GetSleepCommand(int seconds)
    {
        if (IsWindows)
            return ("powershell", ["-Command", $"Start-Sleep -Seconds {seconds}"]);
        
        return ("sleep", [seconds.ToString()]);
    }

    /// <summary>
    /// Возвращает команду pwd/cd для текущей платформы
    /// </summary>
    public static (string Command, string[] Args) GetPwdCommand()
    {
        if (IsWindows)
            return ("cmd", ["/c", "cd"]);
        
        return ("pwd", []);
    }

    /// <summary>
    /// Возвращает команду с указанным exit code
    /// </summary>
    public static (string Command, string[] Args) GetExitCodeCommand(int exitCode)
    {
        if (IsWindows)
            return ("cmd", ["/c", $"exit {exitCode}"]);
        
        return ("sh", ["-c", $"exit {exitCode}"]);
    }

    /// <summary>
    /// Возвращает команду которая пишет в stderr
    /// </summary>
    public static (string Command, string[] Args) GetStdErrCommand(string text)
    {
        if (IsWindows)
            return ("cmd", ["/c", $"echo {text} 1>&2"]);
        
        return ("sh", ["-c", $"echo '{text}' >&2"]);
    }

    /// <summary>
    /// Создаёт временный файл
    /// </summary>
    public static string CreateTempFile(string content = "")
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Создаёт временную директорию
    /// </summary>
    public static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        return path;
    }
}
```

## ⚠️ Пропуск платформо-специфичных тестов

Некоторые тесты работают только на определённых платформах:
```csharp
[SkippableFact]
public async Task FromString_SendsInputToProcess()
{
    Skip.If(TestHelper.IsWindows, "cat command not available on Windows");
    
    var output = await Command.Run("cat")
        .WithStandardInputPipe(PipeSource.FromString("hello"))
        .ExecuteAndReadOutputAsync();
    
    output.Should().Be("hello");
}
```

## 📊 Покрытие кода

### Генерация отчёта
```bash
# Запуск тестов с покрытием
dotnet test --collect:"XPlat Code Coverage"

# Установка ReportGenerator (однократно)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Генерация HTML отчёта
reportgenerator \
    -reports:"TestResults/**/coverage.cobertura.xml" \
    -targetdir:"coveragereport" \
    -reporttypes:Html

# Открыть отчёт
open coveragereport/index.html  # macOS
start coveragereport/index.html # Windows
xdg-open coveragereport/index.html # Linux
```

### Целевые показатели

| Метрика | Цель |
|---------|------|
| Line Coverage | > 80% |
| Branch Coverage | > 75% |
| Method Coverage | > 90% |

## 🔄 CI/CD Integration

### GitHub Actions
```yaml
name: Tests

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  test:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]
        dotnet: ['8.0.x', '9.0.x']

    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ matrix.dotnet }}
    
    - name: Restore
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"
    
    - name: Upload coverage
      uses: codecov/codecov-action@v4
      with:
        files: '**/coverage.cobertura.xml'
        fail_ci_if_error: true
```

### Azure DevOps
```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Restore'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build'
  inputs:
    command: 'build'
    arguments: '--no-restore'

- task: DotNetCoreCLI@2
  displayName: 'Test'
  inputs:
    command: 'test'
    arguments: '--no-build --collect:"XPlat Code Coverage"'
    publishTestResults: true

- task: PublishCodeCoverageResults@2
  displayName: 'Publish Coverage'
  inputs:
    codeCoverageTool: 'Cobertura'
    summaryFileLocation: '**/coverage.cobertura.xml'
```

## 📝 Написание тестов

### Соглашения об именовании
```
MethodName_Scenario_ExpectedBehavior
```

Примеры:
- `ExecuteAsync_SimpleCommand_ReturnsSuccessResult`
- `ExecuteAsync_WithTimeout_ThrowsTimeoutException`
- `AddArgumentIf_ConditionFalse_DoesNotAddArgument`

### Структура теста (AAA)
```csharp
[Fact]
public async Task ExecuteAsync_SimpleCommand_ReturnsSuccessResult()
{
    // Arrange
    var (cmd, args) = TestHelper.GetEchoCommand("hello");

    // Act
    var result = await Command.Run(cmd)
        .WithArguments(args)
        .ExecuteAsync();

    // Assert
    result.ExitCode.Should().Be(0);
    result.IsSuccess.Should().BeTrue();
    result.RunTime.Should().BeGreaterThan(TimeSpan.Zero);
}
```

### Использование FluentAssertions
```csharp
// Базовые проверки
result.Should().NotBeNull();
result.ExitCode.Should().Be(0);
result.IsSuccess.Should().BeTrue();

// Строки
output.Should().Contain("expected");
output.Should().StartWith("prefix");
output.Should().BeEmpty();

// Коллекции
lines.Should().HaveCount(3);
lines.Should().Contain(l => l.Contains("error"));

// Исключения
var act = () => command.ExecuteAsync().AsTask();
await act.Should().ThrowAsync<TimeoutException>();
await act.Should().ThrowAsync<CommandExecutionException>()
    .Where(e => e.ExitCode == 1);
```

### Theory с InlineData
```csharp
[Theory]
[InlineData(0)]
[InlineData(1)]
[InlineData(42)]
[InlineData(255)]
public async Task ExecuteAsync_VariousExitCodes_ReturnsCorrectCode(int expectedCode)
{
    // Arrange
    var (cmd, args) = TestHelper.GetExitCodeCommand(expectedCode);

    // Act
    var result = await Command.Run(cmd)
        .WithArguments(args)
        .WithValidation(false)
        .ExecuteAsync();

    // Assert
    result.ExitCode.Should().Be(expectedCode);
}
```

## 🔧 Отладка тестов

### Visual Studio

1. Установить breakpoint в тесте
2. ПКМ на тесте → Debug Test

### VS Code

1. Установить расширение "C# Dev Kit"
2. Открыть тест
3. Нажать "Debug Test" над методом

### Командная строка
```bash
# С подробным выводом
dotnet test --logger "console;verbosity=detailed"

# Один тест
dotnet test --filter "DisplayName~ExecuteAsync_SimpleCommand"
```
