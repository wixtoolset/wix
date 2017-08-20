// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    /// <summary>
    /// Bind stage of a file.. The reason we need this is to change the ResolveFile behavior based on if
    /// dynamic bindpath plugin is desirable. We cannot change the signature of ResolveFile since it might
    /// break existing implementers which derived from BinderFileManager
    /// </summary>
    public enum BindStage
    {
        /// <summary>
        /// Normal binding
        /// </summary>
        Normal,

        /// <summary>
        /// Bind the file path of the target build file
        /// </summary>
        Target,

        /// <summary>
        /// Bind the file path of the updated build file
        /// </summary>
        Updated,
    }
}
