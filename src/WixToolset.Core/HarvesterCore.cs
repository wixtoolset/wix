// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// The WiX Toolset harvester core.
    /// </summary>
    public class HarvesterCore : IHarvesterCore
    {
        public IMessaging Messaging { get; set; }

        /// <summary>
        /// Gets or sets the value of the extension argument passed to heat.
        /// </summary>
        /// <value>The extension argument.</value>
        public string ExtensionArgument { get; set; }

        /// <summary>
        /// Gets or sets the value of the root directory that is being harvested.
        /// </summary>
        /// <value>The root directory being harvested.</value>
        public string RootDirectory { get; set; }

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
        /// Resolves a file's path if the Wix.File.Source value starts with "SourceDir\".
        /// </summary>
        /// <param name="fileSource">The Wix.File.Source value with "SourceDir\".</param>
        /// <returns>The full path of the file.</returns>
        public string ResolveFilePath(string fileSource)
        {
            if (fileSource.StartsWith("SourceDir\\", StringComparison.Ordinal))
            {
                string file = Path.GetFullPath(this.RootDirectory);
                if (File.Exists(file))
                {
                    return file;
                }
                else
                {
                    fileSource = fileSource.Substring(10);
                    fileSource = Path.Combine(Path.GetFullPath(this.RootDirectory), fileSource);
                }
            }

            return fileSource;
        }
    }
}
