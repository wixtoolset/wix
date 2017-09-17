// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections;
    using WixToolset.Extensibility;
    using Wix = WixToolset.Data.Serialize;

    /// <summary>
    /// The WiX Toolset mutator.
    /// </summary>
    public sealed class Mutator
    {
        private SortedList extensions;
        private string extensionArgument;

        /// <summary>
        /// Instantiate a new mutator.
        /// </summary>
        public Mutator()
        {
            this.extensions = new SortedList();
        }

        /// <summary>
        /// Gets or sets the harvester core for the extension.
        /// </summary>
        /// <value>The harvester core for the extension.</value>
        public IHarvesterCore Core { get; set; }

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
        /// Adds a mutator extension.
        /// </summary>
        /// <param name="mutatorExtension">The mutator extension to add.</param>
        public void AddExtension(MutatorExtension mutatorExtension)
        {
            this.extensions.Add(mutatorExtension.Sequence, mutatorExtension);
        }

        /// <summary>
        /// Mutate a WiX document.
        /// </summary>
        /// <param name="wix">The Wix document element.</param>
        /// <returns>true if mutation was successful</returns>
        public bool Mutate(Wix.Wix wix)
        {
            bool encounteredError = false;
            
            try
            {
                foreach (MutatorExtension mutatorExtension in this.extensions.Values)
                {
                    if (null == mutatorExtension.Core)
                    {
                        mutatorExtension.Core = this.Core;
                    }

                    mutatorExtension.Mutate(wix);
                }
            }
            finally
            {
                encounteredError = this.Core.EncounteredError;
            }

            // return the Wix document element only if mutation completed successfully
            return !encounteredError;
        }

        /// <summary>
        /// Mutate a WiX document.
        /// </summary>
        /// <param name="wixString">The Wix document as a string.</param>
        /// <returns>The mutated Wix document as a string if mutation was successful, else null.</returns>
        public string Mutate(string wixString)
        {
            bool encounteredError = false;

            try
            {
                foreach (MutatorExtension mutatorExtension in this.extensions.Values)
                {
                    if (null == mutatorExtension.Core)
                    {
                        mutatorExtension.Core = this.Core;
                    }

                    wixString = mutatorExtension.Mutate(wixString);

                    if (String.IsNullOrEmpty(wixString) || this.Core.EncounteredError)
                    {
                        break;
                    }
                }
            }
            finally
            {
                encounteredError = this.Core.EncounteredError;
            }

            return encounteredError ? null : wixString;
        }
    }
}
