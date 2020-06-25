// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;

    public interface IBurnBackendExtension
    {
        /// <summary>
        /// Called before binding occurs.
        /// </summary>
        void PreBackendBind(IBindContext context);

        IResolveFileResult ResolveRelatedFile(string source, string relatedSource, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage);

        string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName);

        /// <summary>
        /// Called for each extension symbol that hasn't been handled yet.
        /// Use IBurnBackendHelper to add data to the appropriate data manifest.
        /// </summary>
        /// <param name="section">The linked section.</param>
        /// <param name="symbol">The current symbol.</param>
        /// <returns>
        /// True if the extension handled the symbol, false otherwise.
        /// The Burn backend will warn on all unhandled symbols.
        /// </returns>
        bool TryAddSymbolToDataManifest(IntermediateSection section, IntermediateSymbol symbol);

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        void BundleFinalize();

        /// <summary>
        /// Called after output is bound into its final format.
        /// </summary>
        /// <param name="result"></param>
        void PostBackendBind(IBindResult result);
    }
}
