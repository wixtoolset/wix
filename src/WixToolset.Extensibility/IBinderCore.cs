// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;

    public interface IBinderCore : IMessageHandler
    {
        /// <summary>
        /// Gets or sets the file manager core for the extension.
        /// </summary>
        /// <value>File manager core for the extension.</value>
        IBinderFileManagerCore FileManagerCore { get; set; }

        /// <summary>
        /// Gets whether the binder core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        bool EncounteredError { get; }

        /// <summary>
        /// Gets the table definitions used by the Binder.
        /// </summary>
        /// <value>Table definitions used by the binder.</value>
        TableDefinitionCollection TableDefinitions { get; }

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        string CreateIdentifier(string prefix, params string[] args);
    }
}
