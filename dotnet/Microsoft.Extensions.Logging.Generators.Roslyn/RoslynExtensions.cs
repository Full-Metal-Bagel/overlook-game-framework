// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Logging.Generators
{
    internal static class RoslynExtensions
    {
        /// <summary>
        /// Gets the best type by metadata name, handling cases where multiple types might exist.
        /// </summary>
        public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName)
        {
            INamedTypeSymbol? type = null;

            foreach (var currentType in compilation.GetTypesByMetadataName(fullyQualifiedMetadataName))
            {
                if (ReferenceEquals(currentType.ContainingAssembly, compilation.Assembly))
                {
                    return currentType;
                }

                switch (currentType.GetResultantVisibility())
                {
                    case SymbolVisibility.Public:
                    case SymbolVisibility.Internal when currentType.ContainingAssembly.GivesAccessTo(compilation.Assembly):
                        break;
                    default:
                        continue;
                }

                if (type is object)
                {
                    // Multiple visible types with the same metadata name exist
                    return null;
                }

                type = currentType;
            }

            return type;
        }

        private enum SymbolVisibility
        {
            Public,
            Internal,
            Private,
        }

        private static SymbolVisibility GetResultantVisibility(this ISymbol symbol)
        {
            SymbolVisibility visibility = SymbolVisibility.Public;

            for (ISymbol? s = symbol; s != null && s.Kind != SymbolKind.Namespace; s = s.ContainingSymbol)
            {
                switch (s.DeclaredAccessibility)
                {
                    case Accessibility.NotApplicable:
                    case Accessibility.Public:
                        break;
                    case Accessibility.Internal:
                    case Accessibility.ProtectedOrInternal:
                        if (visibility == SymbolVisibility.Public)
                        {
                            visibility = SymbolVisibility.Internal;
                        }
                        break;
                    default:
                        return SymbolVisibility.Private;
                }
            }

            return visibility;
        }

        private static ImmutableArray<INamedTypeSymbol> GetTypesByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName)
        {
            var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

            var type = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
            if (type != null)
            {
                builder.Add(type);
            }

            foreach (var reference in compilation.References)
            {
                var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
                if (assemblySymbol != null)
                {
                    type = assemblySymbol.GetTypeByMetadataName(fullyQualifiedMetadataName);
                    if (type != null)
                    {
                        builder.Add(type);
                    }
                }
            }

            return builder.ToImmutable();
        }
    }
}
