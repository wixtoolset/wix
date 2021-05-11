// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.IO;
    using System.Xml.XPath;

    /// <summary>
    /// Utility class for reading BootstrapperApplicationData.xml.
    /// </summary>
    public class BootstrapperApplicationData : IBootstrapperApplicationData
    {
        /// <summary>
        /// 
        /// </summary>
        public const string DefaultFileName = "BootstrapperApplicationData.xml";

        /// <summary>
        /// 
        /// </summary>
        public const string XMLNamespace = "http://wixtoolset.org/schemas/v4/BootstrapperApplicationData";

        /// <summary>
        /// The default path of where the BA was extracted to.
        /// </summary>
        public static readonly DirectoryInfo DefaultFolder;

        /// <summary>
        /// The default path to BootstrapperApplicationData.xml.
        /// </summary>
        public static readonly FileInfo DefaultFile;

        static BootstrapperApplicationData()
        {
            DefaultFolder = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            DefaultFile = new FileInfo(Path.Combine(DefaultFolder.FullName, DefaultFileName));
        }

        /// <inheritdoc/>
        public FileInfo BADataFile { get; private set; }

        /// <inheritdoc/>
        public IBundleInfo Bundle { get; private set; }

        /// <summary>
        /// Uses the default location for BootstrapperApplicationData.xml.
        /// </summary>
        public BootstrapperApplicationData() : this(DefaultFile) { }

        /// <summary>
        /// Uses the given file for BootstrapperApplicationData.xml.
        /// </summary>
        /// <param name="baDataFile"></param>
        public BootstrapperApplicationData(FileInfo baDataFile)
        {
            this.BADataFile = baDataFile;

            using (FileStream fs = this.BADataFile.OpenRead())
            {
                this.Bundle = BundleInfo.ParseBundleFromStream(fs);
            }
        }

        /// <summary>
        /// Utility method for parsing BootstrapperApplicationData.xml.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
        public static string GetAttribute(XPathNavigator node, string attributeName)
        {
            XPathNavigator attribute = node.SelectSingleNode("@" + attributeName);

            if (attribute == null)
            {
                return null;
            }

            return attribute.Value;
        }

        /// <summary>
        /// Utility method for parsing BootstrapperApplicationData.xml.
        /// </summary>
        /// <param name="node"></param>
        /// <param name="attributeName"></param>
        /// <returns></returns>
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
