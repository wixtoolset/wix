// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters.Extensibility
{
    using Wix = WixToolset.Harvesters.Serialize;

    /// <summary>
    /// Interface for a mutator.
    /// </summary>
    public interface IMutator
    {
        /// <summary>
        /// Gets or sets the harvester core for the extension.
        /// </summary>
        /// <value>The harvester core for the extension.</value>
        IHarvesterCore Core { get; }

        /// <summary>
        /// Gets or sets the value of the extension argument passed to heat.
        /// </summary>
        /// <value>The extension argument.</value>
        string ExtensionArgument { get; }

        /// <summary>
        /// Adds a mutator extension.
        /// </summary>
        /// <param name="mutatorExtension">The mutator extension to add.</param>
        void AddExtension(IMutatorExtension mutatorExtension);

        /// <summary>
        /// Mutate a WiX document.
        /// </summary>
        /// <param name="wix">The Wix document element.</param>
        /// <returns>true if mutation was successful</returns>
        bool Mutate(Wix.Wix wix);

        /// <summary>
        /// Mutate a WiX document.
        /// </summary>
        /// <param name="wixString">The Wix document as a string.</param>
        /// <returns>The mutated Wix document as a string if mutation was successful, else null.</returns>
        string Mutate(string wixString);
    }
}
