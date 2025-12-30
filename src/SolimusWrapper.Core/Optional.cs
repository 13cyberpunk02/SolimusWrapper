namespace SolimusWrapper.Core;

/// <summary>
/// Обёртка для различения "не передано" и "передано null"
/// </summary>
internal readonly struct Optional<T>
{
    private readonly T _value;
    
    public bool HasValue { get; }
    public T Value => HasValue ? _value : throw new InvalidOperationException("No value");

    private Optional(T value)
    {
        _value = value;
        HasValue = true;
    }

    public static implicit operator Optional<T>(T value) => new(value);
    
    public static Optional<T> None => default;
}