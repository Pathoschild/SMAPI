using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ConsoleWrapperLib;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Internal.ConsoleWriting
{
    /// <summary>Writes color-coded text to a ConsoleWrapper object.</summary>
    internal class ConsoleWrapperConsoleWriter : ColorfulConsoleWriter
    {
        /// <summary>The console wrapper object to use, if avaiable.</summary>
        public ConsoleWrapper? ConsoleWrapper { get; set; }

        /// <summary>Construct an instance.</summary>
        /// <param name="platform">The target platform.</param>
        /// <param name="colorConfig">The colors to use for text written to the SMAPI console.</param>
        public ConsoleWrapperConsoleWriter(Platform platform, ColorSchemeConfig colorConfig)
            : base(platform, colorConfig)
        {
        }

        /// <inheritdoc/>
        protected override void WriteLineImpl(string message, ConsoleColor? foregroundColor, ConsoleColor? backgroundColor)
        {
            if (this.ConsoleWrapper != null)
                this.ConsoleWrapper.WriteLine(message, foregroundColor ?? this.ConsoleWrapper.DefaultForeground, backgroundColor ?? this.ConsoleWrapper.DefaultBackground);
            else
                base.WriteLineImpl(message, foregroundColor, backgroundColor);
        }
    }
}
