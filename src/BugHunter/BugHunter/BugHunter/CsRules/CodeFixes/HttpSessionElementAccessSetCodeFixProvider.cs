﻿using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using BugHunter.CsRules.Analyzers;
using BugHunter.Helpers.CodeFixes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BugHunter.CsRules.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(HttpSessionElementAccessSetCodeFixProvider)), Shared]
    public class HttpSessionElementAccessSetCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(HttpSessionElementAccessAnalyzer.DIAGNOSTIC_ID_SET);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var elementAccess = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ElementAccessExpressionSyntax>().First();
            if (elementAccess == null)
            {
                return;
            }

            var codeFixTitle = new LocalizableResourceString(nameof(CsResources.ApiReplacements_CodeFix), CsResources.ResourceManager, typeof(CsResources)).ToString();
            var usingNamespace = typeof(CMS.Helpers.SessionHelper).Namespace;
            var codeFixHelper = new CodeFixHelper(context);

            var assignmentExpression = elementAccess.FirstAncestorOrSelf<AssignmentExpressionSyntax>();
            var sessionKey = GetElementAccessKey(elementAccess);
            var valueToBeAssigned = assignmentExpression.Right;
            
            SyntaxNode oldNode = assignmentExpression;
            SyntaxNode newNode = SyntaxFactory.ParseExpression($@"SessionHelper.SetValue({sessionKey}, {valueToBeAssigned})");

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: string.Format(codeFixTitle, newNode),
                    createChangedDocument: c => codeFixHelper.ReplaceExpressionWith(oldNode, newNode, usingNamespace),
                    equivalenceKey: nameof(HttpSessionElementAccessSetCodeFixProvider)),
                diagnostic);
        }

        private ArgumentSyntax GetElementAccessKey(ElementAccessExpressionSyntax elementAccess)
        {
            return elementAccess.ArgumentList.Arguments.First();
        }
    }
}