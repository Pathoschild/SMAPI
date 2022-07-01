namespace StardewModdingAPI.Framework.Utilities;

/// <summary>Counts down from a baseline value.</summary>
internal class Countdown
{
    /*********
    ** Accessors
    *********/
    /// <summary>The initial value from which to count down.</summary>
    public int Initial { get; }

    /// <summary>The current value.</summary>
    public int Current { get; private set; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="initial">The initial value from which to count down.</param>
    public Countdown(int initial)
    {
        this.Initial = initial;
        this.Current = initial;
    }

    /// <summary>Reduce the current value by one.</summary>
    /// <returns>Returns whether the value was decremented (i.e. wasn't already zero).</returns>
    public bool Decrement()
    {
        if (this.Current <= 0)
            return false;

        this.Current--;
        return true;
    }

    /// <summary>Restart the countdown.</summary>
    public void Reset()
    {
        this.Current = this.Initial;
    }
}
