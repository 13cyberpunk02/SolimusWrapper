using System.Buffers;
using System.Text;

namespace SolimusWrapper.Core;

/// <summary>
/// Цель для перенаправления потока вывода
/// </summary>
public abstract class PipeTarget
{
    public static PipeTarget Null { get; } = new NullPipeTarget();
    
    public static PipeTarget ToStringBuilder(StringBuilder sb) => 
        new StringBuilderPipeTarget(sb);
    
    public static PipeTarget ToStream(Stream stream) => 
        new StreamPipeTarget(stream);
    
    public static PipeTarget ToDelegate(Action<string> handler) => 
        new DelegatePipeTarget(handler);
    
    public static PipeTarget ToFile(string filePath) =>
        new FilePipeTarget(filePath);

    internal abstract Task CopyFromAsync(StreamReader reader, CancellationToken ct);
}

file sealed class NullPipeTarget : PipeTarget
{
    internal override async Task CopyFromAsync(StreamReader reader, CancellationToken ct)
    {
        var buffer = ArrayPool<char>.Shared.Rent(4096);
        try
        {
            while (await reader.ReadAsync(buffer.AsMemory(), ct) > 0) { }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }
}

file sealed class StringBuilderPipeTarget(StringBuilder sb) : PipeTarget
{
    internal override async Task CopyFromAsync(StreamReader reader, CancellationToken ct)
    {
        var buffer = ArrayPool<char>.Shared.Rent(4096);
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
            ArrayPool<char>.Shared.Return(buffer);
        }
    }
}

file sealed class StreamPipeTarget(Stream stream) : PipeTarget
{
    internal override async Task CopyFromAsync(StreamReader reader, CancellationToken ct)
    {
        await reader.BaseStream.CopyToAsync(stream, ct);
    }
}

file sealed class DelegatePipeTarget(Action<string> handler) : PipeTarget
{
    internal override async Task CopyFromAsync(StreamReader reader, CancellationToken ct)
    {
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            handler(line);
        }
    }
}

file sealed class FilePipeTarget(string path) : PipeTarget
{
    internal override async Task CopyFromAsync(StreamReader reader, CancellationToken ct)
    {
        await using var fileStream = new FileStream(
            path, FileMode.Create, FileAccess.Write, FileShare.Read, 
            bufferSize: 4096, useAsync: true);
        
        await reader.BaseStream.CopyToAsync(fileStream, ct);
    }
}