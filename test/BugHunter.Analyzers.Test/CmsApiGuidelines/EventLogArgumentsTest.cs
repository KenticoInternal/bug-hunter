﻿// Copyright (c) Zuzana Dankovcikova. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BugHunter.Analyzers.CmsApiGuidelinesRules.Analyzers;
using BugHunter.Analyzers.CmsApiGuidelinesRules.CodeFixes;
using BugHunter.Core.Constants;
using BugHunter.TestUtils;
using BugHunter.TestUtils.Helpers;
using BugHunter.TestUtils.Verifiers;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace BugHunter.Analyzers.Test.CmsApiGuidelines
{
    [TestFixture]
    public class EventLogArgumentsTest : CodeFixVerifier<EventLogArgumentsAnalyzer, EventLogArgumentsCodeFixProvider>
    {
        protected override MetadataReference[] AdditionalReferences
            => ReferencesHelper.CMSBasicReferences;

        [Test]
        public void EmptyInput_NoDiagnostic()
        {
            var test = string.Empty;

            VerifyCSharpDiagnostic(test);
        }

        [TestCase("\"I\"", "EventType.INFORMATION")]
        [TestCase("\"W\"", "EventType.WARNING")]
        [TestCase("\"E\"", "EventType.ERROR")]
        public void InputWithWrongArgument_SurfacesDiagnostic(string oldArgument, string newArgument)
        {
            var test = $@"
namespace SampleTestProject.CsSamples
{{
    public class SampleClass
    {{
        public void SampleMethod()
        {{
            CMS.EventLog.EventLogProvider.LogEvent({oldArgument}, ""source"", ""eventCode"", ""eventDescription"");
        }}
    }}
}}";
            var expectedDiagnostic = CreateDiagnosticResult(oldArgument).WithLocation(8, 52);

            VerifyCSharpDiagnostic(test, expectedDiagnostic);

            var expectedFix = $@"using CMS.EventLog;

namespace SampleTestProject.CsSamples
{{
    public class SampleClass
    {{
        public void SampleMethod()
        {{
            CMS.EventLog.EventLogProvider.LogEvent({newArgument}, ""source"", ""eventCode"", ""eventDescription"");
        }}
    }}
}}";

            VerifyCSharpFix(test, expectedFix);
        }

        [TestCase("\"S\"")]
        [TestCase("CMS.EventLog.EventType.INFORMATION")]
        [TestCase("CMS.EventLog.EventType.WARNING")]
        [TestCase("CMS.EventLog.EventType.ERROR")]
        public void InputWithOkArgument_NoDiagnostic(string oldArgument)
        {
            var test = $@"
namespace SampleTestProject.CsSamples
{{
    public class SampleClass
    {{
        public void SampleMethod()
        {{
            CMS.EventLog.EventLogProvider.LogEvent({oldArgument}, ""source"", ""eventCode"", ""eventDescription"");
        }}
    }}
}}";
            VerifyCSharpDiagnostic(test);
        }

        private static DiagnosticResult CreateDiagnosticResult(string oldArgument)
            => new DiagnosticResult
            {
                Id = DiagnosticIds.EventLogArguments,
                Message = $"LogEvent called with event type '{oldArgument}'.",
                Severity = DiagnosticSeverity.Warning,
            };
    }
}