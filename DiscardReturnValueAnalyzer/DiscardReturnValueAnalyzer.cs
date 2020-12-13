using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DiscardReturnValueAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DiscardReturnValueAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DiscardReturnValueAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInvocationExpression, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax) context.Node;

            // Get the method declaration so we can find out if it has a return value
            var methodSymbol = context
                .SemanticModel
                .GetSymbolInfo(invocation, context.CancellationToken)
                .Symbol as IMethodSymbol;


            if(methodSymbol == null)
                return; // TODO This can be null as well, e.g. when you were calling a method from another assembly, so test for that.

            if(methodSymbol.ReturnsVoid)
                return;
            
            var parent = invocation.Parent;

            if(parent == null)
                return; // TODO apparently this can be null, but under what circumstances?

            if (parent is ExpressionStatementSyntax)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), invocation));
            }
            
        }

    }
}
