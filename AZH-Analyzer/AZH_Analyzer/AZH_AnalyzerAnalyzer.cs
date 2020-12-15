using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace AZH_Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AZH_AnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "AZH_Analyzer";

        private const string DebugCategory = "API Usage";
        private static readonly LocalizableString DebugTitle = new LocalizableResourceString(nameof(Resources.AnalyzerDebugTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString DebugMessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerDebugMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString DebugDescription = new LocalizableResourceString(nameof(Resources.AnalyzerDebugDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly DiagnosticDescriptor DebugRule = new DiagnosticDescriptor(DiagnosticId, DebugTitle, DebugMessageFormat, DebugCategory, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: DebugDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(DebugRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(c =>
            {
                var hubClientCallType = c.Compilation.GetTypeByMetadataName("Microsoft.AspNetCore.SignalR.IClientProxy");
                if (hubClientCallType != null)
                {
                    context.RegisterCodeBlockStartAction<SyntaxKind>(
                    c1 => c1.RegisterSyntaxNodeAction(
                    c2 => AnalyzeClientCall(c2, hubClientCallType),
                    SyntaxKind.InvocationExpression));
                }
            });

        }

        private static void AnalyzeClientCall(SyntaxNodeAnalysisContext context, INamedTypeSymbol type) {
            var nodeString = context.Node.ToFullString();
            var node = (InvocationExpressionSyntax)context.Node;
            var firstArgument = node.ArgumentList.Arguments.FirstOrDefault();
            if (firstArgument != null)
            {
                if (firstArgument.Expression.Kind() == SyntaxKind.StringLiteralToken || firstArgument.Expression.Kind() == SyntaxKind.StringLiteralExpression) {
                    var diagnostic = Diagnostic.Create(
                        DebugRule,
                        firstArgument.Expression.GetLocation(),
                        "IClientProxy invocation found with a string literal for method. Please use an enumeration or constants class"
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
