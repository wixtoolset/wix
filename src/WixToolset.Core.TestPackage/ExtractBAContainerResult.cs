// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.TestPackage
{
    using System.IO;
    using System.Xml;
    using Xunit;

    /// <summary>
    /// The result of extracting the BA container.
    /// </summary>
    public class ExtractBAContainerResult
    {
        /// <summary>
        /// <see cref="XmlDocument"/> for BundleExtensionData.xml.
        /// </summary>
        public XmlDocument BundleExtensionDataDocument { get; set; }

        /// <summary>
        /// <see cref="XmlNamespaceManager"/> for BundleExtensionData.xml.
        /// </summary>
        public XmlNamespaceManager BundleExtensionDataNamespaceManager { get; set; }

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
        /// 
        /// </summary>
        /// <returns></returns>
        public ExtractBAContainerResult AssertSuccess()
        {
            Assert.True(this.Success);
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
        /// Returns the relative path of the BundleExtension entry point dll in the given folder.
        /// </summary>
        /// <param name="extractedBAContainerFolderPath"></param>
        /// <param name="extensionId"></param>
        /// <returns></returns>
        public string GetBundleExtensionFilePath(string extractedBAContainerFolderPath, string extensionId)
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
        /// <param name="xpath">elements must have the 'be' prefix</param>
        /// <returns></returns>
        public XmlNodeList SelectBundleExtensionDataNodes(string xpath)
        {
            return this.BundleExtensionDataDocument.SelectNodes(xpath, this.BundleExtensionDataNamespaceManager);
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
    }
}
