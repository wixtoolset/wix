// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Interfaces
{
    using System.Diagnostics;
    using WixToolset.Data.Symbols;

    /// <summary>
    /// Service for harvesting payload information.
    /// </summary>
    public interface IPayloadHarvester
    {
        /// <summary>
        /// Uses <see cref="WixBundlePayloadSymbol.SourceFile"/> to:
        ///   update <see cref="WixBundlePayloadSymbol.Hash"/> from file contents,
        ///   update <see cref="WixBundlePayloadSymbol.FileSize"/> from file size, and
        ///   update <see cref="WixBundlePayloadSymbol.Description"/>, <see cref="WixBundlePayloadSymbol.DisplayName"/>, and <see cref="WixBundlePayloadSymbol.Version"/> from <see cref="FileVersionInfo"/>.
        /// </summary>
        /// <param name="payload">The symbol to update.</param>
        /// <returns>Whether the symbol had a source file specified.</returns>
        bool HarvestStandardInformation(WixBundlePayloadSymbol payload);
    }
}
