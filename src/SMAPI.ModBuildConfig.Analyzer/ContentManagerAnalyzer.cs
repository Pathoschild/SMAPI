using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using System.Linq;

namespace StardewModdingAPI.ModBuildConfig.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ContentManagerAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }


        /// <summary>The diagnostic info for an avoidable runtime casting error.</summary>
        private readonly DiagnosticDescriptor AvoidBadTypeRule = new(
            id: "AvoidContentManagerBadType",
            title: "Avoid incorrectly typing ContentManager Loads",
            messageFormat: "'{0}' uses the {1} type, but {2} is in use instead. See https://smapi.io/package/avoid-contentmanager-type for details.",
            category: "SMAPI.CommonErrors",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: "https://smapi.io/package/avoid-contentmanager-type"
        );
        /// <summary>The diagnostic info for best practices using DataLoader</summary>
        private readonly DiagnosticDescriptor PreferDataLoader = new(
            id: "PreferContentManagerDataLoader",
            title: "Prefer using DataLoader to ContentManager Loads",
            messageFormat: "'{0}' can be accessed using 'DataLoader.{1}(LocalizedContentManager content)' instead. See https://smapi.io/package/prefer-contentmanager-dataloader for details.",
            category: "SMAPI.CommonErrors",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: "https://smapi.io/package/prefer-contentmanager-dataloader"
        );

        public ContentManagerAnalyzer()
        {
            this.SupportedDiagnostics = ImmutableArray.CreateRange(new[] { this.AvoidBadTypeRule, this.PreferDataLoader });
        }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(
                this.AnalyzeContentManagerLoads,
                SyntaxKind.InvocationExpression
            );
        }

        private void AnalyzeContentManagerLoads(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null || memberAccess.Name.Identifier.ValueText != "Load")
                return;
            string? loadNamespace = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol?.ContainingNamespace.Name;
            if (!(loadNamespace == "StardewValley" || loadNamespace == "StardewModdingAPI"))
                return;
            // "Data\\Fish" -> Data\Fish
            string assetName = invocation.ArgumentList.Arguments[0].ToString().Replace("\"", "").Replace("\\\\", "\\");

            if (!assetName.StartsWith("Data", StringComparison.InvariantCultureIgnoreCase)) return;
            string dataAsset = assetName.Substring(5);

            var dataLoader = context.Compilation.GetTypeByMetadataName("StardewValley.DataLoader");
            var dataMatch = dataLoader.GetMembers().FirstOrDefault(m => m.Name == dataAsset);
            if (dataMatch == null) return;
            if (dataMatch is IMethodSymbol method)
            {
                var genericArgument = context.SemanticModel.GetTypeInfo((memberAccess.Name as GenericNameSyntax).TypeArgumentList.Arguments[0]).Type;
                // Can't use the proper way of using SymbolEquityComparer due to System.Collections overlapping with CoreLib.
                if (method.ReturnType.ToString() != genericArgument.ToString())
                {
                    context.ReportDiagnostic(Diagnostic.Create(this.AvoidBadTypeRule, context.Node.GetLocation(), assetName, method.ReturnType.ToString(), genericArgument.ToString()));
                }
                context.ReportDiagnostic(Diagnostic.Create(this.PreferDataLoader, context.Node.GetLocation(), assetName, dataAsset));
            }
        }
    }
}
