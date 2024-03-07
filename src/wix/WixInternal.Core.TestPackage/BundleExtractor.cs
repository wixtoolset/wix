// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixInternal.Core.TestPackage
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using WixToolset.Data.Burn;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Class to extract bundle contents for testing.
    /// </summary>
    public class BundleExtractor
    {
        private const string BurnNamespace = "http://wixtoolset.org/schemas/v4/2008/Burn";
        private const string BADataFileName = "BootstrapperApplicationData.xml";
        private const string BootstrapperExtensionDataFileName = "BootstrapperExtensionData.xml";

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
            return ExtractAllContainers(messaging, bundleFilePath, destinationFolderPath, null, tempFolderPath);
        }

        /// <summary>
        /// Extracts the BA container.
        /// </summary>
        /// <param name="messaging"></param>
        /// <param name="bundleFilePath">Path to the bundle.</param>
        /// <param name="baFolderPath">Path to extract BA to.</param>
        /// <param name="otherContainersFolderPath">Optional path to extract other attached containers to.</param>
        /// <param name="tempFolderPath">Temp path for extraction.</param>
        /// <returns></returns>
        public static ExtractBAContainerResult ExtractAllContainers(IMessaging messaging, string bundleFilePath, string baFolderPath, string otherContainersFolderPath, string tempFolderPath)
        {
            Directory.CreateDirectory(tempFolderPath);

            var args = new List<string>
            {
                "burn", "extract",
                "-intermediatefolder", tempFolderPath,
                bundleFilePath,
                "-oba", baFolderPath
            };

            if (!String.IsNullOrEmpty(otherContainersFolderPath))
            {
                args.Add("-o");
                args.Add(otherContainersFolderPath);
            }

            var runnerResult = WixRunner.Execute(args.ToArray());

            var result = new ExtractBAContainerResult();

            if (runnerResult.ExitCode == 0)
            {
                result.Success = true;
                result.ManifestDocument = LoadBurnManifest(baFolderPath);
                result.ManifestNamespaceManager = GetBurnNamespaceManager(result.ManifestDocument, "burn");

                result.BADataDocument = LoadBAData(baFolderPath);
                result.BADataNamespaceManager = GetBADataNamespaceManager(result.BADataDocument, "ba");

                result.BootstrapperExtensionDataDocument = LoadBootstrapperExtensionData(baFolderPath);
                result.BootstrapperExtensionDataNamespaceManager = GetBootstrapperExtensionDataNamespaceManager(result.BootstrapperExtensionDataDocument, "be");
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
            namespaceManager.AddNamespace(prefix, BurnConstants.BootstrapperApplicationDataNamespace);
            return namespaceManager;
        }

        /// <summary>
        /// Gets an <see cref="XmlNamespaceManager"/> for BootstrapperExtensionData.xml with the given prefix assigned to the root namespace.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static XmlNamespaceManager GetBootstrapperExtensionDataNamespaceManager(XmlDocument document, string prefix)
        {
            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace(prefix, BurnConstants.BootstrapperExtensionDataNamespace);
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
            namespaceManager.AddNamespace(prefix, BurnNamespace);
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
            document.Load(Path.Combine(baFolderPath, BADataFileName));
            return document;
        }

        /// <summary>
        /// Loads an XmlDocument with the BootstrapperApplicationData.xml from the given folder that contains the contents of the BA container.
        /// </summary>
        /// <param name="baFolderPath"></param>
        /// <returns></returns>
        public static XmlDocument LoadBootstrapperExtensionData(string baFolderPath)
        {
            var document = new XmlDocument();
            document.Load(Path.Combine(baFolderPath, BootstrapperExtensionDataFileName));
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
