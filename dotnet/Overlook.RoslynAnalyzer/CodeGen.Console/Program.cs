// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using CodeGen.GlobalSuppressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var globalSuppressionFile = "GlobalSuppressions.g.cs";
var inputDirectory = args[0];
var outputDirectory = args[1];
var unityMethodFileName = args.Length > 2 ? args[2] : "UnityMethods";
var unityMethodFilePath = Path.Combine(outputDirectory, unityMethodFileName);
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
