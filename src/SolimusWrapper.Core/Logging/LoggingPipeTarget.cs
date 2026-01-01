using System.Buffers;
using System.Text;

namespace SolimusWrapper.Core.Logging;

/// <summary>
/// PipeTarget с поддержкой логирования
/// </summary>
internal sealed class LoggingPipeTarget : PipeTarget
{
    private readonly PipeTarget _inner;
    private readonly ICommandLogger _logger;
    private readonly bool _isStdErr;

    public LoggingPipeTarget(PipeTarget inner, ICommandLogger logger, bool isStdErr)
    {
        _inner = inner;
        _logger = logger;
        _isStdErr = isStdErr;
    }

    internal override async Task CopyFromAsync(StreamReader reader, CancellationToken ct)
    {
        var buffer = ArrayPool<char>.Shared.Rent(4096);
        var lineBuilder = new StringBuilder();

        try
        {
            int charsRead;
            while ((charsRead = await reader.ReadAsync(buffer.AsMemory(), ct)) > 0)
            {
                for (int i = 0; i < charsRead; i++)
                {
                    var c = buffer[i];

                    if (c == '\n')
                    {
                        var line = lineBuilder.ToString();
                        lineBuilder.Clear();

                        if (_isStdErr)
                            _logger.LogStandardError(line);
                        else
                            _logger.LogStandardOutput(line);
                    }
                    else if (c != '\r')
                    {
                        lineBuilder.Append(c);
                    }
                }
            }

            if (lineBuilder.Length > 0)
            {
                var line = lineBuilder.ToString();
                if (_isStdErr)
                    _logger.LogStandardError(line);
                else
                    _logger.LogStandardOutput(line);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }
    }
}