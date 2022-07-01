// ReSharper disable UnusedMember.Global -- used dynamically through proxies

using System;
using System.Collections.Generic;
using System.Reflection;
using StardewModdingAPI.Utilities;

namespace SMAPI.Tests.ModApiProvider.Framework;

/// <summary>A mod-provided API which provides basic events, properties, and methods.</summary>
public class SimpleApi : BaseApi
{
    /*********
    ** Test interface
    *********/
    /****
    ** Events
    ****/
    /// <summary>A simple event field.</summary>
    public event EventHandler<int>? OnEventRaised;

    /// <summary>A simple event property with custom add/remove logic.</summary>
    public event EventHandler<int> OnEventRaisedProperty
    {
        add => this.OnEventRaised += value;
        remove => this.OnEventRaised -= value;
    }


    /****
    ** Properties
    ****/
    /// <summary>A simple numeric property.</summary>
    public int NumberProperty { get; set; }

    /// <summary>A simple object property.</summary>
    public object? ObjectProperty { get; set; }

    /// <summary>A simple list property.</summary>
    public List<string>? ListProperty { get; set; }

    /// <summary>A simple list property with an interface.</summary>
    public IList<string>? ListPropertyWithInterface { get; set; }

    /// <summary>A property with nested generics.</summary>
    public IDictionary<string, IList<string>>? GenericsProperty { get; set; }

    /// <summary>A property using an enum available to both mods.</summary>
    public BindingFlags EnumProperty { get; set; }

    /// <summary>A read-only property.</summary>
    public int GetterProperty => 42;


    /****
    ** Methods
    ****/
    /// <summary>A simple method with no return value.</summary>
    public void GetNothing() { }

    /// <summary>A simple method which returns a number.</summary>
    public int GetInt(int value)
    {
        return value;
    }

    /// <summary>A simple method which returns an object.</summary>
    public object GetObject(object value)
    {
        return value;
    }

    /// <summary>A simple method which returns a list.</summary>
    public List<string> GetList(string value)
    {
        return new() { value };
    }

    /// <summary>A simple method which returns a list with an interface.</summary>
    public IList<string> GetListWithInterface(string value)
    {
        return new List<string> { value };
    }

    /// <summary>A simple method which returns nested generics.</summary>
    public IDictionary<string, IList<string>> GetGenerics(string key, string value)
    {
        return new Dictionary<string, IList<string>>
        {
            [key] = new List<string> { value }
        };
    }

    /// <summary>A simple method which returns a lambda.</summary>
    public Func<string, string> GetLambda(Func<string, string> value)
    {
        return value;
    }

    /// <summary>A simple method which returns out parameters.</summary>
    public bool TryGetOutParameter(int inputNumber, out int outNumber, out string outString, out PerScreen<int> outReference, out IDictionary<int, PerScreen<int>> outComplexType)
    {
        outNumber = inputNumber;
        outString = inputNumber.ToString();
        outReference = new PerScreen<int>(() => inputNumber);
        outComplexType = new Dictionary<int, PerScreen<int>>
        {
            [inputNumber] = new PerScreen<int>(() => inputNumber)
        };

        return true;
    }


    /*********
    ** Helper methods
    *********/
    /// <summary>Raise the <see cref="OnEventRaised"/> event.</summary>
    /// <param name="value">The value to pass to the event.</param>
    public void RaiseEventField(int value)
    {
        this.OnEventRaised?.Invoke(null, value);
    }
}
