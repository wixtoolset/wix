// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.TestSupport.XunitExtensions
{
    using Xunit;
    using Xunit.Sdk;

    [XunitTestCaseDiscoverer("WixInternal.TestSupport.XunitExtensions.SkippableFactDiscoverer", "WixInternal.TestSupport")]
    public class SkippableTheoryAttribute : TheoryAttribute
    {
    }
}
