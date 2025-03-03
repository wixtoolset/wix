// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.MSTestSupport
{
    using System.Diagnostics;

    public class ExternalExecutableResult
    {
        public int ExitCode { get; set; }

        public string[] StandardError { get; set; }

        public string[] StandardOutput { get; set; }

        public string FileName { get; set; }

        public string Arguments { get; set; }
    }
}
