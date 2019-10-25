// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System;

    public interface IExpectedExtractFile
    {
        Uri Uri { get; set; }

        string EmbeddedFileId { get; set; }

        string OutputPath { get; set; }
    }
}