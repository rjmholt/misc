using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CmdletAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CmdletAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CmdletAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            Debugger.Launch();


            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            Compilation compilation = context.Compilation;

            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            if (!InheritsFromType(namedTypeSymbol, compilation.GetTypeByMetadataName("System.Management.Automation.Cmdlet")))
            {
                return;
            }

            INamedTypeSymbol parameterAttributeType = compilation.GetTypeByMetadataName("System.Management.Automation.ParameterAttribute");

            foreach (IPropertySymbol property in namedTypeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.Type.SpecialType == SpecialType.System_Boolean
                    && HasAttribute(property.GetAttributes(), parameterAttributeType))
                {
                    foreach (Location propertyLocation in property.Locations)
                    {
                        context.ReportDiagnostic(
                            Diagnostic.Create(
                                Rule,
                                propertyLocation));
                    }
                }
            }
        }

        private static bool InheritsFromType(INamedTypeSymbol type, INamedTypeSymbol expectedType)
        {
            INamedTypeSymbol curr = type;
            do
            {
                if (SymbolEqualityComparer.Default.Equals(curr, expectedType))
                {
                    return true;
                }

                curr = curr.BaseType;
            } while (curr != null);

            return false;
        }

        private static bool HasAttribute(ImmutableArray<AttributeData> attributes, INamedTypeSymbol attributeType)
        {
            foreach (AttributeData attribute in attributes)
            {
                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
