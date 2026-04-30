using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Fbt.SourceGen
{
    [Generator]
    public class BTreeActionGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidateMethods = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0,
                    transform: static (ctx, _) => GetMethodInfo(ctx))
                .Where(static m => m != null);

            var compilationAndMethods = context.CompilationProvider.Combine(candidateMethods.Collect());

            context.RegisterSourceOutput(
                compilationAndMethods,
                static (spc, source) => Execute(spc, source.Left, source.Right));
        }

        private static BTreeMethodInfo? GetMethodInfo(GeneratorSyntaxContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            var symbol = context.SemanticModel.GetDeclaredSymbol(method) as IMethodSymbol;

            if (symbol == null) return null;

            bool hasActionAttr = symbol.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "BTreeActionAttribute");
            bool hasConditionAttr = symbol.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "BTreeConditionAttribute");

            if (!hasActionAttr && !hasConditionAttr) return null;

            int paramCount = symbol.Parameters.Length;

            if (paramCount == 3)
            {
                // Reusable delegate (3-param): TValue = first param type (strip ref),
                // TContext = third param type (strip ref).
                // These are registered as bridge closures in the generated RegisterAll.
                string tvType = symbol.Parameters[0].Type.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat);
                string stateType = symbol.Parameters[1].Type.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat);
                string tcType = symbol.Parameters[2].Type.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat);

                return new BTreeMethodInfo
                {
                    MethodName = symbol.Name,
                    FullQualifiedMethodName = symbol.ContainingType.ToDisplayString() + "." + symbol.Name,
                    TBlackboardType = null,   // determined from 4-param sibling in same TContext group
                    TContextType = tcType,
                    TValueType = tvType,
                    StateType = stateType,
                    IsReusable = true,
                    IsActionKind = hasActionAttr
                };
            }

            if (paramCount != 4) return null;

            // 4-param NodeLogicDelegate: extract TBlackboard (param 0) and TContext (param 2)
            string tbType = symbol.Parameters[0].Type.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat);
            string tcType4 = symbol.Parameters[2].Type.ToDisplayString(Microsoft.CodeAnalysis.SymbolDisplayFormat.FullyQualifiedFormat);

            return new BTreeMethodInfo
            {
                MethodName = symbol.Name,
                FullQualifiedMethodName = symbol.ContainingType.ToDisplayString() + "." + symbol.Name,
                TBlackboardType = tbType,
                TContextType = tcType4,
                TValueType = null,
                StateType = null,
                IsReusable = false,
                IsActionKind = hasActionAttr
            };
        }

        private static void Execute(
            SourceProductionContext context,
            Compilation compilation,
            ImmutableArray<BTreeMethodInfo?> methods)
        {
            // Separate 4-param (registrable) from 3-param (reusable bridge) delegates.
            var registrable = new List<BTreeMethodInfo>();
            var reusable    = new List<BTreeMethodInfo>();
            foreach (var m in methods)
            {
                if (m == null) continue;
                if (m.IsReusable)
                    reusable.Add(m);
                else
                    registrable.Add(m);
            }

            if (registrable.Count == 0 && reusable.Count == 0) return;

            string assemblyName = compilation.AssemblyName ?? "Generated";
            string namespaceName = assemblyName + ".Generated";

            // Group 4-param delegates by (TBlackboard, TContext) key.
            var groups4 = registrable
                .GroupBy(m => m.TBlackboardType + "|" + m.TContextType)
                .ToDictionary(g => g.Key);

            // Group 3-param delegates by TContext.
            // They are merged into the group that shares TContext and whose TBlackboard
            // comes from any 4-param delegate in the same TContext bucket.
            var reusableByCtx = reusable
                .GroupBy(m => m.TContextType)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Build merged groups: for every 4-param (TBB,TCtx) group, attach any
            // 3-param delegates that share TCtx.
            var mergedGroups = new List<(string tbType, string tcType,
                                         List<BTreeMethodInfo> direct,
                                         List<BTreeMethodInfo> bridges)>();

            foreach (var kvp in groups4)
            {
                var first  = kvp.Value.First();
                string tb  = first.TBlackboardType!;
                string tc  = first.TContextType!;
                reusableByCtx.TryGetValue(tc, out var bridgeList);
                mergedGroups.Add((tb, tc, kvp.Value.ToList(), bridgeList ?? new List<BTreeMethodInfo>()));
            }

            // If there are orphaned 3-param groups (no matching 4-param for that TContext),
            // we cannot infer TBlackboard -- skip them silently (they must be registered
            // manually or via a 4-param sibling).

            if (mergedGroups.Count == 0) return;

            var source = GenerateRegistrar(mergedGroups, namespaceName);
            context.AddSource("FbtActionRegistrar.g.cs", source);
        }

        private static string GenerateRegistrar(
            List<(string tbType, string tcType,
                  List<BTreeMethodInfo> direct,
                  List<BTreeMethodInfo> bridges)> groups,
            string namespaceName)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine();
            sb.AppendLine("namespace " + namespaceName);
            sb.AppendLine("{");
            sb.AppendLine("    [global::Fbt.FbtRegistrar]");
            sb.AppendLine("    public static class FbtActionRegistrar");
            sb.AppendLine("    {");
            sb.AppendLine("        // 4-param NodeLogicDelegate methods are registered directly.");
            sb.AppendLine("        // 3-param ReusableDelegate methods are registered as bridge closures");
            sb.AppendLine("        // using Unsafe.As to project the runtime blackboard to TValue.");

            foreach (var (tbType, tcType, direct, bridges) in groups)
            {
                sb.AppendLine();
                sb.AppendLine("        public static void RegisterAll(");
                sb.AppendLine("            global::Fbt.Runtime.ActionRegistry<" + tbType + ", " + tcType + "> registry)");
                sb.AppendLine("        {");

                // 4-param delegates: register directly by method name (no @offset suffix)
                foreach (var m in direct)
                {
                    sb.AppendLine("            registry.Register(\"" + m.FullQualifiedMethodName + "\", global::" + m.FullQualifiedMethodName + ");");
                }

                // 3-param delegates: register as bridge closures with "@0" key suffix
                foreach (var m in bridges)
                {
                    string stateType  = m.StateType ?? "global::Fbt.BehaviorTreeState";
                    string valueType  = m.TValueType!;
                    string key = m.FullQualifiedMethodName + "@0";

                    sb.AppendLine("            registry.Register(\"" + key + "\",");
                    sb.AppendLine("                (ref " + tbType + " bb, ref " + stateType + " st, ref " + tcType + " ctx, int _) =>");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    ref var p = ref global::System.Runtime.CompilerServices.Unsafe.As<" + tbType + ", " + valueType + ">(ref bb);");
                    sb.AppendLine("                    return global::" + m.FullQualifiedMethodName + "(ref p, ref st, ref ctx);");
                    sb.AppendLine("                });");;
                }

                sb.AppendLine("        }");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }

    internal class BTreeMethodInfo
    {
        public string MethodName { get; set; } = "";
        public string FullQualifiedMethodName { get; set; } = "";
        public string? TBlackboardType { get; set; }
        public string? TContextType { get; set; }
        /// <summary>For 3-param delegates: the TValue type (first param, stripped of ref).</summary>
        public string? TValueType { get; set; }
        /// <summary>For 3-param delegates: fully-qualified BehaviorTreeState type from param 1.</summary>
        public string? StateType { get; set; }
        /// <summary>True for 3-param ReusableDelegate; false for 4-param NodeLogicDelegate.</summary>
        public bool IsReusable { get; set; }
        public bool IsActionKind { get; set; }
    }
}
