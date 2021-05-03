// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.ManagedHost
{
    using System.Collections.Generic;

    public class TestEngineResult
    {
        public int ExitCode { get; set; }
        public List<string> Output { get; set; }
    }
}
