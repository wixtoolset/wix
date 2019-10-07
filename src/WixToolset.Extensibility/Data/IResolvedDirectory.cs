// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility.Data
{
    /// <summary>
    /// Used for resolved directory information.
    /// </summary>
    public interface IResolvedDirectory
    {
        /// <summary>The directory parent.</summary>
        string DirectoryParent { get; set; }

        /// <summary>The name of this directory.</summary>
        string Name { get; set; }

        /// <summary>The path of this directory.</summary>
        string Path { get; set; }
    }
}
