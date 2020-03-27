// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.TestPackage
{
    using System.IO;
    using System.Xml;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Extensibility.Services;

    public class BundleExtractor
    {
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

        public static XmlNamespaceManager GetBADataNamespaceManager(XmlDocument document, string prefix)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace(prefix, BurnCommon.BADataNamespace);
            return namespaceManager;
        }

        public static XmlNamespaceManager GetBundleExtensionDataNamespaceManager(XmlDocument document, string prefix)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace(prefix, BurnCommon.BundleExtensionDataNamespace);
            return namespaceManager;
        }

        public static XmlNamespaceManager GetBurnNamespaceManager(XmlDocument document, string prefix)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace(prefix, BurnCommon.BurnNamespace);
            return namespaceManager;
        }

        public static XmlDocument LoadBAData(string baFolderPath)
        {
            var document = new XmlDocument();
            document.Load(Path.Combine(baFolderPath, BurnCommon.BADataFileName));
            return document;
        }

        public static XmlDocument LoadBundleExtensionData(string baFolderPath)
        {
            var document = new XmlDocument();
            document.Load(Path.Combine(baFolderPath, BurnCommon.BundleExtensionDataFileName));
            return document;
        }

        public static XmlDocument LoadBurnManifest(string baFolderPath)
        {
            var document = new XmlDocument();
            document.Load(Path.Combine(baFolderPath, "manifest.xml"));
            return document;
        }
    }
}
