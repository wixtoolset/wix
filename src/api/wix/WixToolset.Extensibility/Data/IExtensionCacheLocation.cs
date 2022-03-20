// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    /// <summary>
    /// Extension cache location scope.
    /// </summary>
    public enum ExtensionCacheLocationScope
    {
        /// <summary>
        /// Project extension cache location.
        /// </summary>
        Project,

        /// <summary>
        /// User extension cache location.
        /// </summary>
        User,

        /// <summary>
        /// Machine extension cache location.
        /// </summary>
        Machine,
    }

    /// <summary>
    /// Location where extensions may be cached.
    /// </summary>
    public interface IExtensionCacheLocation
    {
        /// <summary>
        /// Path for  the extension cache location.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Scope for the extension cache location.
        /// </summary>
        ExtensionCacheLocationScope Scope { get; }
    }
}
