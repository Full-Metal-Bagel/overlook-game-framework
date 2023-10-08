using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen.GlobalSuppressions;

internal static class Extension
{
    public static string GetFullName(this ClassDeclarationSyntax node)
    {
        var namespaceName = GetNamespaceName(node);
        var className = node.Identifier.Text;
        return string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}";
    }

    public static string GetFullName(this FieldDeclarationSyntax node)
    {
        var className = GetFullName((ClassDeclarationSyntax)node.Parent!);
        return $"{className}.{node.Declaration.Variables.First().Identifier.Text}";
    }

    public static string GetFullName(this MethodDeclarationSyntax node)
    {
        var className = GetFullName((ClassDeclarationSyntax)node.Parent!);
        return $"{className}.{node.Identifier.Text}";
    }

    public static string GetNamespaceName(ClassDeclarationSyntax node)
    {
        var namespaceNode = node.Ancestors()
            .OfType<NamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (namespaceNode == null)
        {
            return string.Empty;
        }

        return namespaceNode.Name.ToString();
    }
}
