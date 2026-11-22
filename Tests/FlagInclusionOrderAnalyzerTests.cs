using System.Collections.Immutable;
using CoreSourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Z2Randomizer.Tests;

[TestClass]
public class FlagInclusionOrderAnalyzerTests
{
    private const string FixturePreamble = """
        using System;
        sealed class FlagSerializeAttribute : Attribute { }
        sealed class ConditionallyIncludeInFlagsAttribute : Attribute { }
        sealed class IgnoreInFlagsAttribute : Attribute { }
        [FlagSerialize]
        partial class Config
        {
        """;

    [TestMethod]
    [DataRow("parent")]
    [DataRow("this.parent")]
    [DataRow("Helper()")]
    [DataRow("Parent")]
    [DataRow("ParentWithAccessor")]
    [DataRow("Local()")]
    public async Task RejectsLaterFieldsThroughPredicatesHelpersAndGetters(string expression)
    {
        string source = $$"""
            [ConditionallyIncludeInFlags] bool child = false;
            bool childIncluded()
            {
                bool Local() => parent;
                return {{expression}};
            }
            bool Helper() => SecondHelper();
            bool SecondHelper() => parent;
            bool Parent => parent;
            bool ParentWithAccessor { get { return parent; } }
            bool parent = true;
            """;

        var diagnostics = await Analyze(source);

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("Z2R002", diagnostics[0].Id);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostics[0].Severity);
        StringAssert.Contains(diagnostics[0].GetMessage(), "'childIncluded()'");
        StringAssert.Contains(diagnostics[0].GetMessage(), "'parent'");
    }

    [TestMethod]
    public async Task RejectsReadingTheFlagBeingDecoded()
    {
        var diagnostics = await Analyze("""
            [ConditionallyIncludeInFlags] bool child = false;
            bool childIncluded() => child;
            """);

        Assert.HasCount(1, diagnostics);
        StringAssert.Contains(diagnostics[0].GetMessage(), "field 'child'");
    }

    [TestMethod]
    public async Task RejectsLaterFieldsThroughAnotherIncludedPredicate()
    {
        var diagnostics = await Analyze("""
            [ConditionallyIncludeInFlags] bool child = false;
            bool childIncluded() => parentIncluded();
            [ConditionallyIncludeInFlags] bool parent = false;
            bool parentIncluded() => grandparent;
            bool grandparent = true;
            """);

        Assert.HasCount(2, diagnostics);
        Assert.IsTrue(diagnostics.All(diagnostic => diagnostic.GetMessage().Contains("field 'grandparent'")));
    }

    [TestMethod]
    public async Task AllowsEarlierFieldsIncludingAnEarlierConditionalFlag()
    {
        var diagnostics = await Analyze("""
            bool grandparent = true;
            [ConditionallyIncludeInFlags] bool parent = false;
            bool parentIncluded() => grandparent;
            [ConditionallyIncludeInFlags] bool child = false;
            bool childIncluded() => parentIncluded() && Parent;
            bool Parent => parent;
            """);

        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public async Task TracksReactivePropertyGettersWithoutFollowingSetters()
    {
        var diagnostics = await Analyze("""
            bool parent = true;
            [ConditionallyIncludeInFlags] bool child = false;
            bool childIncluded() => Parent;
            bool Parent { get => parent; set { parent = value; later = value; } }
            bool later = false;
            """);

        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public async Task IgnoresNamesConstantsLocalsAndFieldsOutsideTheFlagPayload()
    {
        var diagnostics = await Analyze("""
            [ConditionallyIncludeInFlags] bool child = false;
            bool childIncluded()
            {
                bool parent = true;
                return parent && nameof(later).Length > 0 && Constant && ignored;
            }
            bool parent = false;
            bool later = false;
            const bool Constant = true;
            [IgnoreInFlags] bool ignored = true;
            """);

        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public async Task UiOnlyPredicatesDoNotImposeAFlagDecodingOrder()
    {
        var diagnostics = await Analyze("""
            bool child = false;
            bool childIncluded() => parent;
            bool parent = true;
            """);

        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public async Task RepeatedReferencesReportEachDependencyOnce()
    {
        var diagnostics = await Analyze("""
            [ConditionallyIncludeInFlags] bool child = false;
            bool childIncluded() => parent || Helper();
            bool Helper() => parent;
            bool parent = true;
            """);

        Assert.HasCount(1, diagnostics);
    }

    [TestMethod]
    public async Task ChecksGeneratedReactiveGettersAndReportsAtTheAuthoredFlag()
    {
        var diagnostics = await Analyze("""
            [ConditionallyIncludeInFlags] bool child = false;
            bool childIncluded() => Parent;
            bool parent = true;
            """, """
            // <auto-generated/>
            partial class Config
            {
                public bool Parent { get => parent; set => parent = value; }
            }
            """);

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("Config.cs", diagnostics[0].Location.SourceTree!.FilePath);
    }

    [TestMethod]
    public async Task DoesNotEvaluateAnUnusedLocalFunction()
    {
        var diagnostics = await Analyze("""
            [ConditionallyIncludeInFlags] bool child = false;
            bool childIncluded()
            {
                bool Unused() => parent;
                return true;
            }
            bool parent = true;
            """);

        Assert.IsEmpty(diagnostics);
    }

    [TestMethod]
    public async Task HandlesRecursiveHelperGraphsWithoutRepeatedDiagnostics()
    {
        var diagnostics = await Analyze("""
            [ConditionallyIncludeInFlags] bool child = false;
            bool childIncluded() => Helper();
            bool Helper() => parent || childIncluded();
            bool parent = true;
            """);

        Assert.HasCount(1, diagnostics);
    }

    [TestMethod]
    public async Task FollowsHelpersDeclaredOnOtherTypes()
    {
        var diagnostics = await Analyze("""
            [ConditionallyIncludeInFlags] bool child = false;
            bool childIncluded() => HelperType.IsIncluded(this);
            bool parent = true;
            bool Parent => parent;
            static class HelperType
            {
                public static bool IsIncluded(Config config) => config.Parent;
            }
            """);

        Assert.HasCount(1, diagnostics);
        StringAssert.Contains(diagnostics[0].GetMessage(), "field 'parent'");
    }

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string members, string? generatedMembers = null)
    {
        var source = CSharpSyntaxTree.ParseText(FixturePreamble + Environment.NewLine + members + "\n}", path: "Config.cs");
        var compilation = CSharpCompilation.Create("FlagOrderFixture", [source],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        if (generatedMembers != null)
            compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText(generatedMembers, path: "Config.Reactive.g.cs"));
        Assert.IsEmpty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
            "The fixture must compile so analyzer results cannot hide unresolved references");

        return await compilation.WithAnalyzers([new FlagInclusionOrderAnalyzer()]).GetAnalyzerDiagnosticsAsync();
    }
}
