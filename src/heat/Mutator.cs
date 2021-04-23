// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using System;
    using System.Collections;
    using WixToolset.Harvesters.Extensibility;
    using Wix = WixToolset.Harvesters.Serialize;

    /// <summary>
    /// The WiX Toolset mutator.
    /// </summary>
    internal class Mutator : IMutator
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

        public IHarvesterCore Core { get; set; }

        public string ExtensionArgument
        {
            get { return this.extensionArgument; }
            set { this.extensionArgument = value; }
        }

        public void AddExtension(IMutatorExtension mutatorExtension)
        {
            this.extensions.Add(mutatorExtension.Sequence, mutatorExtension);
        }

        public bool Mutate(Wix.Wix wix)
        {
            bool encounteredError = false;
            
            try
            {
                foreach (IMutatorExtension mutatorExtension in this.extensions.Values)
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
                encounteredError = this.Core.Messaging.EncounteredError;
            }

            // return the Wix document element only if mutation completed successfully
            return !encounteredError;
        }

        public string Mutate(string wixString)
        {
            bool encounteredError = false;

            try
            {
                foreach (IMutatorExtension mutatorExtension in this.extensions.Values)
                {
                    if (null == mutatorExtension.Core)
                    {
                        mutatorExtension.Core = this.Core;
                    }

                    wixString = mutatorExtension.Mutate(wixString);

                    if (String.IsNullOrEmpty(wixString) || this.Core.Messaging.EncounteredError)
                    {
                        break;
                    }
                }
            }
            finally
            {
                encounteredError = this.Core.Messaging.EncounteredError;
            }

            return encounteredError ? null : wixString;
        }
    }
}
