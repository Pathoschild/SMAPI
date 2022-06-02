using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

/// <summary>A set of changes which can be applied to a mod data field.</summary>
public class ChangeDescriptor
{
    /*********
    ** Accessors
    *********/
    /// <summary>The values to add to the field.</summary>
    public ISet<string> Add { get; }

    /// <summary>The values to remove from the field.</summary>
    public ISet<string> Remove { get; }

    /// <summary>The values to replace in the field, if matched.</summary>
    public IReadOnlyDictionary<string, string> Replace { get; }

    /// <summary>Whether the change descriptor would make any changes.</summary>
    public bool HasChanges { get; }

    /// <summary>Format a raw value into a normalized form.</summary>
    public Func<string, string> FormatValue { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="add">The values to add to the field.</param>
    /// <param name="remove">The values to remove from the field.</param>
    /// <param name="replace">The values to replace in the field, if matched.</param>
    /// <param name="formatValue">Format a raw value into a normalized form.</param>
    public ChangeDescriptor(ISet<string> add, ISet<string> remove, IReadOnlyDictionary<string, string> replace, Func<string, string> formatValue)
    {
        this.Add = add;
        this.Remove = remove;
        this.Replace = replace;
        this.HasChanges = add.Any() || remove.Any() || replace.Any();
        this.FormatValue = formatValue;
    }

    /// <summary>Apply the change descriptors to a comma-delimited field.</summary>
    /// <param name="rawField">The raw field text.</param>
    /// <returns>Returns the modified field.</returns>
#if NET5_0_OR_GREATER
    [return: NotNullIfNotNull("rawField")]
#endif
    public string? ApplyToCopy(string? rawField)
    {
        // get list
        List<string> values = !string.IsNullOrWhiteSpace(rawField)
            ? new List<string>(
                from field in rawField.Split(',')
                let value = field.Trim()
                where value.Length > 0
                select value
            )
            : new List<string>();

        // apply changes
        this.Apply(values);

        // format
        if (rawField == null && !values.Any())
            return null;
        return string.Join(", ", values);
    }

    /// <summary>Apply the change descriptors to the given field values.</summary>
    /// <param name="values">The field values.</param>
    /// <returns>Returns the modified field values.</returns>
    public void Apply(List<string> values)
    {
        // replace/remove values
        if (this.Replace.Any() || this.Remove.Any())
        {
            for (int i = values.Count - 1; i >= 0; i--)
            {
                string value = this.FormatValue(values[i].Trim());

                if (this.Remove.Contains(value))
                    values.RemoveAt(i);

                else if (this.Replace.TryGetValue(value, out string? newValue))
                    values[i] = newValue;
            }
        }

        // add values
        if (this.Add.Any())
        {
            HashSet<string> curValues = new HashSet<string>(values.Select(p => p.Trim()), StringComparer.OrdinalIgnoreCase);
            foreach (string add in this.Add)
            {
                if (!curValues.Contains(add))
                {
                    values.Add(add);
                    curValues.Add(add);
                }
            }
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (!this.HasChanges)
            return string.Empty;

        List<string> descriptors = new List<string>(this.Add.Count + this.Remove.Count + this.Replace.Count);
        foreach (string add in this.Add)
            descriptors.Add($"+{add}");
        foreach (string remove in this.Remove)
            descriptors.Add($"-{remove}");
        foreach (var pair in this.Replace)
            descriptors.Add($"{pair.Key} → {pair.Value}");

        return string.Join(", ", descriptors);
    }

    /// <summary>Parse a raw change descriptor string into a <see cref="ChangeDescriptor"/> model.</summary>
    /// <param name="descriptor">The raw change descriptor.</param>
    /// <param name="errors">The human-readable error message describing any invalid values that were ignored.</param>
    /// <param name="formatValue">Format a raw value into a normalized form if needed.</param>
    public static ChangeDescriptor Parse(string? descriptor, out string[] errors, Func<string, string>? formatValue = null)
    {
        // init
        formatValue ??= p => p;
        var add = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var remove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var replace = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // parse each change in the descriptor
        if (!string.IsNullOrWhiteSpace(descriptor))
        {
            List<string> rawErrors = new List<string>();
            foreach (string rawEntry in descriptor.Split(','))
            {
                // normalize entry
                string entry = rawEntry.Trim();
                if (entry == string.Empty)
                    continue;

                // parse as replace (old value → new value)
                if (entry.Contains('→'))
                {
                    string[] parts = entry.Split(new[] { '→' }, 2);
                    string oldValue = formatValue(parts[0].Trim());
                    string newValue = formatValue(parts[1].Trim());

                    if (oldValue == string.Empty)
                    {
                        rawErrors.Add($"Failed parsing '{rawEntry}': can't map from a blank old value. Use the '+value' format to add a value.");
                        continue;
                    }

                    if (newValue == string.Empty)
                    {
                        rawErrors.Add($"Failed parsing '{rawEntry}': can't map to a blank value. Use the '-value' format to remove a value.");
                        continue;
                    }

                    replace[oldValue] = newValue;
                }

                // else as remove
                else if (entry.StartsWith("-"))
                {
                    entry = formatValue(entry.Substring(1).Trim());
                    remove.Add(entry);
                }

                // else as add
                else
                {
                    if (entry.StartsWith("+"))
                        entry = formatValue(entry.Substring(1).Trim());
                    add.Add(entry);
                }
            }

            errors = rawErrors.ToArray();
        }
        else
            errors = Array.Empty<string>();

        // build model
        return new ChangeDescriptor(
            add: add,
            remove: remove,
            replace: new ReadOnlyDictionary<string, string>(replace),
            formatValue: formatValue
        );
    }
}