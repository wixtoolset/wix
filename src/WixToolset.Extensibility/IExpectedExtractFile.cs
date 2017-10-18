// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;

    public interface IExpectedExtractFile
    {
        Uri Uri { get; set; }

        int EmbeddedFileIndex { get; set; }

        string OutputPath { get; set; }
    }
}