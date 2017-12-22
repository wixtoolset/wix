// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.LightIntegration.Utility
{
    using System;
    using System.IO;

    public class TestData
    {
        public static string LocalPath => Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);

        public static string Get(params string[] paths)
        {
            return Path.Combine(LocalPath, Path.Combine(paths));
        }
    }
}
