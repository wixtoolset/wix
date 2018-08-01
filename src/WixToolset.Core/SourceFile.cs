// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    internal class SourceFile
    {
        public SourceFile(string sourcePath, string outputPath)
        {
            this.SourcePath = sourcePath;
            this.OutputPath = outputPath;
        }

        public string OutputPath { get; }

        public string SourcePath { get; }
    }
}
