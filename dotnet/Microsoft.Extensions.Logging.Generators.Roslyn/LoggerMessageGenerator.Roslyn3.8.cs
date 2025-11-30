// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[assembly: System.Resources.NeutralResourcesLanguage("en-us")]

namespace Microsoft.Extensions.Logging.Generators
{
    [Generator]
    public partial class LoggerMessageGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(SyntaxContextReceiver.Create);
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxContextReceiver receiver || receiver.Nodes.Count == 0)
            {
                // nothing to do yet
                return;
            }

            var classDeclarations = receiver.FindClassDeclarations(context);
            if (!classDeclarations.Any()) return;

            var p = new Parser(context.Compilation, context.ReportDiagnostic, context.CancellationToken);
            IReadOnlyList<LoggerClass> logClasses = p.GetLogClasses(classDeclarations);
            if (logClasses.Count > 0)
            {
                var e = new Emitter();
                string result = e.Emit(logClasses, context.CancellationToken);

                context.AddSource("LoggerMessage.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }

        private sealed class SyntaxContextReceiver : ISyntaxReceiver
        {
            internal static ISyntaxReceiver Create()
            {
                return new SyntaxContextReceiver();
            }

            public HashSet<SyntaxNode> Nodes { get; } = new();
            private Dictionary<SyntaxTree, SemanticModel> _models = new();

            public void OnVisitSyntaxNode(SyntaxNode node)
            {
                if (IsSyntaxTargetForGeneration(node)) Nodes.Add(node);
            }

            public IReadOnlyCollection<ClassDeclarationSyntax> FindClassDeclarations(GeneratorExecutionContext context)
            {
                var classes = new HashSet<ClassDeclarationSyntax>();
                foreach (var node in Nodes)
                {
                    if (!_models.TryGetValue(node.SyntaxTree, out var semanticModel))
                    {
                        semanticModel = context.Compilation.GetSemanticModel(node.SyntaxTree);
                        _models[node.SyntaxTree] = semanticModel;
                    }
                    if (semanticModel == null) continue;
                    var @class = GetSemanticTargetForGeneration(node, semanticModel);
                    if (@class != null && !classes.Contains(@class)) classes.Add(@class);
                }
                return classes;
            }

            private static bool IsSyntaxTargetForGeneration(SyntaxNode node) =>
                node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0;

            private ClassDeclarationSyntax? GetSemanticTargetForGeneration(SyntaxNode node, SemanticModel semanticModel)
            {
                var methodDeclarationSyntax = (MethodDeclarationSyntax)node;
                foreach (AttributeListSyntax attributeListSyntax in methodDeclarationSyntax.AttributeLists)
                {
                    foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
                    {
                        var attributeSymbol = semanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;
                        if (attributeSymbol == null)
                        {
                            continue;
                        }

                        var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                        var fullName = attributeContainingTypeSymbol?.ToDisplayString();

                        if (fullName == Parser.LoggerMessageAttribute)
                        {
                            return methodDeclarationSyntax.Parent as ClassDeclarationSyntax;
                        }
                    }
                }

                return null;
            }
        }
    }
}
