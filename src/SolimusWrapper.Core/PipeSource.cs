namespace SolimusWrapper.Core;

/// <summary>
/// Источник данных для stdin
/// </summary>
public abstract class PipeSource
{
    public static PipeSource Null { get; } = new NullPipeSource();

    public static PipeSource FromString(string text) => new StringPipeSource(text);
    public static PipeSource FromStream(Stream stream) => new StreamPipeSource(stream);
    public static PipeSource FromFile(string filePath) => new FilePipeSource(filePath);
    public static PipeSource FromBytes(byte[] data) => new BytesPipeSource(data);

    internal abstract Task CopyToAsync(StreamWriter writer, CancellationToken ct);
}

file sealed class NullPipeSource : PipeSource
{
    internal override Task CopyToAsync(StreamWriter writer, CancellationToken ct) 
        => Task.CompletedTask;
}

file sealed class StringPipeSource(string text) : PipeSource
{
    internal override async Task CopyToAsync(StreamWriter writer, CancellationToken ct)
    {
        await writer.WriteAsync(text.AsMemory(), ct);
        await writer.FlushAsync(ct);
    }
}

file sealed class StreamPipeSource(Stream stream) : PipeSource
{
    internal override async Task CopyToAsync(StreamWriter writer, CancellationToken ct)
    {
        await stream.CopyToAsync(writer.BaseStream, ct);
        await writer.FlushAsync(ct);
    }
}

file sealed class FilePipeSource(string path) : PipeSource
{
    internal override async Task CopyToAsync(StreamWriter writer, CancellationToken ct)
    {
        await using var fileStream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        
        await fileStream.CopyToAsync(writer.BaseStream, ct);
        await writer.FlushAsync(ct);
    }
}

file sealed class BytesPipeSource(byte[] data) : PipeSource
{
    internal override async Task CopyToAsync(StreamWriter writer, CancellationToken ct)
    {
        await writer.BaseStream.WriteAsync(data.AsMemory(), ct);
        await writer.FlushAsync(ct);
    }
}