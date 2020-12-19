// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System.Collections.Generic;
    using WixToolset.Data;

#pragma warning disable 1591 // TODO: add documentation
    public interface IResolveResult
    {
        int Codepage { get; set; }

        IEnumerable<IDelayedField> DelayedFields { get; set; }

        IEnumerable<IExpectedExtractFile> ExpectedEmbeddedFiles { get; set; }

        Intermediate IntermediateRepresentation { get; set; }
    }
}
