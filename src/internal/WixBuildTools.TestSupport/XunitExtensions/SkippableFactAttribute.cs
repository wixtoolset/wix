// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport.XunitExtensions
{
    using Xunit;
    using Xunit.Sdk;

    // https://github.com/xunit/samples.xunit/blob/5dc1d35a63c3394a8678ac466b882576a70f56f6/DynamicSkipExample
    [XunitTestCaseDiscoverer("WixBuildTools.TestSupport.XunitExtensions.SkippableFactDiscoverer", "WixBuildTools.TestSupport")]
    public class SkippableFactAttribute : FactAttribute
    {
    }
}
