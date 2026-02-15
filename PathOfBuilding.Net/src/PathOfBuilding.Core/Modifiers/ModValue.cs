namespace PathOfBuilding.Core.Modifiers;

public enum ModValueKind : byte
{
    Number,
    Boolean,
    Complex,
}

/// <summary>
/// A discriminated union that holds a modifier's value.
/// Most modifiers carry a numeric value; FLAG mods carry a boolean;
/// LIST mods may carry a complex object.
/// </summary>
public readonly struct ModValue : IEquatable<ModValue>
{
    public ModValueKind Kind { get; }
    public double Number { get; }
    public bool Boolean { get; }
    public object? Complex { get; }

    private ModValue(double number)
    {
        Kind = ModValueKind.Number;
        Number = number;
    }

    private ModValue(bool boolean)
    {
        Kind = ModValueKind.Boolean;
        Boolean = boolean;
    }

    private ModValue(object complex)
    {
        Kind = ModValueKind.Complex;
        Complex = complex;
    }

    public static implicit operator ModValue(double value) => new(value);
    public static implicit operator ModValue(int value) => new((double)value);
    public static implicit operator ModValue(bool value) => new(value);

    public static ModValue FromComplex(object value) => new(value);

    public double AsNumber() => Kind == ModValueKind.Number
        ? Number
        : throw new InvalidOperationException($"ModValue is {Kind}, not Number");

    public bool AsBoolean() => Kind == ModValueKind.Boolean
        ? Boolean
        : throw new InvalidOperationException($"ModValue is {Kind}, not Boolean");

    public override string ToString() => Kind switch
    {
        ModValueKind.Number => Number.ToString("G"),
        ModValueKind.Boolean => Boolean.ToString(),
        ModValueKind.Complex => Complex?.ToString() ?? "null",
        _ => "?",
    };

    public bool Equals(ModValue other) => Kind == other.Kind && Kind switch
    {
        ModValueKind.Number => Number == other.Number,
        ModValueKind.Boolean => Boolean == other.Boolean,
        ModValueKind.Complex => Equals(Complex, other.Complex),
        _ => false,
    };

    public override bool Equals(object? obj) => obj is ModValue other && Equals(other);
    public override int GetHashCode() => Kind switch
    {
        ModValueKind.Number => HashCode.Combine(Kind, Number),
        ModValueKind.Boolean => HashCode.Combine(Kind, Boolean),
        ModValueKind.Complex => HashCode.Combine(Kind, Complex),
        _ => 0,
    };

    public static bool operator ==(ModValue left, ModValue right) => left.Equals(right);
    public static bool operator !=(ModValue left, ModValue right) => !left.Equals(right);
}
