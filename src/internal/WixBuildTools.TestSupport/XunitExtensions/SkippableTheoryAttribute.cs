// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport.XunitExtensions
{
    using Xunit;
    using Xunit.Sdk;

    [XunitTestCaseDiscoverer("WixBuildTools.TestSupport.XunitExtensions.SkippableFactDiscoverer", "WixBuildTools.TestSupport")]
    public class SkippableTheoryAttribute : TheoryAttribute
    {
    }
}
