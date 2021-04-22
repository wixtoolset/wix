// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using System.Collections.Generic;
    using WixToolset.Data;

    /// <summary>
    /// Result of resolving localization and bind variables.
    /// </summary>
    public interface IResolveResult
    {
        /// <summary>
        /// Resolved codepage, if provided.
        /// </summary>
        int? Codepage { get; set; }

        /// <summary>
        /// Resolved summary information codepage, if provided.
        /// </summary>
        int? SummaryInformationCodepage { get; set; }

        /// <summary>
        /// Resolved package language, if provided.
        /// </summary>
        int? PackageLcid { get; set; }

        /// <summary>
        /// Fields still requiring resolution.
        /// </summary>
        IReadOnlyCollection<IDelayedField> DelayedFields { get; set; }

        /// <summary>
        /// Files to extract from embedded .wixlibs.
        /// </summary>
        IReadOnlyCollection<IExpectedExtractFile> ExpectedEmbeddedFiles { get; set; }

        /// <summary>
        /// Resolved intermediate.
        /// </summary>
        Intermediate IntermediateRepresentation { get; set; }
    }
}
