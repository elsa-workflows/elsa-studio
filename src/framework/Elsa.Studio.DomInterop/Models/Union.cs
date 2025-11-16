namespace Elsa.Studio.Models;

/// <summary>
/// Represents a discriminated union type that can hold one of two possible values.
/// </summary>
/// <typeparam name="T1">The type of the first possible value.</typeparam>
/// <typeparam name="T2">The type of the second possible value.</typeparam>
public class Union<T1, T2>
{
    private readonly T1? _value1;
    private readonly T2? _value2;
    private readonly bool _isValue1;

    /// <summary>
    /// Initializes a new instance of the Union with a value of the first type.
    /// </summary>
    /// <param name="value">The value of the first type.</param>
    protected Union(T1 value)
    {
        _value1 = value;
        _isValue1 = true;
    }

    /// <summary>
    /// Initializes a new instance of the Union with a value of the second type.
    /// </summary>
    /// <param name="value">The value of the second type.</param>
    protected Union(T2 value)
    {
        _value2 = value;
        _isValue1 = false;
    }

    /// <summary>
    /// Implicitly converts a value of the first type to a Union.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A Union containing the value.</returns>
    public static implicit operator Union<T1, T2>(T1 value) => new(value);

    /// <summary>
    /// Implicitly converts a value of the second type to a Union.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>A Union containing the value.</returns>
    public static implicit operator Union<T1, T2>(T2 value) => new(value);

    /// <summary>
    /// Matches the contained value and applies the appropriate function.
    /// </summary>
    /// <typeparam name="T">The return type of the match functions.</typeparam>
    /// <param name="case1">Function to apply if the Union contains a value of the first type.</param>
    /// <param name="case2">Function to apply if the Union contains a value of the second type.</param>
    /// <returns>The result of applying the appropriate function.</returns>
    /// <exception cref="ArgumentNullException">Thrown when case1 or case2 is null.</exception>
    public T Match<T>(Func<T1, T> case1, Func<T2, T> case2)
    {
        if (case1 == null) throw new ArgumentNullException(nameof(case1));
        if (case2 == null) throw new ArgumentNullException(nameof(case2));

        return _isValue1 ? case1(_value1!) : case2(_value2!);
    }

    /// <summary>
    /// Gets the raw value contained in this Union.
    /// </summary>
    /// <returns>The contained value as an object.</returns>
    public object Match() => _isValue1 ? _value1! : _value2!;
}