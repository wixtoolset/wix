// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    /// <summary>
    /// Overridable variable from the BA manifest.
    /// </summary>
    public interface IOverridableVariableInfo
    {
        /// <summary>
        /// The Variable name.
        /// </summary>
        string Name { get; }
    }
}