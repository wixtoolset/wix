// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data;

    public class ResolveResult
    {
        public int Codepage { get; set; }

        public IEnumerable<IDelayedField> DelayedFields { get; set; }

        public IEnumerable<IExpectedExtractFile> ExpectedEmbeddedFiles { get; set; }

        public Intermediate IntermediateRepresentation { get; set; }
    }
}