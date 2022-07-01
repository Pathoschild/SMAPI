using System;
using System.Reflection;

namespace StardewModdingAPI.Framework.Reflection;

/// <summary>A property obtained through reflection.</summary>
/// <typeparam name="TValue">The property value type.</typeparam>
internal class ReflectedProperty<TValue> : IReflectedProperty<TValue>
{
    /*********
    ** Fields
    *********/
    /// <summary>The display name shown in error messages.</summary>
    private readonly string DisplayName;

    /// <summary>The underlying property getter.</summary>
    private readonly Func<TValue>? GetMethod;

    /// <summary>The underlying property setter.</summary>
    private readonly Action<TValue>? SetMethod;


    /*********
    ** Accessors
    *********/
    /// <inheritdoc />
    public PropertyInfo PropertyInfo { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="parentType">The type that has the property.</param>
    /// <param name="obj">The object that has the instance property, or <c>null</c> for a static property.</param>
    /// <param name="property">The reflection metadata.</param>
    /// <param name="isStatic">Whether the property is static.</param>
    /// <exception cref="ArgumentNullException">The <paramref name="parentType"/> or <paramref name="property"/> is null.</exception>
    /// <exception cref="ArgumentException">The <paramref name="obj"/> is null for a non-static property, or not null for a static property.</exception>
    public ReflectedProperty(Type parentType, object? obj, PropertyInfo property, bool isStatic)
    {
        // validate input
        if (parentType == null)
            throw new ArgumentNullException(nameof(parentType));
        if (property == null)
            throw new ArgumentNullException(nameof(property));

        // validate static
        if (isStatic && obj != null)
            throw new ArgumentException("A static property cannot have an object instance.");
        if (!isStatic && obj == null)
            throw new ArgumentException("A non-static property must have an object instance.");


        this.DisplayName = $"{parentType.FullName}::{property.Name}";
        this.PropertyInfo = property;

        if (this.PropertyInfo.GetMethod != null)
            this.GetMethod = (Func<TValue>)Delegate.CreateDelegate(typeof(Func<TValue>), obj, this.PropertyInfo.GetMethod);
        if (this.PropertyInfo.SetMethod != null)
            this.SetMethod = (Action<TValue>)Delegate.CreateDelegate(typeof(Action<TValue>), obj, this.PropertyInfo.SetMethod);
    }

    /// <inheritdoc />
    public TValue GetValue()
    {
        if (this.GetMethod == null)
            throw new InvalidOperationException($"The {this.DisplayName} property has no get method.");

        try
        {
            return this.GetMethod();
        }
        catch (InvalidCastException)
        {
            throw new InvalidCastException($"Can't convert the {this.DisplayName} property from {this.PropertyInfo.PropertyType.FullName} to {typeof(TValue).FullName}.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Couldn't get the value of the {this.DisplayName} property", ex);
        }
    }

    /// <inheritdoc />
    public void SetValue(TValue value)
    {
        if (this.SetMethod == null)
            throw new InvalidOperationException($"The {this.DisplayName} property has no set method.");

        try
        {
            this.SetMethod(value);
        }
        catch (InvalidCastException)
        {
            throw new InvalidCastException($"Can't assign the {this.DisplayName} property a {typeof(TValue).FullName} value, must be compatible with {this.PropertyInfo.PropertyType.FullName}.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Couldn't set the value of the {this.DisplayName} property", ex);
        }
    }
}
