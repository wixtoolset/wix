// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System;
    using System.IO;
    using WixToolset.Data;

    /// <summary>
    /// Opitonal base class for inspector extensions.
    /// </summary>
    public class InspectorExtension : IInspectorExtension
    {
        /// <summary>
        /// Gets the <see cref="InspectorCore"/> for inspector extensions to use.
        /// </summary>
        public IInspectorCore Core { get; set; }

        /// <summary>
        /// Inspect the source before preprocessing.
        /// </summary>
        /// <param name="source">The source to preprocess.</param>
        public virtual void InspectSource(Stream source)
        {
        }

        /// <summary>
        /// Inspect the compiled output.
        /// </summary>
        /// <param name="intermediate">The compiled output.</param>
        public virtual void InspectIntermediate(Intermediate intermediate)
        {
        }

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
        public virtual void InspectOutput(Output output)
        {
        }

        /// <summary>
        /// Inspect the final output after binding.
        /// </summary>
        /// <param name="filePath">The file path to the final bound output.</param>
        /// <param name="pdb">The <see cref="Pdb"/> that contains source line numbers
        /// for the database and all rows.</param>
        public virtual void InspectDatabase(string filePath, Pdb pdb)
        {
        }
    }
}
