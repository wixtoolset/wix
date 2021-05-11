// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using System.IO;
    using WixToolset.Data;

    /// <summary>
    /// Interface for inspector extensions.
    /// </summary>
    /// <remarks>
    /// The inspector methods are stateless, but extensions are loaded once. If you want to maintain state, you should check
    /// if your data is loaded for each method and, if not, load it.
    /// </remarks>
    public interface IInspectorExtension
    {
        /// <summary>
        /// Gets or sets the <see cref="IInspectorCore"/> for inspector extensions to use.
        /// </summary>
        IInspectorCore Core { get; set; }

        /// <summary>
        /// Inspect the source before preprocessing.
        /// </summary>
        /// <param name="source">The source to preprocess.</param>
        void InspectSource(Stream source);

        /// <summary>
        /// Inspect the compiled output.
        /// </summary>
        /// <param name="intermediate">The compiled output.</param>
        void InspectIntermediate(Intermediate intermediate);

#if REWRITE
        /// <summary>
        /// Inspect the output.
        /// </summary>
        /// <param name="output">The output. May be called after linking or binding.</param>
        /// <remarks>
        /// To inspect a patch's filtered transforms, enumerate <see cref="Output.SubStorages"/>.
        /// Transforms where the <see cref="SubStorage.Name"/> begins with "#" are
        /// called patch transforms and instruct Windows Installer how to apply the
        /// authored transforms - those that do not begin with "#". The authored
        /// transforms are the primary transforms you'll typically want to inspect
        /// and contain your changes to target products.
        /// </remarks>
#endif
        /// <summary />
        void InspectOutput(Intermediate output);

        /// <summary>
        /// Inspect the final output after binding.
        /// </summary>
        /// <param name="filePath">The file path to the final bound output.</param>
        /// <param name="pdb">The <see cref="Intermediate"/> that contains source line numbers
        /// for the database and all rows.</param>
        void InspectDatabase(string filePath, Intermediate pdb);
    }
}
