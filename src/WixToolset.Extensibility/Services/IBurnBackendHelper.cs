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
        /// Adds an XML element for the given tuple to the BootstrapperApplicationData manifest.
        /// The tuple's name is used for the element's name.
        /// All of the tuple's fields are used for the element's attributes.
        /// </summary>
        /// <param name="tuple">The tuple to create the element from.</param>
        /// <param name="tupleIdIsIdAttribute">
        /// If true and the tuple has an Id,
        /// then an Id attribute is created with a value of the tuple's Id.
        /// </param>
        void AddBootstrapperApplicationData(IntermediateTuple tuple, bool tupleIdIsIdAttribute = false);

        /// <summary>
        /// Adds the given XML to the BundleExtensionData manifest for the given bundle extension.
        /// </summary>
        /// <param name="extensionId">The bundle extension's id.</param>
        /// <param name="xml">A valid XML fragment.</param>
        void AddBundleExtensionData(string extensionId, string xml);

        /// <summary>
        /// Adds an XML element for the given tuple to the BundleExtensionData manifest for the given bundle extension.
        /// The tuple's name is used for the element's name.
        /// All of the tuple's fields are used for the element's attributes.
        /// </summary>
        /// <param name="extensionId">The bundle extension's id.</param>
        /// <param name="tuple">The tuple to create the element from.</param>
        /// <param name="tupleIdIsIdAttribute">
        /// If true and the tuple has an Id,
        /// then an Id attribute is created with a value of the tuple's Id.
        /// </param>
        void AddBundleExtensionData(string extensionId, IntermediateTuple tuple, bool tupleIdIsIdAttribute = false);
    }
}
