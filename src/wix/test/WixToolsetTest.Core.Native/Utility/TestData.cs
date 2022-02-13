// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreNative.Utility
{
    using System;
    using System.IO;

    public class TestData
    {
        public static string GetLocalPath()
#if !(NET461 || NET472 || NET48 || NETCOREAPP3_1 || NET5_0)
        {
            throw new System.NotImplementedException();
        }
#else
        {
#if NET461 || NET472 || NET48
            var localPath = (new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase)).LocalPath;
#else // NETCOREAPP3_1 || NET5_0
            var localPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
#endif
            return Path.GetDirectoryName(localPath);
        }
#endif

        public static string Get(params string[] paths)
        {
            return Path.Combine(GetLocalPath(), Path.Combine(paths));
        }
    }
}
