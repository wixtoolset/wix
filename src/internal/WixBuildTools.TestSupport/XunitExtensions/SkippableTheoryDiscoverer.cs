// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport.XunitExtensions
{
    using System.Collections.Generic;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class SkippableTheoryDiscoverer : IXunitTestCaseDiscoverer
    {
        private IMessageSink DiagnosticMessageSink { get; }
        private TheoryDiscoverer TheoryDiscoverer { get; }

        public SkippableTheoryDiscoverer(IMessageSink diagnosticMessageSink)
        {
            this.DiagnosticMessageSink = diagnosticMessageSink;

            this.TheoryDiscoverer = new TheoryDiscoverer(diagnosticMessageSink);
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var defaultMethodDisplay = discoveryOptions.MethodDisplayOrDefault();
            var defaultMethodDisplayOptions = discoveryOptions.MethodDisplayOptionsOrDefault();

            // Unlike fact discovery, the underlying algorithm for theories is complex, so we let the theory discoverer
            // do its work, and do a little on-the-fly conversion into our own test cases.
            foreach (var testCase in this.TheoryDiscoverer.Discover(discoveryOptions, testMethod, factAttribute))
            {
                if (testCase is XunitTheoryTestCase)
                {
                    yield return new SkippableTheoryTestCase(this.DiagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testCase.TestMethod);
                }
                else
                {
                    yield return new SkippableFactTestCase(this.DiagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testCase.TestMethod, testCase.TestMethodArguments);
                }
            }
        }
    }
}
