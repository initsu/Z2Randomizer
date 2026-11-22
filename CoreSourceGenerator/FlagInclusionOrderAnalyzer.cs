using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace CoreSourceGenerator;

/// <summary>
/// Checks the field dependencies of conditional flag predicates in
/// <c>[FlagSerialize]</c> classes, principally
/// <c>RandomizerCore/RandomizerConfiguration.cs</c>. Build error <c>Z2R002</c>
/// prevents a predicate from depending on flag values that have not been decoded.
/// </summary>
/// <remarks>
/// <c>FlagsSerializeGenerator.cs</c> generates the positional flag-string codec.
/// Its decoder visits serialized fields in declaration order and calls
/// <c>fieldIncluded()</c> before reading each <c>[ConditionallyIncludeInFlags]</c>
/// field. Every serialized field used by that predicate must therefore precede
/// the conditional field. For example, <c>shuffleOverworldItems</c> must precede
/// <c>includeSwordTechsInShuffle</c>, whose inclusion predicate reads that parent.
/// <para>
/// A declaration initializer does not make a later field safe: the decoder would
/// see the default or a previous value instead of the value in the incoming flag
/// string. A wrong inclusion decision can then misalign all subsequent reads.
/// This is a compile-time check in both Debug and Release, unlike the runtime
/// assignment checks generated for <c>RandomizerProperties</c>.
/// </para>
/// <para>
/// Dependencies are followed through helper methods, other inclusion predicates,
/// and property getters, including generated reactive getters. Only fields in the
/// decoded payload impose ordering constraints. Constants, <c>nameof</c>, and
/// <c>[IgnoreInFlags]</c> fields are excluded. Predicates used only for UI
/// enablement have no decoding-order constraint until their field becomes
/// <c>[ConditionallyIncludeInFlags]</c>.
/// </para>
/// <para>
/// Diagnostics point at the conditional field and also identify the dependency
/// reference. Field order is part of the flag-string format, so moving fields to
/// fix a diagnostic requires considering compatibility with existing flags.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FlagInclusionOrderAnalyzer : DiagnosticAnalyzer
{
#pragma warning disable RS2008 // This repo-local analyzer is not released as a package.
    private static readonly DiagnosticDescriptor UndecodedDependency = new(
        "Z2R002", "Flag inclusion depends on an undecoded field",
        "Flag '{0}' evaluates '{1}()' before field '{2}' is decoded; declare '{2}' before '{0}'",
        "Initialization", DiagnosticSeverity.Error, isEnabledByDefault: true);
#pragma warning restore RS2008

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [UndecodedDependency];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeClass, SymbolKind.NamedType);
    }

    private static void AnalyzeClass(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (!HasAttribute(type, "FlagSerializeAttribute"))
            return;

        // Match FlagsSerializeGenerator's declaration order and IgnoreInFlags filtering.
        var fields = type.GetMembers().OfType<IFieldSymbol>()
            .Where(field => !field.IsImplicitlyDeclared && !HasAttribute(field, "IgnoreInFlagsAttribute"))
            .ToArray();
        var positions = new Dictionary<IFieldSymbol, int>(SymbolEqualityComparer.Default);
        for (int index = 0; index < fields.Length; index++)
            positions.Add(fields[index], index);

        foreach (var field in fields)
        {
            if (!HasAttribute(field, "ConditionallyIncludeInFlagsAttribute"))
                continue;

            string predicateName = field.Name + "Included";
            var predicate = type.GetMembers(predicateName).OfType<IMethodSymbol>()
                .FirstOrDefault(method => method.Parameters.Length == 0);
            if (predicate == null)
                continue; // The generated serializer already produces a compiler error for a missing predicate.

            var dependencies = new DependencyWalker(context.Compilation, type, context.CancellationToken);
            dependencies.VisitMethod(predicate);
            foreach (var dependency in dependencies.Fields)
            {
                if (positions.TryGetValue(dependency.Key, out int position) && position >= positions[field])
                {
                    // Point at the authored flag, even when a generated reactive getter reads the dependency.
                    context.ReportDiagnostic(Diagnostic.Create(UndecodedDependency, field.Locations[0],
                        additionalLocations: [dependency.Value], properties: null,
                        messageArgs: [field.Name, predicateName, dependency.Key.Name]));
                }
            }
        }
    }

    private static bool HasAttribute(ISymbol symbol, string name) =>
        symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.Name == name);

    private sealed class DependencyWalker(
        Compilation compilation, INamedTypeSymbol owner, CancellationToken cancellationToken) : OperationWalker
    {
        private readonly HashSet<IMethodSymbol> visitedMethods = new(SymbolEqualityComparer.Default);
        public Dictionary<IFieldSymbol, Location> Fields { get; } = new(SymbolEqualityComparer.Default);

        public void VisitMethod(IMethodSymbol? method)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (method == null || !visitedMethods.Add(method.OriginalDefinition))
                return;

            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                var declaration = reference.GetSyntax(cancellationToken);
                SyntaxNode? body = declaration switch
                {
                    MethodDeclarationSyntax member => (SyntaxNode?)member.Body ?? member.ExpressionBody?.Expression,
                    AccessorDeclarationSyntax accessor => (SyntaxNode?)accessor.Body ?? accessor.ExpressionBody?.Expression,
                    PropertyDeclarationSyntax property => property.ExpressionBody?.Expression,
                    ArrowExpressionClauseSyntax expression => expression.Expression,
                    LocalFunctionStatementSyntax local => (SyntaxNode?)local.Body ?? local.ExpressionBody?.Expression,
                    _ => null,
                };
                if (body != null)
                    Visit(compilation.GetSemanticModel(body.SyntaxTree).GetOperation(body, cancellationToken));
            }
        }

        public override void VisitFieldReference(IFieldReferenceOperation operation)
        {
            if (!operation.Field.IsConst && SymbolEqualityComparer.Default.Equals(operation.Field.ContainingType, owner)
                && !Fields.ContainsKey(operation.Field))
                Fields.Add(operation.Field, operation.Syntax.GetLocation());
            base.VisitFieldReference(operation);
        }

        public override void VisitInvocation(IInvocationOperation operation)
        {
            VisitMethod(operation.TargetMethod);
            base.VisitInvocation(operation);
        }

        public override void VisitPropertyReference(IPropertyReferenceOperation operation)
        {
            VisitMethod(operation.Property.GetMethod);
            base.VisitPropertyReference(operation);
        }

        public override void VisitMethodReference(IMethodReferenceOperation operation)
        {
            VisitMethod(operation.Method);
            base.VisitMethodReference(operation);
        }

        public override void VisitNameOf(INameOfOperation operation)
        {
            // nameof mentions a field without reading its undecoded value.
        }

        public override void VisitLocalFunction(ILocalFunctionOperation operation)
        {
            // Its body is evaluated only if VisitInvocation reaches it.
        }
    }
}
