using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Newtonsoft.Json;
using StardewModdingAPI.Framework;
using StardewValley;

namespace StardewModdingAPI.Utilities;

/// <summary>Represents a Stardew Valley date.</summary>
public class SDate : IEquatable<SDate>
{
    /*********
    ** Fields
    *********/
    /// <summary>The internal season names in order.</summary>
    private readonly string[] Seasons = { "spring", "summer", "fall", "winter" };

    /// <summary>The number of seasons in a year.</summary>
    private int SeasonsInYear => this.Seasons.Length;

    /// <summary>The number of days in a season.</summary>
    private readonly int DaysInSeason = 28;

    /// <summary>The core SMAPI translations.</summary>
    internal static Translator? Translations;


    /*********
    ** Accessors
    *********/
    /// <summary>The day of month.</summary>
    public int Day { get; }

    /// <summary>The season name.</summary>
    public string Season { get; }

    /// <summary>The index of the season (where 0 is spring, 1 is summer, 2 is fall, and 3 is winter).</summary>
    /// <remarks>This is used in some game calculations (e.g. seasonal game sprites) and methods (e.g. <see cref="Utility.getSeasonNameFromNumber"/>).</remarks>
    [JsonIgnore]
    public int SeasonIndex { get; }

    /// <summary>The year.</summary>
    public int Year { get; }

    /// <summary>The day of week.</summary>
    [JsonIgnore]
    public DayOfWeek DayOfWeek { get; }

    /// <summary>The number of days since the game began (starting at 1 for the first day of spring in Y1).</summary>
    [JsonIgnore]
    public int DaysSinceStart { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="day">The day of month.</param>
    /// <param name="season">The season name.</param>
    /// <exception cref="ArgumentException">One of the arguments has an invalid value (like day 35).</exception>
    public SDate(int day, string season)
        : this(day, season, Game1.year) { }

    /// <summary>Construct an instance.</summary>
    /// <param name="day">The day of month.</param>
    /// <param name="season">The season name.</param>
    /// <param name="year">The year.</param>
    /// <exception cref="ArgumentException">One of the arguments has an invalid value (like day 35).</exception>
    [JsonConstructor]
    public SDate(int day, string season, int year)
        : this(day, season, year, allowDayZero: false) { }

    /// <summary>Get the current in-game date.</summary>
    public static SDate Now()
    {
        return new SDate(Game1.dayOfMonth, Game1.currentSeason, Game1.year, allowDayZero: true);
    }

    /// <summary>Get a date from the number of days after 0 spring Y1.</summary>
    /// <param name="daysSinceStart">The number of days since 0 spring Y1.</param>
    public static SDate FromDaysSinceStart(int daysSinceStart)
    {
        try
        {
            return new SDate(0, "spring", 1, allowDayZero: true).AddDays(daysSinceStart);
        }
        catch (ArithmeticException)
        {
            throw new ArgumentException($"Invalid daysSinceStart '{daysSinceStart}', must be at least 1.");
        }
    }

    /// <summary>Get a date from a game date instance.</summary>
    /// <param name="date">The world date.</param>
    [return: NotNullIfNotNull("date")]
    public static SDate? From(WorldDate? date)
    {
        if (date == null)
            return null;

        return new SDate(date.DayOfMonth, date.Season, date.Year, allowDayZero: true);
    }

    /// <summary>Get a new date with the given number of days added.</summary>
    /// <param name="offset">The number of days to add.</param>
    /// <returns>Returns the resulting date.</returns>
    /// <exception cref="ArithmeticException">The offset would result in an invalid date (like year 0).</exception>
    public SDate AddDays(int offset)
    {
        // get new hash code
        int hashCode = this.DaysSinceStart + offset;
        if (hashCode < 1)
            throw new ArithmeticException($"Adding {offset} days to {this} would result in a date before 01 spring Y1.");

        // get day
        int day = hashCode % 28;
        if (day == 0)
            day = 28;

        // get season index
        int seasonIndex = hashCode / 28;
        if (seasonIndex > 0 && hashCode % 28 == 0)
            seasonIndex -= 1;
        seasonIndex %= 4;

        // get year
        int year = (int)Math.Ceiling(hashCode / (this.Seasons.Length * this.DaysInSeason * 1m));

        // create date
        return new SDate(day, this.Seasons[seasonIndex], year);
    }

    /// <summary>Get a game date representation of the date.</summary>
    public WorldDate ToWorldDate()
    {
        return new WorldDate(this.Year, this.Season, this.Day);
    }

    /// <summary>Get an untranslated string representation of the date. This is mainly intended for debugging or console messages.</summary>
    public override string ToString()
    {
        return $"{this.Day:00} {this.Season} Y{this.Year}";
    }

    /// <summary>Get a translated string representation of the date in the current game locale.</summary>
    /// <param name="withYear">Whether to get a string which includes the year number.</param>
    public string ToLocaleString(bool withYear = true)
    {
        // get fallback translation from game
        string fallback = Utility.getDateStringFor(this.Day, this.SeasonIndex, this.Year);
        if (SDate.Translations == null)
            return fallback;

        // get short format
        string seasonName = Utility.getSeasonNameFromNumber(this.SeasonIndex);
        return SDate.Translations
            .Get(withYear ? "generic.date-with-year" : "generic.date", new
            {
                day = this.Day,
                year = this.Year,
                season = seasonName,
                seasonLowercase = seasonName?.ToLower()
            })
            .Default(fallback);
    }

    /****
    ** IEquatable
    ****/
    /// <summary>Get whether this instance is equal to another.</summary>
    /// <param name="other">The other value to compare.</param>
    public bool Equals(SDate? other)
    {
        return this == other;
    }

    /// <summary>Get whether this instance is equal to another.</summary>
    /// <param name="obj">The other value to compare.</param>
    public override bool Equals(object? obj)
    {
        return obj is SDate other && this == other;
    }

    /// <summary>Get a hash code which uniquely identifies a date.</summary>
    public override int GetHashCode()
    {
        return this.DaysSinceStart;
    }

    /****
    ** Operators
    ****/
    /// <summary>Get whether one date is equal to another.</summary>
    /// <param name="date">The base date to compare.</param>
    /// <param name="other">The other date to compare.</param>
    /// <returns>The equality of the dates</returns>
    public static bool operator ==(SDate? date, SDate? other)
    {
        return date?.DaysSinceStart == other?.DaysSinceStart;
    }

    /// <summary>Get whether one date is not equal to another.</summary>
    /// <param name="date">The base date to compare.</param>
    /// <param name="other">The other date to compare.</param>
    public static bool operator !=(SDate? date, SDate? other)
    {
        return date?.DaysSinceStart != other?.DaysSinceStart;
    }

    /// <summary>Get whether one date is more than another.</summary>
    /// <param name="date">The base date to compare.</param>
    /// <param name="other">The other date to compare.</param>
    public static bool operator >(SDate? date, SDate? other)
    {
        return date?.DaysSinceStart > other?.DaysSinceStart;
    }

    /// <summary>Get whether one date is more than or equal to another.</summary>
    /// <param name="date">The base date to compare.</param>
    /// <param name="other">The other date to compare.</param>
    public static bool operator >=(SDate? date, SDate? other)
    {
        return date?.DaysSinceStart >= other?.DaysSinceStart;
    }

    /// <summary>Get whether one date is less than or equal to another.</summary>
    /// <param name="date">The base date to compare.</param>
    /// <param name="other">The other date to compare.</param>
    public static bool operator <=(SDate? date, SDate? other)
    {
        return date?.DaysSinceStart <= other?.DaysSinceStart;
    }

    /// <summary>Get whether one date is less than another.</summary>
    /// <param name="date">The base date to compare.</param>
    /// <param name="other">The other date to compare.</param>
    public static bool operator <(SDate? date, SDate? other)
    {
        return date?.DaysSinceStart < other?.DaysSinceStart;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Construct an instance.</summary>
    /// <param name="day">The day of month.</param>
    /// <param name="season">The season name.</param>
    /// <param name="year">The year.</param>
    /// <param name="allowDayZero">Whether to allow 0 spring Y1 as a valid date.</param>
    /// <exception cref="ArgumentException">One of the arguments has an invalid value (like day 35).</exception>
    [SuppressMessage("ReSharper", "ConstantConditionalAccessQualifier", Justification = "The nullability is validated in this constructor.")]
    private SDate(int day, string season, int year, bool allowDayZero)
    {
        season = season?.Trim().ToLowerInvariant()!; // null-checked below

        // validate
        if (season == null)
            throw new ArgumentNullException(nameof(season));
        if (!this.Seasons.Contains(season))
            throw new ArgumentException($"Unknown season '{season}', must be one of [{string.Join(", ", this.Seasons)}].");
        if (day < 0 || day > this.DaysInSeason)
            throw new ArgumentException($"Invalid day '{day}', must be a value from 1 to {this.DaysInSeason}.");
        if (day == 0 && !(allowDayZero && this.IsDayZero(day, season, year)))
            throw new ArgumentException($"Invalid day '{day}', must be a value from 1 to {this.DaysInSeason}.");
        if (year < 1)
            throw new ArgumentException($"Invalid year '{year}', must be at least 1.");

        // initialize
        this.Day = day;
        this.Season = season;
        this.SeasonIndex = this.GetSeasonIndex(season);
        this.Year = year;
        this.DayOfWeek = this.GetDayOfWeek(day);
        this.DaysSinceStart = this.GetDaysSinceStart(day, season, year);
    }

    /// <summary>Get whether a date represents 0 spring Y1, which is the date during the in-game intro.</summary>
    /// <param name="day">The day of month.</param>
    /// <param name="season">The season name.</param>
    /// <param name="year">The year.</param>
    private bool IsDayZero(int day, string season, int year)
    {
        return day == 0 && season?.Trim().ToLower() == "spring" && year == 1;
    }

    /// <summary>Get the day of week for a given date.</summary>
    /// <param name="day">The day of month.</param>
    private DayOfWeek GetDayOfWeek(int day)
    {
        return (day % 7) switch
        {
            0 => DayOfWeek.Sunday,
            1 => DayOfWeek.Monday,
            2 => DayOfWeek.Tuesday,
            3 => DayOfWeek.Wednesday,
            4 => DayOfWeek.Thursday,
            5 => DayOfWeek.Friday,
            6 => DayOfWeek.Saturday,
            _ => 0
        };
    }

    /// <summary>Get the number of days since the game began (starting at 1 for the first day of spring in Y1).</summary>
    /// <param name="day">The day of month.</param>
    /// <param name="season">The season name.</param>
    /// <param name="year">The year.</param>
    private int GetDaysSinceStart(int day, string season, int year)
    {
        // return the number of days since 01 spring Y1 (inclusively)
        int yearIndex = year - 1;
        return
            yearIndex * this.DaysInSeason * this.SeasonsInYear
            + this.GetSeasonIndex(season) * this.DaysInSeason
            + day;
    }

    /// <summary>Get a season index.</summary>
    /// <param name="season">The season name.</param>
    /// <exception cref="InvalidOperationException">The current season wasn't recognized.</exception>
    private int GetSeasonIndex(string season)
    {
        int index = Array.IndexOf(this.Seasons, season);
        if (index == -1)
            throw new InvalidOperationException($"The season '{season}' wasn't recognized.");
        return index;
    }
}