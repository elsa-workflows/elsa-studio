namespace Elsa.Studio.DomInterop.Models;

public class Union<T1, T2>
{
    private readonly T1? _value1;
    private readonly T2? _value2;
    private readonly bool _isValue1;

    protected Union(T1 value)
    {
        _value1 = value;
        _isValue1 = true;
    }

    protected Union(T2 value)
    {
        _value2 = value;
        _isValue1 = false;
    }

    public static implicit operator Union<T1, T2>(T1 value) => new(value);
    public static implicit operator Union<T1, T2>(T2 value) => new(value);

    public T Match<T>(Func<T1, T> case1, Func<T2, T> case2)
    {
        if (case1 == null) throw new ArgumentNullException(nameof(case1));
        if (case2 == null) throw new ArgumentNullException(nameof(case2));

        return _isValue1 ? case1(_value1!) : case2(_value2!);
    }

    public object Match() => _isValue1 ? _value1! : _value2!;
}