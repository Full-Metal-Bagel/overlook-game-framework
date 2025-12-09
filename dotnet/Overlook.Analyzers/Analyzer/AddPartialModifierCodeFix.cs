#nullable enable

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Overlook.Analyzers;

/// <summary>
/// Code fix provider that adds the 'partial' modifier to types decorated with [QueryComponent].
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddPartialModifierCodeFix)), Shared]
public class AddPartialModifierCodeFix : CodeFixProvider
{
    private const string Title = "Add 'partial' modifier";

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create("OVL005");

    public sealed override FixAllProvider GetFixAllProvider() =>
        WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration at the diagnostic location
        var typeDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();

        if (typeDeclaration == null)
            return;

        // Register a code action that will invoke the fix
        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => AddPartialModifierAsync(context.Document, typeDeclaration, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> AddPartialModifierAsync(
        Document document,
        TypeDeclarationSyntax typeDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Create the partial keyword token with appropriate trivia
        var partialKeyword = SyntaxFactory.Token(SyntaxKind.PartialKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        // Find the position to insert the partial modifier
        // It should come after access modifiers (public, private, etc.) and before the type keyword
        var newModifiers = InsertPartialModifier(typeDeclaration.Modifiers, partialKeyword);

        // Create new type declaration with the partial modifier
        var newTypeDeclaration = typeDeclaration.WithModifiers(newModifiers);

        // Replace the old type declaration with the new one
        var newRoot = root.ReplaceNode(typeDeclaration, newTypeDeclaration);

        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxTokenList InsertPartialModifier(SyntaxTokenList modifiers, SyntaxToken partialKeyword)
    {
        // If there are no modifiers, just return the partial keyword
        if (modifiers.Count == 0)
        {
            return SyntaxFactory.TokenList(partialKeyword);
        }

        // Find the right position: after access modifiers, static, etc., but before the type keyword
        // Order should be: [access] [static] [readonly] [partial] [class/struct/record]
        var insertIndex = modifiers.Count;

        for (int i = 0; i < modifiers.Count; i++)
        {
            var kind = modifiers[i].Kind();
            // Insert before any modifiers that should come after partial
            // (In practice, partial usually comes last among modifiers, right before the type keyword)
            if (kind == SyntaxKind.AbstractKeyword ||
                kind == SyntaxKind.SealedKeyword ||
                kind == SyntaxKind.NewKeyword)
            {
                insertIndex = i;
                break;
            }
        }

        // Copy leading trivia from the next token (if inserting at end) or preserve existing
        if (insertIndex == modifiers.Count)
        {
            // Add at the end - partial keyword already has trailing space
            return modifiers.Add(partialKeyword);
        }
        else
        {
            // Insert at the found position
            return modifiers.Insert(insertIndex, partialKeyword);
        }
    }
}
