using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DiscardReturnValueAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DiscardReturnValueAnalyzerCodeFixProvider)), Shared]
    public class DiscardReturnValueAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiscardReturnValueAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: c => AssignDiscardAsync(context.Document, declaration, c),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private async Task<Document> AssignDiscardAsync(Document document, InvocationExpressionSyntax declaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var parentExpressionStatement = (ExpressionStatementSyntax) declaration.Parent;

            // Fix trivia
            var firstToken = declaration.GetFirstToken();
            var leadingTrivia = declaration.GetLeadingTrivia();
            var trailingTrivia = parentExpressionStatement.GetTrailingTrivia();
            var declarationTrimmed = declaration.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(SyntaxTriviaList.Empty));

            // NOTE Roslyn is based on immutable structures
            // NOTE See: https://roslynquoter.azurewebsites.net/ to easily get the syntax tree for a piece of code.

            var discardOperatorIdentifierName = SyntaxFactory.IdentifierName("_");
            var discardOperatorIdentifierNameTrailed = discardOperatorIdentifierName.WithLeadingTrivia(leadingTrivia);
            var assignmentExpressionSyntax = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, discardOperatorIdentifierNameTrailed, declarationTrimmed);
            var expressionStatementSyntax = SyntaxFactory.ExpressionStatement(assignmentExpressionSyntax);
            var expressionStatementSyntaxToken = expressionStatementSyntax.GetLastToken();
            var expressionStatementSyntaxTrivia = expressionStatementSyntax.ReplaceToken(expressionStatementSyntaxToken, expressionStatementSyntaxToken.WithTrailingTrivia(trailingTrivia));

            var newNode = root.ReplaceNode(parentExpressionStatement, expressionStatementSyntaxTrivia);
            var newDocument = document.WithSyntaxRoot(newNode);

            return newDocument;
        }
    }
}
