// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTestTools
{
    using System.IO;
    using WixBuildTools.TestSupport;

    public class TestExeTool : TestTool
    {
        private static readonly string TestExePath32 = Path.Combine(TestData.Get(), "win-x86", "TestExe.exe");

        public TestExeTool()
            : base(TestExePath32)
        {
        }
    }
}
