using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using SMAPI.ModBuildConfig.Analyzer.Tests.Framework;
using StardewModdingAPI.ModBuildConfig.Analyzer;

namespace SMAPI.ModBuildConfig.Analyzer.Tests;

/// <summary>Unit tests for <see cref="ObsoleteFieldAnalyzer"/>.</summary>
[TestFixture]
public class ObsoleteFieldAnalyzerTests : DiagnosticVerifier
{
    /*********
    ** Fields
    *********/
    /// <summary>Sample C# mod code, with a {{test-code}} placeholder for the code in the Entry method to test.</summary>
    const string SampleProgram = @"
            using System;
            using StardewValley;
            using Netcode;
            using SObject = StardewValley.Object;

            namespace SampleMod
            {
                class ModEntry
                {
                    public void Entry()
                    {
                        {{test-code}}
                    }
                }
            }
        ";

    /// <summary>The line number where the unit tested code is injected into <see cref="SampleProgram"/>.</summary>
    private const int SampleCodeLine = 13;

    /// <summary>The column number where the unit tested code is injected into <see cref="SampleProgram"/>.</summary>
    private const int SampleCodeColumn = 25;


    /*********
    ** Unit tests
    *********/
    /// <summary>Test that no diagnostics are raised for an empty code block.</summary>
    [TestCase]
    public void EmptyCode_HasNoDiagnostics()
    {
        // arrange
        string test = @"";

        // assert
        this.VerifyCSharpDiagnostic(test);
    }

    /// <summary>Test that the expected diagnostic message is raised for an obsolete field reference.</summary>
    /// <param name="codeText">The code line to test.</param>
    /// <param name="column">The column within the code line where the diagnostic message should be reported.</param>
    /// <param name="oldName">The old field name which should be reported.</param>
    /// <param name="newName">The new field name which should be reported.</param>
    [TestCase("var x = new Farmer().friendships;", 8, "StardewValley.Farmer.friendships", "friendshipData")]
    [TestCase("var x = new Farmer()?.friendships;", 8, "StardewValley.Farmer.friendships", "friendshipData")]
    public void AvoidObsoleteField_RaisesDiagnostic(string codeText, int column, string oldName, string newName)
    {
        // arrange
        string code = ObsoleteFieldAnalyzerTests.SampleProgram.Replace("{{test-code}}", codeText);
        DiagnosticResult expected = new()
        {
            Id = "AvoidObsoleteField",
            Message = $"The '{oldName}' field is obsolete and should be replaced with '{newName}'. See https://smapi.io/package/avoid-obsolete-field for details.",
            Severity = DiagnosticSeverity.Warning,
            Locations = new[] { new DiagnosticResultLocation("Test0.cs", ObsoleteFieldAnalyzerTests.SampleCodeLine, ObsoleteFieldAnalyzerTests.SampleCodeColumn + column) }
        };

        // assert
        this.VerifyCSharpDiagnostic(code, expected);
    }


    /*********
    ** Helpers
    *********/
    /// <summary>Get the analyzer being tested.</summary>
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ObsoleteFieldAnalyzer();
    }
}