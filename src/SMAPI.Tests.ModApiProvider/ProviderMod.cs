using System.Collections.Generic;
using System.Reflection;
using SMAPI.Tests.ModApiProvider.Framework;

namespace SMAPI.Tests.ModApiProvider;

/// <summary>A simulated mod instance.</summary>
public class ProviderMod
{
    /// <summary>The underlying API instance.</summary>
    private readonly SimpleApi Api = new();

    /// <summary>Get the mod API instance.</summary>
    public object GetModApi()
    {
        return this.Api;
    }

    /// <summary>Raise the <see cref="SimpleApi.OnEventRaised"/> event.</summary>
    /// <param name="value">The value to send as an event argument.</param>
    public void RaiseEvent(int value)
    {
        this.Api.RaiseEventField(value);
    }

    /// <summary>Set the values for the API property.</summary>
    public void SetPropertyValues(int number, object obj, string listValue, string listWithInterfaceValue, string dictionaryKey, string dictionaryListValue, BindingFlags enumValue, string inheritedValue)
    {
        this.Api.NumberProperty = number;
        this.Api.ObjectProperty = obj;
        this.Api.ListProperty = new List<string> { listValue };
        this.Api.ListPropertyWithInterface = new List<string> { listWithInterfaceValue };
        this.Api.GenericsProperty = new Dictionary<string, IList<string>> { [dictionaryKey] = new List<string> { dictionaryListValue } };
        this.Api.EnumProperty = enumValue;
        this.Api.InheritedProperty = inheritedValue;
    }
}
