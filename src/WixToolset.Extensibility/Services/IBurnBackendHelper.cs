// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Services
{
    using WixToolset.Data;

    /// <summary>
    /// Interface provided to help Burn backend extensions.
    /// </summary>
    public interface IBurnBackendHelper
    {
        /// <summary>
        /// Adds the given XML to the BootstrapperApplicationData manifest.
        /// </summary>
        /// <param name="xml">A valid XML fragment.</param>
        void AddBootstrapperApplicationData(string xml);

        /// <summary>
        /// Adds an XML element for the given symbol to the BootstrapperApplicationData manifest.
        /// The symbol's name is used for the element's name.
        /// All of the symbol's fields are used for the element's attributes.
        /// </summary>
        /// <param name="symbol">The symbol to create the element from.</param>
        /// <param name="symbolIdIsIdAttribute">
        /// If true and the symbol has an Id,
        /// then an Id attribute is created with a value of the symbol's Id.
        /// </param>
        void AddBootstrapperApplicationData(IntermediateSymbol symbol, bool symbolIdIsIdAttribute = false);

        /// <summary>
        /// Adds the given XML to the BundleExtensionData manifest for the given bundle extension.
        /// </summary>
        /// <param name="extensionId">The bundle extension's id.</param>
        /// <param name="xml">A valid XML fragment.</param>
        void AddBundleExtensionData(string extensionId, string xml);

        /// <summary>
        /// Adds an XML element for the given symbol to the BundleExtensionData manifest for the given bundle extension.
        /// The symbol's name is used for the element's name.
        /// All of the symbol's fields are used for the element's attributes.
        /// </summary>
        /// <param name="extensionId">The bundle extension's id.</param>
        /// <param name="symbol">The symbol to create the element from.</param>
        /// <param name="symbolIdIsIdAttribute">
        /// If true and the symbol has an Id,
        /// then an Id attribute is created with a value of the symbol's Id.
        /// </param>
        void AddBundleExtensionData(string extensionId, IntermediateSymbol symbol, bool symbolIdIsIdAttribute = false);
    }
}
