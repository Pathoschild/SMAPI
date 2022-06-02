// ReSharper disable CheckNamespace -- matches Stardew Valley's code
namespace Netcode;

/// <summary>A simplified version of Stardew Valley's <c>Netcode.NetFieldBase</c> for unit testing.</summary>
/// <typeparam name="T">The type of the synchronized value.</typeparam>
/// <typeparam name="TSelf">The type of the current instance.</typeparam>
public class NetFieldBase<T, TSelf> where TSelf : NetFieldBase<T, TSelf>
{
    /// <summary>The synchronised value.</summary>
    public T? Value { get; set; }

    /// <summary>Implicitly convert a net field to the its type.</summary>
    /// <param name="field">The field to convert.</param>
    public static implicit operator T?(NetFieldBase<T, TSelf> field)
    {
        return field.Value;
    }
}
