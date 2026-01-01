using FluentAssertions;
using SolimusWrapper.Core;
using SolimusWrapper.Tests.Fixtures;

namespace SolimusWrapper.Tests;

public class RetryTests
{
    #region RetryOptions Tests

    [Fact]
    public void RetryOptions_Default_HasCorrectValues()
    {
        // Act
        var options = new RetryOptions();

        // Assert
        options.MaxAttempts.Should().Be(3);
        options.Delay.Should().Be(TimeSpan.FromSeconds(1));
        options.BackoffMultiplier.Should().Be(2.0);
        options.MaxDelay.Should().Be(TimeSpan.FromSeconds(30));
        options.UseJitter.Should().BeTrue();
    }

    [Fact]
    public void RetryOptions_Exponential_HasCorrectValues()
    {
        // Act
        var options = RetryOptions.Exponential(maxAttempts: 5, initialDelay: TimeSpan.FromMilliseconds(500));

        // Assert
        options.MaxAttempts.Should().Be(5);
        options.Delay.Should().Be(TimeSpan.FromMilliseconds(500));
        options.BackoffMultiplier.Should().Be(2.0);
        options.UseJitter.Should().BeTrue();
    }

    [Fact]
    public void RetryOptions_Linear_HasCorrectValues()
    {
        // Act
        var options = RetryOptions.Linear(maxAttempts: 4, delay: TimeSpan.FromSeconds(2));

        // Assert
        options.MaxAttempts.Should().Be(4);
        options.Delay.Should().Be(TimeSpan.FromSeconds(2));
        options.BackoffMultiplier.Should().Be(1.0);
        options.UseJitter.Should().BeFalse();
    }

    [Fact]
    public void RetryOptions_Immediate_HasZeroDelay()
    {
        // Act
        var options = RetryOptions.Immediate(maxAttempts: 3);

        // Assert
        options.MaxAttempts.Should().Be(3);
        options.Delay.Should().Be(TimeSpan.Zero);
        options.BackoffMultiplier.Should().Be(1.0);
        options.UseJitter.Should().BeFalse();
    }

    #endregion

    #region RetryAttempt Tests

    [Fact]
    public void RetryAttempt_IsLastAttempt_ReturnsTrueOnLastAttempt()
    {
        // Arrange
        var attempt = new RetryAttempt(3, 3, null, null, TimeSpan.Zero);

        // Assert
        attempt.IsLastAttempt.Should().BeTrue();
    }

    [Fact]
    public void RetryAttempt_IsLastAttempt_ReturnsFalseBeforeLastAttempt()
    {
        // Arrange
        var attempt = new RetryAttempt(1, 3, null, null, TimeSpan.Zero);

        // Assert
        attempt.IsLastAttempt.Should().BeFalse();
    }

    #endregion

    #region ExecuteWithRetry Tests

    [Fact]
    public async Task ExecuteWithRetryAsync_SuccessOnFirstAttempt_ReturnsResult()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("success");

        // Act
        var result = await Command.Run(cmd)
            .WithArguments(args)
            .ExecuteWithRetryAsync(RetryOptions.Default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ExitCode.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithZeroAttempts_ThrowsArgumentException()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("test");
        var options = new RetryOptions { MaxAttempts = 0 };

        // Act
        var act = () => Command.Run(cmd)
            .WithArguments(args)
            .ExecuteWithRetryAsync(options)
            .AsTask();

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_FailsAllAttempts_ThrowsException()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetExitCodeCommand(1);
        var options = RetryOptions.Immediate(maxAttempts: 3);

        // Act
        var act = () => Command.Run(cmd)
            .WithArguments(args)
            .ExecuteWithRetryAsync(options)
            .AsTask();

        // Assert
        await act.Should().ThrowAsync<CommandExecutionException>()
            .Where(e => e.ExitCode == 1);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_OnRetryCallback_IsCalledOnEachRetry()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetExitCodeCommand(1);
        var retryCount = 0;
        var options = new RetryOptions
        {
            MaxAttempts = 3,
            Delay = TimeSpan.Zero,
            UseJitter = false,
            OnRetry = attempt =>
            {
                retryCount++;
                attempt.AttemptNumber.Should().Be(retryCount);
                attempt.MaxAttempts.Should().Be(3);
            }
        };

        // Act
        try
        {
            await Command.Run(cmd)
                .WithArguments(args)
                .ExecuteWithRetryAsync(options);
        }
        catch (CommandExecutionException)
        {
            // Expected
        }

        // Assert
        retryCount.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ShouldRetryOnExitCode_RespectsFilter()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetExitCodeCommand(42);
        var attemptCount = 0;
        var options = new RetryOptions
        {
            MaxAttempts = 3,
            Delay = TimeSpan.Zero,
            UseJitter = false,
            ShouldRetryOnExitCode = code => code == 1,
            OnRetry = _ => attemptCount++
        };

        // Act
        var act = () => Command.Run(cmd)
            .WithArguments(args)
            .ExecuteWithRetryAsync(options)
            .AsTask();

        // Assert
        await act.Should().ThrowAsync<CommandExecutionException>()
            .Where(e => e.ExitCode == 42);

        attemptCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ConfigureAction_Works()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("test");

        // Act
        var result = await Command.Run(cmd)
            .WithArguments(args)
            .ExecuteWithRetryAsync(options =>
            {
                options.MaxAttempts = 2;
                options.Delay = TimeSpan.Zero;
            });

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteWithRetryAndReadOutputAsync_ReturnsOutput()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetEchoCommand("retry output");

        // Act
        var output = await Command.Run(cmd)
            .WithArguments(args)
            .ExecuteWithRetryAndReadOutputAsync(RetryOptions.Immediate(2));

        // Assert
        output.Should().Contain("retry output");
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_RespectsMaxDelay()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetExitCodeCommand(1);
        var delays = new List<TimeSpan>();
        var options = new RetryOptions
        {
            MaxAttempts = 4,
            Delay = TimeSpan.FromSeconds(10),
            BackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(15),
            UseJitter = false,
            OnRetry = attempt => delays.Add(attempt.NextDelay)
        };

        // Act
        try
        {
            await Command.Run(cmd)
                .WithArguments(args)
                .ExecuteWithRetryAsync(options);
        }
        catch (CommandExecutionException)
        {
            // Expected
        }

        // Assert
        // First delay: 10s, Second: 15s (capped), Third: 15s (capped)
        delays.Should().HaveCount(3);
        delays[0].Should().Be(TimeSpan.FromSeconds(10));
        delays[1].Should().Be(TimeSpan.FromSeconds(15)); // Capped
        delays[2].Should().Be(TimeSpan.FromSeconds(15)); // Capped
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var (cmd, args) = TestHelper.GetSleepCommand(10);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var act = () => Command.Run(cmd)
            .WithArguments(args)
            .ExecuteWithRetryAsync(RetryOptions.Default, cts.Token)
            .AsTask();

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion
}