// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    internal class ResolveResult : IResolveResult
    {
        public int? Codepage { get; set; }

        public int? SummaryInformationCodepage { get; set; }

        public int? PackageLcid { get; set; }

        public IReadOnlyCollection<IDelayedField> DelayedFields { get; set; }

        public IReadOnlyCollection<IExpectedExtractFile> ExpectedEmbeddedFiles { get; set; }

        public Intermediate IntermediateRepresentation { get; set; }
    }
}
