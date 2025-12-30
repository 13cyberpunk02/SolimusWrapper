using System.Text;
using SolimusWrapper.Core;
using SolimusWrapper.Core.Extensions;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

//simple run
var result = await Command.Run("dotnet")
    .WithArguments("--version")
    .ExecuteAsync();
    
Console.WriteLine($"Exit code: {result.ExitCode}, Time: {result.RunTime}");

//get output
var version = await Command.Run("dotnet")
    .WithArguments("--version")
    .ExecuteAndReadOutputAsync();
    
Console.WriteLine($"Version: {version}");


// With redirect
var stdOut = new StringBuilder();
var stdErr = new StringBuilder();

await Command.Run("dotnet")
    .WithArguments("build", "-v", "minimal")
    .WithWorkingDirectory("path/to/project/") //Specify your path
    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOut))
    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErr))
    .WithValidation(throwOnNonZero: false)
    .ExecuteAsync();

// Realtime output
var encoding = OperatingSystem.IsWindows() 
    ? Encoding.GetEncoding(866) 
    : Encoding.UTF8;

await Command.Run("ping")
    .WithArguments("-n", "4", "google.com")
    .WithEncoding(encoding)
    .WithStandardOutputPipe(PipeTarget.ToDelegate(Console.WriteLine))
    .ExecuteAsync();
    
// Shell command
var files = OperatingSystem.IsWindows()
    ? await CommandExtensions.Shell("dir").ExecuteAndReadOutputAsync()
    : await CommandExtensions.Shell("ls -la").ExecuteAndReadOutputAsync();

Console.WriteLine(files, encoding);
