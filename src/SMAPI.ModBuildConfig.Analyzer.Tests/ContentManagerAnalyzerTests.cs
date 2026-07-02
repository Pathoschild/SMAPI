using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using SMAPI.ModBuildConfig.Analyzer.Tests.Framework;
using StardewModdingAPI.ModBuildConfig.Analyzer;

namespace SMAPI.ModBuildConfig.Analyzer.Tests
{
    /// <summary>Unit tests for <see cref="ContentManagerAnalyzer"/>.</summary>
    [TestFixture]
    public class ContentManagerAnalyzerTests : DiagnosticVerifier
    {
        /*********
        ** Fields
        *********/
        /// <summary>Sample C# mod code, with a {{test-code}} placeholder for the code in the Entry method to test.</summary>
        const string SampleProgram = @"
            using System;
            using System.Collections.Generic;
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

        const string SampleUnrelatedGoodProgram = @"
            using System;
            using System.Collections.Generic;

            namespace Sample;
            class Loader
            {
                public T Load<T>(string arg)
                {
                    return default(T);
                }
            }
            class ModEntry
            {
                public void Entry()
                {
                    var loader = new Loader();
                    var test = loader.Load<Dictionary<int,string>>(""Data\Fish"");
                }
            }
        ";

        /// <summary>The line number where the unit tested code is injected into <see cref="SampleProgram"/>.</summary>
        private const int SampleCodeLine = 14;

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

        /// <summary>Test that the expected diagnostic message is raised for avoidable net field references.</summary>
        /// <param name="codeText">The code line to test.</param>
        /// <param name="column">The column within the code line where the diagnostic message should be reported.</param>
        /// <param name="expression">The expression which should be reported.</param>
        /// <param name="netType">The net type name which should be reported.</param>
        /// <param name="suggestedProperty">The suggested property name which should be reported.</param>
        [TestCase("Game1.content.Load<Dictionary<int, string>>(\"Data\\\\Fish\");", 0, "Data\\Fish", "System.Collections.Generic.Dictionary<int, string>", "System.Collections.Generic.Dictionary<string, string>")]
        public void BadType_RaisesDiagnostic(string codeText, int column, string assetName, string expectedType, string suggestedType)
        {
            // arrange
            string code = SampleProgram.Replace("{{test-code}}", codeText);
            DiagnosticResult expected = new()
            {
                Id = "AvoidContentManagerBadType",
                Message = $"'{assetName}' uses the {suggestedType} type, but {expectedType} is in use instead. See https://smapi.io/package/avoid-contentmanager-type for details.",
                Severity = DiagnosticSeverity.Error,
                Locations = [new DiagnosticResultLocation("Test0.cs", SampleCodeLine, SampleCodeColumn + column)]
            };
            DiagnosticResult preferDataLoader = new()
            {
                Id = "PreferContentManagerDataLoader",
                Message = $"'{assetName}' can be accessed using 'DataLoader.{assetName[5..]}(LocalizedContentManager content)' instead. See https://smapi.io/package/prefer-contentmanager-dataloader for details.",
                Severity = DiagnosticSeverity.Info,
                Locations = [new DiagnosticResultLocation("Test0.cs", SampleCodeLine, SampleCodeColumn + column)]
            };

            // assert
            this.VerifyCSharpDiagnostic(code, expected, preferDataLoader);
        }



        /// <summary>Test that the expected diagnostic message is raised for avoidable net field references.</summary>
        /// <param name="codeText">The code line to test.</param>
        /// <param name="column">The column within the code line where the diagnostic message should be reported.</param>
        /// <param name="expression">The expression which should be reported.</param>
        /// <param name="netType">The net type name which should be reported.</param>
        /// <param name="suggestedProperty">The suggested property name which should be reported.</param>
        [TestCase("Game1.content.Load<Dictionary<string, string>>(\"Data\\\\Fish\");", 0, "Data\\Fish")]
        public void PreferDataLoader_RaisesDiagnostic(string codeText, int column, string assetName)
        {
            // arrange
            string code = SampleProgram.Replace("{{test-code}}", codeText);
            DiagnosticResult preferDataLoader = new()
            {
                Id = "PreferContentManagerDataLoader",
                Message = $"'{assetName}' can be accessed using 'DataLoader.{assetName[5..]}(LocalizedContentManager content)' instead. See https://smapi.io/package/prefer-contentmanager-dataloader for details.",
                Severity = DiagnosticSeverity.Info,
                Locations = [new DiagnosticResultLocation("Test0.cs", SampleCodeLine, SampleCodeColumn + column)]
            };

            // assert
            this.VerifyCSharpDiagnostic(code, preferDataLoader);
        }

        [TestCase("Game1.content.Load<Dictionary<string, string>>(\"Data\\\\Custom_Asset\");", true)]
        [TestCase(SampleUnrelatedGoodProgram, false)]

        public void ValidCode_HasNoDiagnostics(string codeText, bool useWrapper)
        {
            string code = useWrapper ? SampleProgram.Replace("{{test-code}}", codeText) : codeText;
            this.VerifyCSharpDiagnostic(code);
        }


        /*********
        ** Helpers
        *********/
        /// <summary>Get the analyzer being tested.</summary>
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ContentManagerAnalyzer();
        }
    }
}
