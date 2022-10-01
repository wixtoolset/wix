// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using Example.Extension;

    internal class ExtensionPaths
    {
#if NETFRAMEWORK
        public static readonly string ExampleExtensionPath = new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase).LocalPath;
#else
        public static readonly string ExampleExtensionPath = typeof(ExampleExtensionFactory).Assembly.Location;
#endif
    }
}
