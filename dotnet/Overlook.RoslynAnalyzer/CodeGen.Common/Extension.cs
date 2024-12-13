using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen;

public static class Extension
{
    public static string GetFullName(this TypeDeclarationSyntax node)
    {
        var namespaceName = GetNamespaceName(node);
        var className = node.Identifier.Text;
        return string.IsNullOrEmpty(namespaceName) ? className : $"{namespaceName}.{className}";
    }

    public static string GetFullName(this FieldDeclarationSyntax node)
    {
        var className = GetFullName((TypeDeclarationSyntax)node.Parent!);
        return $"{className}.{node.GetName()}";
    }

    public static string GetFullName(this MethodDeclarationSyntax node)
    {
        var className = GetFullName((TypeDeclarationSyntax)node.Parent!);
        return $"{className}.{node.GetName()}";
    }

    public static string GetDeclaringTypeFullName(this MemberDeclarationSyntax node)
    {
        return GetFullName((TypeDeclarationSyntax)node.Parent!);
    }

    public static string GetDeclaringTypeName(this MemberDeclarationSyntax node)
    {
        return ((TypeDeclarationSyntax)node.Parent!).Identifier.Text;
    }

    public static string GetName(this FieldDeclarationSyntax node)
    {
        return node.Declaration.Variables.First().Identifier.Text;
    }

    public static string GetName(this MethodDeclarationSyntax node)
    {
        return node.Identifier.Text;
    }

    public static string GetName(this PropertyDeclarationSyntax node)
    {
        return node.Identifier.Text;
    }

    public static string GetNamespaceName(this TypeDeclarationSyntax node)
    {
        var namespaceNode = node.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .FirstOrDefault();

        if (namespaceNode == null)
        {
            return string.Empty;
        }

        return namespaceNode.Name.ToString();
    }

    public static string GetFullName(this TypeSyntax type, SemanticModel semanticModel)
    {
        var typeSymbol = ModelExtensions.GetTypeInfo(semanticModel, type).Type;
        return typeSymbol?.ToDisplayString() ?? type.ToString();
    }

    public static string GetTypeFullName(this FieldDeclarationSyntax node, SemanticModel semanticModel)
    {
        return node.Declaration.Type.GetFullName(semanticModel);
    }

    public static string GetTypeFullName(this PropertyDeclarationSyntax node, SemanticModel semanticModel)
    {
        return node.Type.GetFullName(semanticModel);
    }

    public static bool IsPublic(this MemberDeclarationSyntax node)
    {
        return node.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
    }

    public static bool IsReadOnly(this FieldDeclarationSyntax node)
    {
        return node.Modifiers.Any(m => m.IsKind(SyntaxKind.ReadOnlyKeyword));
    }

    public static bool HasGetMethod(this PropertyDeclarationSyntax node)
    {
        return node.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? false;
    }

    public static bool HasSetMethod(this PropertyDeclarationSyntax node)
    {
        return node.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false;
    }

    private static bool HasPublicAccessor(this PropertyDeclarationSyntax node, SyntaxKind accessorKind)
    {
        if (!node.IsPublic()) return false;

        var accessor = node.AccessorList?.Accessors
            .FirstOrDefault(a => a.IsKind(accessorKind));

        if (accessor == null) return false;

        // If the accessor has no modifiers, it inherits the property's accessibility
        // If it has modifiers, check if it's explicitly public
        return !accessor.Modifiers.Any() ||
               accessor.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
    }

    public static bool HasPublicGetMethod(this PropertyDeclarationSyntax node)
    {
        return HasPublicAccessor(node, SyntaxKind.GetAccessorDeclaration);
    }

    public static bool HasPublicSetMethod(this PropertyDeclarationSyntax node)
    {
        return HasPublicAccessor(node, SyntaxKind.SetAccessorDeclaration);
    }

    public static string GetGuid(this MemberDeclarationSyntax node)
    {
        var guidAttribute = node.AttributeLists
            .SelectMany(a => a.Attributes)
            .SingleOrDefault(attribute => attribute.Name.ToString() is "TypeGuid" or "PropertyGuid" or "FieldGuid" or "MethodGuid")
        ;
        return guidAttribute == null ? "" : guidAttribute.ArgumentList!.Arguments[0].ToString();
    }
}
