// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// Core class for the binder.
    /// </summary>
    internal class BinderCore : IBinderCore
    {
        /// <summary>
        /// Constructor for binder core.
        /// </summary>
        internal BinderCore()
        {
            this.TableDefinitions = new TableDefinitionCollection(WindowsInstallerStandard.GetTableDefinitions());
        }

        public IBinderFileManagerCore FileManagerCore { get; set; }

        /// <summary>
        /// Gets whether the binder core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return Messaging.Instance.EncounteredError; }
        }

        /// <summary>
        /// Gets the table definitions used by the Binder.
        /// </summary>
        /// <value>Table definitions used by the binder.</value>
        public TableDefinitionCollection TableDefinitions { get; private set; }

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        public string CreateIdentifier(string prefix, params string[] args)
        {
            return Common.GenerateIdentifier(prefix, args);
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            Messaging.Instance.OnMessage(e);
        }
    }
}
