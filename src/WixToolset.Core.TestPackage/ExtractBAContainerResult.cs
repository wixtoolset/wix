// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.TestPackage
{
    using System.IO;
    using System.Xml;
    using Xunit;

    public class ExtractBAContainerResult
    {
        public XmlDocument BundleExtensionDataDocument { get; set; }
        public XmlNamespaceManager BundleExtensionDataNamespaceManager { get; set; }
        public XmlDocument BADataDocument { get; set; }
        public XmlNamespaceManager BADataNamespaceManager { get; set; }
        public XmlDocument ManifestDocument { get; set; }
        public XmlNamespaceManager ManifestNamespaceManager { get; set; }
        public bool Success { get; set; }

        public ExtractBAContainerResult AssertSuccess()
        {
            Assert.True(this.Success);
            return this;
        }

        public string GetBAFilePath(string extractedBAContainerFolderPath)
        {
            var uxPayloads = this.SelectManifestNodes("/burn:BurnManifest/burn:UX/burn:Payload");
            var baPayload = uxPayloads[0];
            var relativeBAPath = baPayload.Attributes["FilePath"].Value;
            return Path.Combine(extractedBAContainerFolderPath, relativeBAPath);
        }

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
