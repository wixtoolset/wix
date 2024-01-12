// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    using WixToolset.Data;

    /// <summary>
    /// Interface used to by extensions to define a component key path or
    /// (non-intuitively) the executable payload for a the bootstrapper application.
    /// </summary>
    public interface IComponentKeyPath
    {
        /// <summary>
        /// Indicates whether the key path was specified explicitly.
        /// </summary>
        bool Explicit { get; set; }

        /// <summary>
        /// Gets or sets the key path or executable payload identifier.
        /// </summary>
        Identifier Id { get; set; }

        /// <summary>
        /// Gets or sets the key path type for the component or if the
        /// executable payload for a bootstrapper application is provided
        /// as a File.
        /// </summary>
        PossibleKeyPathType Type { get; set; }
    }
}
