// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using WixToolset.Data;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The WiX Toolset harvester core.
    /// </summary>
    public sealed class HarvesterCore : IHarvesterCore
    {
        private string extensionArgument;
        private string rootDirectory;

        /// <summary>
        /// Gets whether the harvester core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return Messaging.Instance.EncounteredError; }
        }

        /// <summary>
        /// Gets or sets the value of the extension argument passed to heat.
        /// </summary>
        /// <value>The extension argument.</value>
        public string ExtensionArgument
        {
            get { return this.extensionArgument; }
            set { this.extensionArgument = value; }
        }

        /// <summary>
        /// Gets or sets the value of the root directory that is being harvested.
        /// </summary>
        /// <value>The root directory being harvested.</value>
        public string RootDirectory
        {
            get { return this.rootDirectory; }
            set { this.rootDirectory = value; }
        }

        /// <summary>
        /// Create an identifier based on passed file name
        /// </summary>
        /// <param name="name">File name to generate identifer from</param>
        /// <returns></returns>
        public string CreateIdentifierFromFilename(string filename)
        {
            return Common.GetIdentifierFromName(filename);
        }

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        public string GenerateIdentifier(string prefix, params string[] args)
        {
            return Common.GenerateIdentifier(prefix, args);
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs mea)
        {
            Messaging.Instance.OnMessage(mea);
        }

        /// <summary>
        /// Resolves a file's path if the Wix.File.Source value starts with "SourceDir\".
        /// </summary>
        /// <param name="fileSource">The Wix.File.Source value with "SourceDir\".</param>
        /// <returns>The full path of the file.</returns>
        public string ResolveFilePath(string fileSource)
        {
            if (fileSource.StartsWith("SourceDir\\", StringComparison.Ordinal))
            {
                string file = Path.GetFullPath(this.rootDirectory);
                if (File.Exists(file))
                {
                    return file;
                }
                else
                {
                    fileSource = fileSource.Substring(10);
                    fileSource = Path.Combine(Path.GetFullPath(this.rootDirectory), fileSource);
                }
            }

            return fileSource;
        }
    }
}
