namespace Washu.Todo;

public readonly struct Percentage
{
    public double Value { get; }
    public Percentage(double value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1.0);
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0.0);
        Value = value;
    }

    public string ToString(byte decimalPlaces) => $"{Math.Round(Value * 100.0, decimalPlaces)}%";
}