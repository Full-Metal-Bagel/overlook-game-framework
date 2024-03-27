// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using CodeGen.Attribute;
using CodeGen.GlobalSuppressions;
using CodeGen.DisallowConstructor;
using CodeGen.FlowNode;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var generatorType = args[0];
var inputDirectory = args[1];
var outputDirectory = args.Length > 2 ? args[2] : ".";
var unityMethodFileName = args.Length > 3 ? args[3] : "UnityMethods";
var unityMethodFilePath = Path.Combine(outputDirectory, unityMethodFileName);

if (generatorType == "sup") GenerateGlobalSuppressions();
else if (generatorType == "attr") GenerateAttributes();
else if (generatorType == "cons") DisallowDefaultConstructor();
else if (generatorType == "nodes") FlowNodes();
return;

void FlowNodes()
{
    // Source generators should be tested using 'GeneratorDriver'.
    var driver = CSharpGeneratorDriver.Create(new CustomEventNodeSourceGenerator());
    var files = Directory.GetFiles(inputDirectory, "*.cs", SearchOption.AllDirectories);

    // We need to create a compilation with the required source code.
    var compilation = CSharpCompilation.Create(
        nameof(CodeGen.FlowNode),
        files.Select(File.ReadAllText).Select(text => CSharpSyntaxTree.ParseText(text))
    );

    // Run generators and retrieve all results.
    _ = driver.RunGenerators(compilation).GetRunResult();
}

void DisallowDefaultConstructor()
{
    // Source generators should be tested using 'GeneratorDriver'.
    var driver = CSharpGeneratorDriver.Create(new DisallowDefaultConstructor());
    var files = Directory.GetFiles(inputDirectory, "*.cs", SearchOption.AllDirectories);

    // We need to create a compilation with the required source code.
    var compilation = CSharpCompilation.Create(
        nameof(CodeGen.DisallowConstructor),
        files.Select(File.ReadAllText).Select(text => CSharpSyntaxTree.ParseText(text))
    );

    // Run generators and retrieve all results.
    _ = driver.RunGenerators(compilation).GetRunResult();
}

void GenerateAttributes()
{
    var globalAttributesFile = "Attributes.g.cs";
    var outputFile = Path.Combine(outputDirectory, globalAttributesFile);
    // Source generators should be tested using 'GeneratorDriver'.
    var driver = CSharpGeneratorDriver.Create(new AttributesSourceGenerator());
    var files = Directory.GetFiles(inputDirectory, "*.cs", SearchOption.AllDirectories);

    // We need to create a compilation with the required source code.
    var compilation = CSharpCompilation.Create(
        nameof(CodeGen.Attribute),
        files.Select(File.ReadAllText).Select(text => CSharpSyntaxTree.ParseText(text))
    );

    // Run generators and retrieve all results.
    var runResult = driver.RunGenerators(compilation).GetRunResult();

    // All generated files can be found in 'RunResults.GeneratedTrees'.
    foreach (var generatedFileSyntax in runResult.GeneratedTrees.Where(t => t.FilePath.EndsWith(globalAttributesFile)))
    {
        var text = generatedFileSyntax.GetText().ToString();
        File.AppendAllText(outputFile, text);
    }
}

void GenerateGlobalSuppressions()
{
    var globalSuppressionFile = "GlobalSuppressions.g.cs";
    var outputFile = Path.Combine(outputDirectory, globalSuppressionFile);

    // Source generators should be tested using 'GeneratorDriver'.
    var driver = CSharpGeneratorDriver.Create(
        new GlobalSuppressionsUnityMethodsSourceGenerator(),
        new GlobalSuppressionsSerializeFieldSourceGenerator()
    ).AddAdditionalTexts(new AdditionalText[] { new FileAdditionalText(unityMethodFilePath) }.ToImmutableArray());

    var files = Directory.GetFiles(inputDirectory, "*.cs", SearchOption.AllDirectories);

    // We need to create a compilation with the required source code.
    var compilation = CSharpCompilation.Create(
        nameof(CodeGen.GlobalSuppressions),
        files.Select(File.ReadAllText).Select(text => CSharpSyntaxTree.ParseText(text))
    );

    // Run generators and retrieve all results.
    var runResult = driver.RunGenerators(compilation).GetRunResult();

    // All generated files can be found in 'RunResults.GeneratedTrees'.
    foreach (var generatedFileSyntax in runResult.GeneratedTrees.Where(t => t.FilePath.EndsWith(globalSuppressionFile)))
    {
        var text = generatedFileSyntax.GetText().ToString();
        File.AppendAllText(outputFile, text);
    }
}
