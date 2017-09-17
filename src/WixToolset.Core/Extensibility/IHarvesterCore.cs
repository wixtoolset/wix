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
    public interface IHarvesterCore
    {
        /// <summary>
        /// Gets whether the harvester core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        bool EncounteredError { get; }

        /// <summary>
        /// Gets or sets the value of the extension argument passed to heat.
        /// </summary>
        /// <value>The extension argument.</value>
        string ExtensionArgument { get; set; }

        /// <summary>
        /// Gets or sets the value of the root directory that is being harvested.
        /// </summary>
        /// <value>The root directory being harvested.</value>
        string RootDirectory { get; set; }

        /// <summary>
        /// Create an identifier based on passed file name
        /// </summary>
        /// <param name="name">File name to generate identifer from</param>
        /// <returns></returns>
        string CreateIdentifierFromFilename(string filename);

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        string GenerateIdentifier(string prefix, params string[] args);

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        void OnMessage(MessageEventArgs mea);

        /// <summary>
        /// Resolves a file's path if the Wix.File.Source value starts with "SourceDir\".
        /// </summary>
        /// <param name="fileSource">The Wix.File.Source value with "SourceDir\".</param>
        /// <returns>The full path of the file.</returns>
        string ResolveFilePath(string fileSource);
    }
}
