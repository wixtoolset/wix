// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all Burn backend extensions implement.
    /// </summary>
    public interface IBurnBackendExtension
    {
        /// <summary>
        /// Called before binding occurs.
        /// </summary>
        void PreBackendBind(IBindContext context);

        /// <summary>
        /// Called to find a file related to another source in the authoring. For example, most often used
        /// to find cabinets and uncompressed files for an MSI package.
        /// </summary>
        /// <param name="source">Path to the source package.</param>
        /// <param name="relatedSource">Expected path to the related file.</param>
        /// <param name="type">Type of related file, such as "File" or "Cabinet"</param>
        /// <param name="sourceLineNumbers">Source line number of source package.</param>
        /// <returns><c>IResolveFileResult</c> if the related file was found, or null for default handling.</returns>
        IResolveFileResult ResolveRelatedFile(string source, string relatedSource, string type, SourceLineNumber sourceLineNumbers);

        /// <summary>
        /// Called right before the output is bound into its final format.
        /// </summary>
        /// <param name="section">The finalized intermediate section.</param>
        void SymbolsFinalized(IntermediateSection section);

        /// <summary>
        /// Called to customize the DownloadUrl provided in source cde.
        /// </summary>
        /// <param name="url">The value from the source code. May not actually be a URL.</param>
        /// <param name="fallbackUrl">The default URL if the extension does not return a value.</param>
        /// <param name="packageId">Identifier of the package.</param>
        /// <param name="payloadId">Identifier of the payload.</param>
        /// <param name="fileName">Filename of the payload.</param>
        /// <returns>Url to override, or null to use default value.</returns>
        string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName);

        /// <summary>
        /// Called for each extension symbol that hasn't been handled yet.
        /// Use IBurnBackendHelper to add data.
        /// </summary>
        /// <param name="section">The linked section.</param>
        /// <param name="symbol">The current symbol.</param>
        /// <returns>
        /// True if the extension handled the symbol, false otherwise.
        /// The Burn backend will warn on all unhandled symbols.
        /// </returns>
        bool TryProcessSymbol(IntermediateSection section, IntermediateSymbol symbol);

        /// <summary>
        /// Called after output is bound into its final format.
        /// </summary>
        /// <param name="result"></param>
        void PostBackendBind(IBindResult result);
    }
}
