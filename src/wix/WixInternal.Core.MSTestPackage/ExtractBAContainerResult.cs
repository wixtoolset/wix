// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.Core.MSTestPackage
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// The result of extracting the BA container.
    /// </summary>
    public class ExtractBAContainerResult
    {
        /// <summary>
        /// <see cref="XmlDocument"/> for BootstrapperExtensionData.xml.
        /// </summary>
        public XmlDocument BootstrapperExtensionDataDocument { get; set; }

        /// <summary>
        /// <see cref="XmlNamespaceManager"/> for BootstrapperExtensionData.xml.
        /// </summary>
        public XmlNamespaceManager BootstrapperExtensionDataNamespaceManager { get; set; }

        /// <summary>
        /// <see cref="XmlDocument"/> for BootstrapperApplicationData.xml.
        /// </summary>
        public XmlDocument BADataDocument { get; set; }

        /// <summary>
        /// <see cref="XmlNamespaceManager"/> for BootstrapperApplicationData.xml.
        /// </summary>
        public XmlNamespaceManager BADataNamespaceManager { get; set; }

        /// <summary>
        /// <see cref="XmlDocument"/> for the Burn manifest.xml.
        /// </summary>
        public XmlDocument ManifestDocument { get; set; }

        /// <summary>
        /// <see cref="XmlNamespaceManager"/> for the Burn manifest.xml.
        /// </summary>
        public XmlNamespaceManager ManifestNamespaceManager { get; set; }

        /// <summary>
        /// Whether extraction succeeded.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Whether attached containers extraction succeeded.
        /// </summary>
        public bool? AttachedContainersSuccess { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public ExtractBAContainerResult AssertSuccess()
        {
            Assert.IsTrue(this.Success);
            Assert.IsTrue(!this.AttachedContainersSuccess.HasValue || this.AttachedContainersSuccess.Value);
            return this;
        }

        /// <summary>
        /// Returns the relative path of the BA entry point dll in the given folder.
        /// </summary>
        /// <param name="extractedBAContainerFolderPath"></param>
        /// <returns></returns>
        public string GetBAFilePath(string extractedBAContainerFolderPath)
        {
            var uxPayloads = this.SelectManifestNodes("/burn:BurnManifest/burn:UX/burn:Payload");
            var baPayload = uxPayloads[0];
            var relativeBAPath = baPayload.Attributes["FilePath"].Value;
            return Path.Combine(extractedBAContainerFolderPath, relativeBAPath);
        }

        /// <summary>
        /// Returns the relative path of the BootstrapperExtension entry point dll in the given folder.
        /// </summary>
        /// <param name="extractedBAContainerFolderPath"></param>
        /// <param name="extensionId"></param>
        /// <returns></returns>
        public string GetBootstrapperExtensionFilePath(string extractedBAContainerFolderPath, string extensionId)
        {
            var uxPayloads = this.SelectManifestNodes($"/burn:BurnManifest/burn:UX/burn:Payload[@Id='{extensionId}']");
            var bextPayload = uxPayloads[0];
            var relativeBextPath = bextPayload.Attributes["FilePath"].Value;
            return Path.Combine(extractedBAContainerFolderPath, relativeBextPath);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xpath">elements must have the 'ba' prefix</param>
        /// <returns></returns>
        public XmlNodeList SelectBADataNodes(string xpath)
        {
            return this.BADataDocument.SelectNodes(xpath, this.BADataNamespaceManager);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xpath">elements must have the 'ba' prefix</param>
        /// <param name="ignoredAttributesByElementName">Attributes for which the value should be set to '*'.</param>
        /// <returns></returns>
        public string[] GetBADataTestXmlLines(string xpath, Dictionary<string, List<string>> ignoredAttributesByElementName = null)
        {
            return this.SelectBADataNodes(xpath).GetTestXmlLines(ignoredAttributesByElementName);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xpath">elements must have the 'be' prefix</param>
        /// <returns></returns>
        public XmlNodeList SelectBootstrapperExtensionDataNodes(string xpath)
        {
            return this.BootstrapperExtensionDataDocument.SelectNodes(xpath, this.BootstrapperExtensionDataNamespaceManager);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xpath">elements must have the 'be' prefix</param>
        /// <param name="ignoredAttributesByElementName">Attributes for which the value should be set to '*'.</param>
        /// <returns></returns>
        public string[] GetBootstrapperExtensionTestXmlLines(string xpath, Dictionary<string, List<string>> ignoredAttributesByElementName = null)
        {
            return this.SelectBootstrapperExtensionDataNodes(xpath).GetTestXmlLines(ignoredAttributesByElementName);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xpath">elements must have the 'burn' prefix</param>
        /// <returns></returns>
        public XmlNodeList SelectManifestNodes(string xpath)
        {
            return this.ManifestDocument.SelectNodes(xpath, this.ManifestNamespaceManager);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="xpath">elements must have the 'burn' prefix</param>
        /// <param name="ignoredAttributesByElementName">Attributes for which the value should be set to '*'.</param>
        /// <returns></returns>
        public string[] GetManifestTestXmlLines(string xpath, Dictionary<string, List<string>> ignoredAttributesByElementName = null)
        {
            return this.SelectManifestNodes(xpath).GetTestXmlLines(ignoredAttributesByElementName);
        }
    }
}
