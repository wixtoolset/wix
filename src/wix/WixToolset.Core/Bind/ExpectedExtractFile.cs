// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Bind
{
    using System;
    using WixToolset.Extensibility.Data;

    internal class ExpectedExtractFile : IExpectedExtractFile
    {
        public Uri Uri { get; set; }

        public string EmbeddedFileId { get; set; }

        public string OutputPath { get; set; }
    }
}
