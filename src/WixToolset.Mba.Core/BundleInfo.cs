// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;
    using System.Xml.XPath;

    public class BundleInfo : IBundleInfo
    {
        public bool PerMachine { get; internal set; }
        public string Name { get; internal set; }
        public string LogVariable { get; internal set; }
        public IDictionary<string, IPackageInfo> Packages { get; internal set; }

        internal BundleInfo()
        {
            this.Packages = new Dictionary<string, IPackageInfo>();
        }

        public void AddRelatedBundleAsPackage(DetectRelatedBundleEventArgs e)
        {
            var package = PackageInfo.GetRelatedBundleAsPackage(e.ProductCode, e.RelationType, e.PerMachine, e.Version);
            this.Packages.Add(package.Id, package);
        }

        public static IBundleInfo ParseBundleFromStream(Stream stream)
        {
            XPathDocument manifest = new XPathDocument(stream);
            XPathNavigator root = manifest.CreateNavigator();
            return ParseBundleFromXml(root);
        }

        public static IBundleInfo ParseBundleFromXml(XPathNavigator root)
        {
            BundleInfo bundle = new BundleInfo();

            XmlNamespaceManager namespaceManager = new XmlNamespaceManager(root.NameTable);
            namespaceManager.AddNamespace("p", BootstrapperApplicationData.XMLNamespace);
            XPathNavigator bundleNode = root.SelectSingleNode("/p:BootstrapperApplicationData/p:WixBundleProperties", namespaceManager);

            if (bundleNode == null)
            {
                throw new Exception("Failed to select bundle information.");
            }

            bool? perMachine = BootstrapperApplicationData.GetYesNoAttribute(bundleNode, "PerMachine");
            if (perMachine.HasValue)
            {
                bundle.PerMachine = perMachine.Value;
            }

            bundle.Name = BootstrapperApplicationData.GetAttribute(bundleNode, "DisplayName");

            bundle.LogVariable = BootstrapperApplicationData.GetAttribute(bundleNode, "LogPathVariable");

            foreach (var package in PackageInfo.ParsePackagesFromXml(root))
            {
                bundle.Packages.Add(package.Id, package);
            }

            return bundle;
        }
    }
}
