// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperCore
{
    using System;
    using System.IO;
    using System.Xml.XPath;

    public class BootstrapperApplicationData : IBootstrapperApplicationData
    {
        public const string DefaultFileName = "BootstrapperApplicationData.xml";
        public const string XMLNamespace = "http://wixtoolset.org/schemas/v4/2010/BootstrapperApplicationData";

        public static readonly DirectoryInfo DefaultFolder;
        public static readonly FileInfo DefaultFile;

        static BootstrapperApplicationData()
        {
            DefaultFolder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            DefaultFile = new FileInfo(Path.Combine(DefaultFolder.FullName, DefaultFileName));
        }

        public FileInfo BADataFile { get; private set; }

        public IBundleInfo Bundle { get; private set; }

        public BootstrapperApplicationData() : this(DefaultFile) { }

        public BootstrapperApplicationData(FileInfo baDataFile)
        {
            this.BADataFile = baDataFile;

            using (FileStream fs = this.BADataFile.OpenRead())
            {
                this.Bundle = BundleInfo.ParseBundleFromStream(fs);
            }
        }

        public static string GetAttribute(XPathNavigator node, string attributeName)
        {
            XPathNavigator attribute = node.SelectSingleNode("@" + attributeName);

            if (attribute == null)
            {
                return null;
            }

            return attribute.Value;
        }

        public static bool? GetYesNoAttribute(XPathNavigator node, string attributeName)
        {
            string attributeValue = GetAttribute(node, attributeName);

            if (attributeValue == null)
            {
                return null;
            }

            return attributeValue.Equals("yes", StringComparison.InvariantCulture);
        }
    }
}
