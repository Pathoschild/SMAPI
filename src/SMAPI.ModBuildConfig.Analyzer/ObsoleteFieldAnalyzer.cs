using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StardewModdingAPI.ModBuildConfig.Analyzer;

/// <summary>Detects references to a field which has been replaced.</summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ObsoleteFieldAnalyzer : DiagnosticAnalyzer
{
    /*********
    ** Fields
    *********/
    /// <summary>Maps obsolete fields/properties to their non-obsolete equivalent.</summary>
    private readonly IDictionary<string, string> ReplacedFields = new Dictionary<string, string>
    {
        // Farmer
        ["StardewValley.Farmer::friendships"] = "friendshipData"
    };

    /// <summary>Describes the diagnostic rule covered by the analyzer.</summary>
    private readonly IDictionary<string, DiagnosticDescriptor> Rules = new Dictionary<string, DiagnosticDescriptor>
    {
        ["AvoidObsoleteField"] = new(
            id: "AvoidObsoleteField",
            title: "Reference to obsolete field",
            messageFormat: "The '{0}' field is obsolete and should be replaced with '{1}'. See https://smapi.io/package/avoid-obsolete-field for details.",
            category: "SMAPI.CommonErrors",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: "https://smapi.io/package/avoid-obsolete-field"
        )
    };


    /*********
    ** Accessors
    *********/
    /// <summary>The descriptors for the diagnostics that this analyzer is capable of producing.</summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }


    /*********
    ** Public methods
    *********/
    /// <summary>Construct an instance.</summary>
    public ObsoleteFieldAnalyzer()
    {
        this.SupportedDiagnostics = ImmutableArray.CreateRange(this.Rules.Values);
    }

    /// <summary>Called once at session start to register actions in the analysis context.</summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(
            this.AnalyzeObsoleteFields,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.ConditionalAccessExpression
        );
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Analyze a syntax node and add a diagnostic message if it references an obsolete field.</summary>
    /// <param name="context">The analysis context.</param>
    private void AnalyzeObsoleteFields(SyntaxNodeAnalysisContext context)
    {
        try
        {
            // get reference info
            if (!AnalyzerUtilities.TryGetMemberInfo(context.Node, context.SemanticModel, out ITypeSymbol declaringType, out _, out string memberName))
                return;

            // suggest replacement
            foreach (ITypeSymbol type in AnalyzerUtilities.GetConcreteTypes(declaringType))
            {
                if (this.ReplacedFields.TryGetValue($"{type}::{memberName}", out string replacement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(this.Rules["AvoidObsoleteField"], context.Node.GetLocation(), $"{type}.{memberName}", replacement));
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed processing expression: '{context.Node}'. Exception details: {ex.ToString().Replace('\r', ' ').Replace('\n', ' ')}");
        }
    }
}
