using System;
using System.Collections.Generic;
using System.Reflection;
using StardewModdingAPI.Utilities;

namespace SMAPI.Tests.ModApiConsumer.Interfaces;

/// <summary>A mod-provided API which provides basic events, properties, and methods.</summary>
public interface ISimpleApi
{
    /*********
    ** Test interface
    *********/
    /****
    ** Events
    ****/
    /// <summary>A simple event field.</summary>
    event EventHandler<int> OnEventRaised;

    /// <summary>A simple event property with custom add/remove logic.</summary>
    event EventHandler<int> OnEventRaisedProperty;


    /****
    ** Properties
    ****/
    /// <summary>A simple numeric property.</summary>
    int NumberProperty { get; set; }

    /// <summary>A simple object property.</summary>
    object ObjectProperty { get; set; }

    /// <summary>A simple list property.</summary>
    List<string> ListProperty { get; set; }

    /// <summary>A simple list property with an interface.</summary>
    IList<string> ListPropertyWithInterface { get; set; }

    /// <summary>A property with nested generics.</summary>
    IDictionary<string, IList<string>> GenericsProperty { get; set; }

    /// <summary>A property using an enum available to both mods.</summary>
    BindingFlags EnumProperty { get; set; }

    /// <summary>A read-only property.</summary>
    int GetterProperty { get; }


    /****
    ** Methods
    ****/
    /// <summary>A simple method with no return value.</summary>
    void GetNothing();

    /// <summary>A simple method which returns a number.</summary>
    int GetInt(int value);

    /// <summary>A simple method which returns an object.</summary>
    object GetObject(object value);

    /// <summary>A simple method which returns a list.</summary>
    List<string> GetList(string value);

    /// <summary>A simple method which returns a list with an interface.</summary>
    IList<string> GetListWithInterface(string value);

    /// <summary>A simple method which returns nested generics.</summary>
    IDictionary<string, IList<string>> GetGenerics(string key, string value);

    /// <summary>A simple method which returns a lambda.</summary>
    Func<string, string> GetLambda(Func<string, string> value);

    /// <summary>A simple method which returns out parameters.</summary>
    bool TryGetOutParameter(int inputNumber, out int outNumber, out string outString, out PerScreen<int> outReference, out IDictionary<int, PerScreen<int>> outComplexType);


    /****
    ** Inherited members
    ****/
    /// <summary>A property inherited from a base class.</summary>
    public string InheritedProperty { get; set; }
}
