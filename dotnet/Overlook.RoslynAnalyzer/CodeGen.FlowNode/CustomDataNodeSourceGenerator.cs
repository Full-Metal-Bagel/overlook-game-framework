using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen.FlowNode;

// https://chat.openai.com/share/c40372f6-9a57-4a8f-8ef7-6e4e534c8b2f
[Generator]
public class CustomDataNodeSourceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new SyntaxContextReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // Ensure the receiver is of the expected type
        if (context.SyntaxReceiver is not SyntaxContextReceiver receiver) return;
        if (receiver.DecoratedTypes.Count == 0)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;"); // Add necessary usings at the top
        builder.AppendLine();
        builder.AppendLine("namespace Game");
        builder.AppendLine("{");
        foreach (var (typeDeclaration, attribute) in receiver.DecoratedTypes)
        {
            var semanticModel = context.Compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
            var namedTypeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
            if (namedTypeSymbol == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    id: "FC0001",
                    title: "invalid type",
                    messageFormat: $"invalid type {typeDeclaration.Identifier.Text}",
                    category: "NODE",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true
                ), location: typeDeclaration.GetLocation(), messageArgs: null));
                continue;
            }

            var typeName = namedTypeSymbol.ToDisplayString();
            var nodeName = FindArgumentValue(attribute, "Name") ?? typeDeclaration.Identifier.Text;
            var typeId = namedTypeSymbol.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "TypeGuidAttribute")
                ?.ConstructorArguments.FirstOrDefault().Value?.ToString()?.Replace("-", "")
                ?? nodeName;
            var category = FindArgumentValue(attribute, "Category");
            if (string.IsNullOrEmpty(category))
            {
                var index = typeName.LastIndexOf('.');
                if (index >= 0) category = typeName.Substring(0, index).Replace('.', '/');
            }
            GenGetDataNode(symbol: namedTypeSymbol, nodeName: nodeName, category: category!, typeId: typeId, typeName: typeName);
            GenSetDataNode(symbol: namedTypeSymbol, nodeName: nodeName, category: category!, typeId: typeId, typeName: typeName);
            var isEvent = bool.TryParse(FindArgumentValue(attribute, "Event"), out bool e) && e;
            if (isEvent)
            {
                GenEventReceiver(namedTypeSymbol: namedTypeSymbol, nodeName: nodeName, category: category!, typeId: typeId, typeName: typeName);
                GenEventSender(nodeName: nodeName, category: category!, typeId: typeId, typeName: typeName);
            }
        }

        builder.AppendLine("}");
        context.AddSource("GameDataNodes.g.cs", builder.ToString());
        return;

        string? FindArgumentValue(AttributeSyntax attribute, string name)
        {
            return attribute.ArgumentList?.Arguments.FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.ValueText == name)?.Expression?.ToString()?.Trim('"');
        }

        IEnumerable<(string Type, string name, string guid, bool isReadOnly, bool isWriteOnly)> GetMembers(INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var member in namedTypeSymbol.GetMembers())
            {
                // Filter based on some criteria, e.g., public properties
                if (member.DeclaredAccessibility != Accessibility.Public || member.IsStatic) continue;

                var name = member.Name;
                if (string.IsNullOrEmpty(name)) continue;

                var (type, isReadOnly, isWriteOnly) = member switch
                {
                    IPropertySymbol property => (property.Type.ToDisplayString(), property.IsReadOnly, property.IsWriteOnly),
                    IFieldSymbol field => (field.Type.ToDisplayString(), field.IsReadOnly, false),
                    _ => ("", true, true)
                };
                if (string.IsNullOrEmpty(type)) continue;

                var guid = member.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.Name is "PropertyGuidAttribute" or "FieldGuidAttribute")
                    ?.ConstructorArguments.FirstOrDefault().Value?.ToString()
                    ?? ""
                ;

                yield return (type, name, guid, isReadOnly, isWriteOnly);
            }
        }

        void GenEventReceiver(INamedTypeSymbol namedTypeSymbol, string nodeName, string category, string typeId, string typeName)
        {
            builder.AppendLine($$"""
                                 [ParadoxNotion.Design.Category("{{category}}")]
                                 [ParadoxNotion.Design.Icon("Icons/NotifyIcon")]
                                 [ParadoxNotion.Design.Color("ff5c5c")]
                                 [ParadoxNotion.Design.Name("{{nodeName}}(Receive)")]
                                 public class CustomEventNode_{{typeId}} : FlowCanvas.FlowNode, ICustomEventNode<{{typeName}}>
                                 {
                                     private FlowCanvas.FlowOutput _on = default!;
                                     private {{typeName}} _event = default!;
                                     public Type EventType => typeof({{typeName}});
                                     private Game.GameData GameData => ((Game.EntityBlackboard)graphAgent).GameData;

                                     [field: UnityEngine.SerializeField, ParadoxNotion.Design.ExposeField]
                                     public bool SelfUpdate { get; private set; } = true;

                                     protected override void RegisterPorts()
                                     {
                                         _on = AddFlowOutput("On");
                                         AddValueOutput("Event", () => _event);
                                 """);
            // Dynamically generate AddValueOutput calls for each property
            foreach (var (type, name, guid, isReadOnly, isWriteOnly) in GetMembers(namedTypeSymbol))
            {
                if (isWriteOnly) continue;
                builder!.AppendLine($"        AddValueOutput<{type!}>(name: \"{name!}\", ID: \"{guid!}\", () => _event.{name!});");
            }

            builder.AppendLine($$"""
                                     }

                                     public void Update()
                                     {
                                         if (SelfUpdate) HandleSystemEvents(GameData);
                                     }

                                     public void ManualTick(Game.GameData? data)
                                     {
                                         if (!SelfUpdate) HandleSystemEvents(data ?? GameData);
                                         else Game.Debug.LogError("do not tick a self updated event node manually {{nodeName}}", graphAgent);
                                     }

                                     public void ManualInvoke({{typeName}} value)
                                     {
                                         _event = value;
                                         _on.Call(new FlowCanvas.Flow());
                                     }

                                     private void HandleSystemEvents(GameData data)
                                     {
                                         foreach (var e in data.GetEvents<{{typeName}}>()) ManualInvoke(e);
                                     }
                                 }
                                 """);
        }

        void GenEventSender(string nodeName, string category, string typeId, string typeName)
        {
            builder.AppendLine($$"""
                                 [ParadoxNotion.Design.Category("{{category}}")]
                                 [ParadoxNotion.Design.Icon("Icons/NotifyIcon")]
                                 [ParadoxNotion.Design.Color("ff5c5c")]
                                 [ParadoxNotion.Design.Name("{{nodeName}}(Send)")]
                                 public class CustomSendEventNode_{{typeId}} : FlowCanvas.FlowNode
                                 {
                                     protected override void RegisterPorts()
                                     {
                                         var e = AddValueInput<{{typeName}}>("Event");
                                         var data = AddValueInput<Game.GameData>("GameData");
                                         AddFlowInput("Send", _ => (data.isConnected ? data.value : ((Game.EntityBlackboard)graphAgent).GameData).AppendEvent(e.value));
                                     }
                                 }
                                 """);
        }

        void GenGetDataNode(INamedTypeSymbol symbol, string nodeName, string category, string typeId, string typeName)
        {
            builder.AppendLine($$"""
                                 [ParadoxNotion.Design.Category("{{category}}")]
                                 [ParadoxNotion.Design.Name("{{nodeName}}(Get)")]
                                 public class CustomGetDataNode_{{typeId}} : FlowCanvas.FlowNode
                                 {
                                     protected override void RegisterPorts()
                                     {
                                         var data = AddValueInput<{{typeName}}>("In");
                                 """);
            foreach (var (type, name, guid, isReadOnly, isWriteOnly) in GetMembers(symbol))
            {
                if (!isWriteOnly)
                {
                    builder.AppendLine($"        AddValueOutput<{type!}>(name: \"{name!}\", ID: \"{guid!}\", () => data.value.{name!});");
                }
            }
            builder.AppendLine("    }");
            builder.AppendLine("}");
        }

        void GenSetDataNode(INamedTypeSymbol symbol, string nodeName, string category, string typeId, string typeName)
        {
            builder.AppendLine($$"""
                                 [ParadoxNotion.Design.Category("{{category}}")]
                                 [ParadoxNotion.Design.Name("{{nodeName}}(Set)")]
                                 public class CustomSetDataNode_{{typeId}} : FlowCanvas.FlowNode
                                 {
                                     protected override void RegisterPorts()
                                     {
                                 """);
            var names = new List<string>();
            foreach (var (type, name, guid, isReadOnly, isWriteOnly) in GetMembers(symbol))
            {
                if (!isReadOnly)
                {
                    names.Add(name);
                    builder.AppendLine($"        var {name} = AddValueInput<{type}>(name: \"{name}\", ID: \"{guid}\");");
                }
            }
            builder.AppendLine($$"""
                                         var data = AddValueOutput<{{typeName}}>("Out", () => new {{typeName}} { {{string.Join(", ", names.Select(name => $"{name} = {name}.value"))}} });
                                     }
                                 }
                                 """);
        }
    }

    private sealed class SyntaxContextReceiver : ISyntaxReceiver
    {
        public List<(TypeDeclarationSyntax type, AttributeSyntax attribute)> DecoratedTypes { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Check for class, struct, or interface declarations
            if (syntaxNode is not TypeDeclarationSyntax typeDeclaration) return;

            foreach (var attributeList in typeDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    // Assuming 'SomeAttribute' is the simple name of the attribute
                    // You might need to adjust the comparison for full names or other scenarios
                    if (attribute.Name.ToString() == "CustomDataNode")
                    {
                        DecoratedTypes.Add((typeDeclaration, attribute));
                        break; // Break from the inner loop once we find the attribute
                    }
                }
            }
        }
    }
}
