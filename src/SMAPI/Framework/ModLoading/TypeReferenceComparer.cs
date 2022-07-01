using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace StardewModdingAPI.Framework.ModLoading;

/// <summary>Performs heuristic equality checks for <see cref="TypeReference"/> instances.</summary>
/// <remarks>
/// This implementation compares <see cref="TypeReference"/> instances to see if they likely
/// refer to the same type. While the implementation is obvious for types like <c>System.Bool</c>,
/// this class mainly exists to handle cases like <c>System.Collections.Generic.Dictionary`2&lt;!0,Netcode.NetRoot`1&lt;!1&gt;&gt;</c>
/// and <c>System.Collections.Generic.Dictionary`2&lt;TKey,Netcode.NetRoot`1&lt;TValue&gt;&gt;</c>
/// which are compatible, but not directly comparable. It does this by splitting each type name
/// into its component token types, and performing placeholder substitution (e.g. <c>!0</c> to
/// <c>TKey</c> in the above example). If all components are equal after substitution, and the
/// tokens can all be mapped to the same generic type, the types are considered equal.
/// </remarks>
internal class TypeReferenceComparer : IEqualityComparer<TypeReference?>
{
    /*********
    ** Public methods
    *********/
    /// <summary>Get whether the specified objects are equal.</summary>
    /// <param name="a">The first object to compare.</param>
    /// <param name="b">The second object to compare.</param>
    public bool Equals(TypeReference? a, TypeReference? b)
    {
        if (a == null || b == null)
            return a == b;

        return
            a == b
            || a.FullName == b.FullName
            || this.HeuristicallyEquals(a, b);
    }

    /// <summary>Get a hash code for the specified object.</summary>
    /// <param name="obj">The object for which a hash code is to be returned.</param>
    /// <exception cref="T:System.ArgumentNullException">The object type is a reference type and <paramref name="obj" /> is null.</exception>
    public int GetHashCode(TypeReference obj)
    {
        return obj.GetHashCode();
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get whether two types are heuristically equal based on generic type token substitution.</summary>
    /// <param name="typeA">The first type to compare.</param>
    /// <param name="typeB">The second type to compare.</param>
    private bool HeuristicallyEquals(TypeReference typeA, TypeReference typeB)
    {
        bool HeuristicallyEqualsImpl(string typeNameA, string typeNameB, IDictionary<string, string> tokenMap)
        {
            // analyze type names
            bool hasTokensA = typeNameA.Contains("!");
            bool hasTokensB = typeNameB.Contains("!");
            bool isTokenA = hasTokensA && typeNameA[0] == '!';
            bool isTokenB = hasTokensB && typeNameB[0] == '!';

            // validate
            if (!hasTokensA && !hasTokensB)
                return typeNameA == typeNameB; // no substitution needed
            if (hasTokensA && hasTokensB)
                throw new InvalidOperationException("Can't compare two type names when both contain generic type tokens.");

            // perform substitution if applicable
            if (isTokenA)
                typeNameA = this.MapPlaceholder(placeholder: typeNameA, type: typeNameB, map: tokenMap);
            if (isTokenB)
                typeNameB = this.MapPlaceholder(placeholder: typeNameB, type: typeNameA, map: tokenMap);

            // compare inner tokens
            string[] symbolsA = this.GetTypeSymbols(typeNameA).ToArray();
            string[] symbolsB = this.GetTypeSymbols(typeNameB).ToArray();
            if (symbolsA.Length != symbolsB.Length)
                return false;

            for (int i = 0; i < symbolsA.Length; i++)
            {
                if (!HeuristicallyEqualsImpl(symbolsA[i], symbolsB[i], tokenMap))
                    return false;
            }

            return true;
        }

        return HeuristicallyEqualsImpl(typeA.FullName, typeB.FullName, new Dictionary<string, string>());
    }

    /// <summary>Map a generic type placeholder (like <c>!0</c>) to its actual type.</summary>
    /// <param name="placeholder">The token placeholder.</param>
    /// <param name="type">The actual type.</param>
    /// <param name="map">The map of token to map substitutions.</param>
    /// <returns>Returns the previously-mapped type if applicable, else the <paramref name="type"/>.</returns>
    private string MapPlaceholder(string placeholder, string type, IDictionary<string, string> map)
    {
        if (map.TryGetValue(placeholder, out string? result))
            return result;

        map[placeholder] = type;
        return type;
    }

    /// <summary>Get the top-level type symbols in a type name (e.g. <code>List</code> and <code>NetRef&lt;T&gt;</code> in <code>List&lt;NetRef&lt;T&gt;&gt;</code>)</summary>
    /// <param name="typeName">The full type name.</param>
    private IEnumerable<string> GetTypeSymbols(string typeName)
    {
        int openGenerics = 0;

        Queue<char> queue = new Queue<char>(typeName);
        string symbol = "";
        while (queue.Any())
        {
            char ch = queue.Dequeue();
            switch (ch)
            {
                // skip `1 generic type identifiers
                case '`':
                    while (int.TryParse(queue.Peek().ToString(), out int _))
                        queue.Dequeue();
                    break;

                // start generic args
                case '<':
                    switch (openGenerics)
                    {
                        // start new generic symbol
                        case 0:
                            yield return symbol;
                            symbol = "";
                            openGenerics++;
                            break;

                        // continue accumulating nested type symbol
                        default:
                            symbol += ch;
                            openGenerics++;
                            break;
                    }
                    break;

                // generic args delimiter
                case ',':
                    switch (openGenerics)
                    {
                        // invalid
                        case 0:
                            throw new InvalidOperationException($"Encountered unexpected comma in type name: {typeName}.");

                        // start next generic symbol
                        case 1:
                            yield return symbol;
                            symbol = "";
                            break;

                        // continue accumulating nested type symbol
                        default:
                            symbol += ch;
                            break;
                    }
                    break;


                // end generic args
                case '>':
                    switch (openGenerics)
                    {
                        // invalid
                        case 0:
                            throw new InvalidOperationException($"Encountered unexpected closing generic in type name: {typeName}.");

                        // end generic symbol
                        case 1:
                            yield return symbol;
                            symbol = "";
                            openGenerics--;
                            break;

                        // continue accumulating nested type symbol
                        default:
                            symbol += ch;
                            openGenerics--;
                            break;
                    }
                    break;

                // continue symbol
                default:
                    symbol += ch;
                    break;
            }
        }

        if (symbol != "")
            yield return symbol;
    }
}
