using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Octopus.RoslynAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BuildersReassigningToMemberVariablesAnalyzer : DiagnosticAnalyzer
    {
        const string DiagnosticId = "Octopus_PreventBuildersReassigningToMemberVariablesFromBuild";

        const string Title = "Builders should not assign state during the Build method";

        const string MessageFormat = "Builders should not re-assign back to their state when using the Build method. A builder should be able to be called multiple times, each successfully creating a new instance from the supplied properties. Consider creating a local variable and assigning the member value to it to use during the Build method.";
        const string Category = "Octopus";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            true
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
        
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(CheckAssignmentsInMethod, SyntaxKind.MethodDeclaration);
        }

        static void CheckAssignmentsInMethod(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MethodDeclarationSyntax { Body: { }, Identifier: { ValueText: "Build" } } method))
                return;

            if (!(method.Parent is ClassDeclarationSyntax classDeclarationSyntax) || !classDeclarationSyntax.Identifier.ValueText.EndsWith("Builder"))
                return;

            var localVariables = method.Body.Statements
                .Where(x => x is LocalDeclarationStatementSyntax)
                .Cast<LocalDeclarationStatementSyntax>()
                .Select(x => x.Declaration)
                .SelectMany(x => x.ChildNodes())
                .Where(syntaxNode => syntaxNode is VariableDeclaratorSyntax)
                .Cast<VariableDeclaratorSyntax>()
                .Select(x => x.Identifier.ValueText)
                .ToImmutableHashSet();
                
            var assignmentStatements = method.Body.Statements
                .Where(x => x is ExpressionStatementSyntax)
                .Select(expressionStatementSyntax => ((ExpressionStatementSyntax)expressionStatementSyntax).Expression)
                .Where(expression => expression is AssignmentExpressionSyntax)
                .Cast<AssignmentExpressionSyntax>();
            
            foreach (var assignmentStatement in assignmentStatements)
            {
                switch (assignmentStatement.Left)
                {
                    //Local variable property modification, let's allow modification to it as it doesn't modify the state of the builder
                    //eg. someLocalVariable = 5; 
                    case IdentifierNameSyntax left when localVariables.Contains(left.Identifier.ValueText):
                    //Local variable, let's allow re-assignment to it as it doesn't modify the state of the builder
                    //eg. cracken.a.x.x.x.x.gold = 1; 
                    case MemberAccessExpressionSyntax memberAccessExpressionSyntax when localVariables.Contains(FindIdentifierName(memberAccessExpressionSyntax)):
                    // Parenthesized Variable Design eg. var (a, b) = (1, 2)
                    case DeclarationExpressionSyntax { Designation: ParenthesizedVariableDesignationSyntax _ }:
                        continue;
                    default:
                    {
                        var diagnostic = Diagnostic.Create(Rule, assignmentStatement.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                        break;
                    }
                }
            }
        }

        static string FindIdentifierName(MemberAccessExpressionSyntax memberAccessExpressionSyntax, int count = 0)
        {
            if (count >= 100) return string.Empty;
            
            return memberAccessExpressionSyntax.Expression switch
            {
                MemberAccessExpressionSyntax childMemberAccessExpressionSyntax => FindIdentifierName(childMemberAccessExpressionSyntax, ++count),
                IdentifierNameSyntax identifierNameSyntax => identifierNameSyntax.Identifier.ValueText,
                _ => string.Empty
            };
        }
    }
}