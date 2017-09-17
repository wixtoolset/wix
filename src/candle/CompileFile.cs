// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Tools
{
    /// <summary>
    /// Source code file to be compiled.
    /// </summary>
    public class CompileFile
    {
        /// <summary>
        /// Path to the source code file.
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// Path to compile the output to.
        /// </summary>
        public string OutputPath { get; set; }
    }
}
