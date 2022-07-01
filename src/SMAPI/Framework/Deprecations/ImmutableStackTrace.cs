using System.Diagnostics;

namespace StardewModdingAPI.Framework.Deprecations;

/// <summary>An immutable stack trace that caches its values.</summary>
internal class ImmutableStackTrace
{
    /*********
    ** Fields
    *********/
    /// <summary>The underlying stack trace.</summary>
    private readonly StackTrace StackTrace;

    /// <summary>The individual method calls in the stack trace.</summary>
    private StackFrame[]? Frames;

    /// <summary>The string representation of the stack trace.</summary>
    private string? StringForm;


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="stackTrace">The underlying stack trace.</param>
    public ImmutableStackTrace(StackTrace stackTrace)
    {
        this.StackTrace = stackTrace;
    }

    /// <summary>Get the underlying frames.</summary>
    /// <remarks>This is a reference to the underlying stack frames, so this array should not be edited.</remarks>
    public StackFrame[] GetFrames()
    {
        return this.Frames ??= this.StackTrace.GetFrames();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.StringForm ??= this.StackTrace.ToString();
    }

    /// <summary>Get the current stack trace.</summary>
    /// <param name="skipFrames">The number of frames up the stack from which to start the trace.</param>
    public static ImmutableStackTrace Get(int skipFrames = 0)
    {
        return new ImmutableStackTrace(
            new StackTrace(skipFrames: skipFrames + 1) // also skip this method
        );
    }
}
