﻿// Copyright (c) Zuzana Dankovcikova. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BugHunter.Core.Constants;
using BugHunter.Core.Extensions;
using BugHunter.Core.Helpers.DiagnosticDescriptors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace BugHunter.AnalyzersVersions.BaseClassAnalyzers
{
    /// <summary>
    /// !!! THIS FILE SERVES ONLY FOR PURPOSES OF PERFORMANCE TESTING !!!
    ///
    /// Checks if Page file inherits from right class. -- register syntax tree action action
    /// </summary>
    // [DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning disable RS1001 // Missing diagnostic analyzer attribute.
    public class PageBaseAnalyzerRegisterSyntaxTreeAction : DiagnosticAnalyzer
#pragma warning restore RS1001 // Missing diagnostic analyzer attribute.
    {
        /// <summary>
        /// The ID for diagnostics raises by <see cref="PageBaseAnalyzerRegisterSyntaxTreeAction"/>
        /// </summary>
        public const string DiagnosticId = "BH3502";

        private static readonly DiagnosticDescriptor Rule = BaseClassesInheritanceRulesProvider.GetRule(DiagnosticId, "Page", "some abstract CMSPage");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var systemWebUiPageType = compilationContext.Compilation.GetTypeByMetadataName("System.Web.UI.Page");
                if (systemWebUiPageType == null)
                {
                    return;
                }

                compilationContext.RegisterSyntaxTreeAction(syntaxTreeAnalysisContext =>
                {
                    var filePath = syntaxTreeAnalysisContext.Tree.FilePath;
                    if (string.IsNullOrEmpty(filePath) || !filePath.EndsWith(FileExtensions.Pages))
                    {
                        return;
                    }

                    var publicPartialInstantiableClassDeclarations = GetAllClassDeclarations(syntaxTreeAnalysisContext)
                        .Where(classDeclarationSyntax
                            => classDeclarationSyntax.IsPublic()
                            && !classDeclarationSyntax.IsAbstract()
                            && classDeclarationSyntax.IsPartial())
                        .ToArray();

                    if (!publicPartialInstantiableClassDeclarations.Any())
                    {
                        return;
                    }

                    var semanticModel = compilationContext.Compilation.GetSemanticModel(syntaxTreeAnalysisContext.Tree);

                    foreach (var classDeclaration in publicPartialInstantiableClassDeclarations)
                    {
                        var baseTypeTypeSymbol = GetBaseTypeSymbol(classDeclaration, semanticModel);
                        if (baseTypeTypeSymbol != null && baseTypeTypeSymbol.Equals(systemWebUiPageType))
                        {
                            var diagnostic = CreateDiagnostic(syntaxTreeAnalysisContext, classDeclaration, Rule);
                            syntaxTreeAnalysisContext.ReportDiagnostic(diagnostic);
                        }
                    }
                });
            });
        }

        private static IEnumerable<ClassDeclarationSyntax> GetAllClassDeclarations(SyntaxTreeAnalysisContext syntaxTreeAnalysisContext)
        {
            return syntaxTreeAnalysisContext
                .Tree
                .GetRoot()
                .DescendantNodesAndSelf()
                .OfType<ClassDeclarationSyntax>();
        }

        private static INamedTypeSymbol GetBaseTypeSymbol(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
        {
            // if class is not extending nor implementing anything, it has no base type
            if (classDeclaration.BaseList == null || classDeclaration.BaseList.Types.IsNullOrEmpty())
            {
                return null;
            }

            return semanticModel.GetDeclaredSymbol(classDeclaration).BaseType;
        }

        private static Diagnostic CreateDiagnostic(SyntaxTreeAnalysisContext syntaxTreeAnalysisContext, ClassDeclarationSyntax classDeclaration, DiagnosticDescriptor rule)
        {
            var location = syntaxTreeAnalysisContext.Tree.GetLocation(classDeclaration.Identifier.FullSpan);
            var diagnostic = Diagnostic.Create(rule, location, classDeclaration.Identifier.ToString());
            return diagnostic;
        }
    }
}
