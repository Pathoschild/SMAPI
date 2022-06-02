using System;
using System.IO;
using System.Text;

namespace StardewModdingAPI.Framework.Logging;

/// <summary>A text writer which allows intercepting output.</summary>
internal class InterceptingTextWriter : TextWriter
{
    /*********
    ** Fields
    *********/
    /// <summary>The event raised when a message is written to the console directly.</summary>
    private readonly Action<string> OnMessageIntercepted;


    /*********
    ** Accessors
    *********/
    /// <summary>Prefixing a message with this character indicates that the console interceptor should write the string without intercepting it. (The character itself is not written.)</summary>
    public const char IgnoreChar = '\u200B';

    /// <summary>The underlying console output.</summary>
    public TextWriter Out { get; }

    /// <inheritdoc />
    public override Encoding Encoding => this.Out.Encoding;

    /// <summary>Whether the text writer should ignore the next input if it's a newline.</summary>
    /// <remarks>This is used when log output is suppressed from the console, since <c>Console.WriteLine</c> writes the trailing newline as a separate call.</remarks>
    public bool IgnoreNextIfNewline { get; set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="output">The underlying output writer.</param>
    /// <param name="onMessageIntercepted">The event raised when a message is written to the console directly.</param>
    public InterceptingTextWriter(TextWriter output, Action<string> onMessageIntercepted)
    {
        this.Out = output;
        this.OnMessageIntercepted = onMessageIntercepted;
    }

    /// <inheritdoc />
    public override void Write(char[] buffer, int index, int count)
    {
        // track newline skip
        bool ignoreIfNewline = this.IgnoreNextIfNewline;
        this.IgnoreNextIfNewline = false;

        // get first character if valid
        if (count == 0 || index < 0 || index >= buffer.Length)
        {
            this.Out.Write(buffer, index, count);
            return;
        }
        char firstChar = buffer[index];

        // handle output
        if (firstChar == InterceptingTextWriter.IgnoreChar)
            this.Out.Write(buffer, index + 1, count - 1);
        else if (char.IsControl(firstChar) && firstChar is not ('\r' or '\n'))
            this.Out.Write(buffer, index, count);
        else if (this.IsEmptyOrNewline(buffer))
        {
            if (!ignoreIfNewline)
                this.Out.Write(buffer, index, count);
        }
        else
            this.OnMessageIntercepted(new string(buffer, index, count));
    }

    /// <inheritdoc />
    public override void Write(char ch)
    {
        this.Out.Write(ch);
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Get whether a buffer represents a line break.</summary>
    /// <param name="buffer">The buffer to check.</param>
    private bool IsEmptyOrNewline(char[] buffer)
    {
        foreach (char ch in buffer)
        {
            if (ch != '\n' && ch != '\r')
                return false;
        }

        return true;
    }
}