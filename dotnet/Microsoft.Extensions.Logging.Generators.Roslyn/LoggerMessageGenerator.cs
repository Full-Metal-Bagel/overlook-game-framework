// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Extensions.Logging.Generators
{
    [Generator]
    public partial class LoggerMessageGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    Parser.LoggerMessageAttribute,
                    (node, _) => node is MethodDeclarationSyntax,
                    (context, _) => context.TargetNode.Parent as ClassDeclarationSyntax)
                .Where(static m => m is not null)!;

            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
                context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
            {
                // nothing to do yet
                return;
            }

            ImmutableHashSet<ClassDeclarationSyntax> distinctClasses = classes.ToImmutableHashSet();

            var p = new Parser(compilation, context.ReportDiagnostic, context.CancellationToken);
            IReadOnlyList<LoggerClass> logClasses = p.GetLogClasses(distinctClasses);
            if (logClasses.Count > 0)
            {
                var e = new Emitter(compilation);
                string result = e.Emit(logClasses, context.CancellationToken);

                context.AddSource("LoggerMessage.g.cs", SourceText.From(result, Encoding.UTF8));
            }
        }
    }
}
