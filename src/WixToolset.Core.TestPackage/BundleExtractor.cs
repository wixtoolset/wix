// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.TestPackage
{
    using System.IO;
    using System.Xml;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Class to extract bundle contents for testing.
    /// </summary>
    public class BundleExtractor
    {
        /// <summary>
        /// Extracts the BA container.
        /// </summary>
        /// <param name="messaging"></param>
        /// <param name="bundleFilePath">Path to the bundle.</param>
        /// <param name="destinationFolderPath">Path to extract to.</param>
        /// <param name="tempFolderPath">Temp path for extraction.</param>
        /// <returns></returns>
        public static ExtractBAContainerResult ExtractBAContainer(IMessaging messaging, string bundleFilePath, string destinationFolderPath, string tempFolderPath)
        {
            var result = new ExtractBAContainerResult();
            Directory.CreateDirectory(tempFolderPath);
            using (var burnReader = BurnReader.Open(messaging, bundleFilePath))
            {
                result.Success = burnReader.ExtractUXContainer(destinationFolderPath, tempFolderPath);
            }

            if (result.Success)
            {
                result.ManifestDocument = LoadBurnManifest(destinationFolderPath);
                result.ManifestNamespaceManager = GetBurnNamespaceManager(result.ManifestDocument, "burn");

                result.BADataDocument = LoadBAData(destinationFolderPath);
                result.BADataNamespaceManager = GetBADataNamespaceManager(result.BADataDocument, "ba");

                result.BundleExtensionDataDocument = LoadBundleExtensionData(destinationFolderPath);
                result.BundleExtensionDataNamespaceManager = GetBundleExtensionDataNamespaceManager(result.BundleExtensionDataDocument, "be");
            }

            return result;
        }

        /// <summary>
        /// Gets an <see cref="XmlNamespaceManager"/> for BootstrapperApplicationData.xml with the given prefix assigned to the root namespace.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static XmlNamespaceManager GetBADataNamespaceManager(XmlDocument document, string prefix)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace(prefix, BurnCommon.BADataNamespace);
            return namespaceManager;
        }

        /// <summary>
        /// Gets an <see cref="XmlNamespaceManager"/> for BundleExtensionData.xml with the given prefix assigned to the root namespace.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static XmlNamespaceManager GetBundleExtensionDataNamespaceManager(XmlDocument document, string prefix)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace(prefix, BurnCommon.BundleExtensionDataNamespace);
            return namespaceManager;
        }

        /// <summary>
        /// Gets an <see cref="XmlNamespaceManager"/> for the Burn manifest.xml with the given prefix assigned to the root namespace.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static XmlNamespaceManager GetBurnNamespaceManager(XmlDocument document, string prefix)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace(prefix, BurnCommon.BurnNamespace);
            return namespaceManager;
        }

        /// <summary>
        /// Loads an XmlDocument with the BootstrapperApplicationData.xml from the given folder that contains the contents of the BA container.
        /// </summary>
        /// <param name="baFolderPath"></param>
        /// <returns></returns>
        public static XmlDocument LoadBAData(string baFolderPath)
        {
            var document = new XmlDocument();
            document.Load(Path.Combine(baFolderPath, BurnCommon.BADataFileName));
            return document;
        }

        /// <summary>
        /// Loads an XmlDocument with the BootstrapperApplicationData.xml from the given folder that contains the contents of the BA container.
        /// </summary>
        /// <param name="baFolderPath"></param>
        /// <returns></returns>
        public static XmlDocument LoadBundleExtensionData(string baFolderPath)
        {
            var document = new XmlDocument();
            document.Load(Path.Combine(baFolderPath, BurnCommon.BundleExtensionDataFileName));
            return document;
        }

        /// <summary>
        /// Loads an XmlDocument with the BootstrapperApplicationData.xml from the given folder that contains the contents of the BA container.
        /// </summary>
        /// <param name="baFolderPath"></param>
        /// <returns></returns>
        public static XmlDocument LoadBurnManifest(string baFolderPath)
        {
            var document = new XmlDocument();
            document.Load(Path.Combine(baFolderPath, "manifest.xml"));
            return document;
        }
    }
}
