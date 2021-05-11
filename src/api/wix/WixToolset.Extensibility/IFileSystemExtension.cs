// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// Interface all file system extensions implement.
    /// </summary>
    public interface IFileSystemExtension
    {
#pragma warning disable 1591 // TODO: add documentation
        void Initialize(IFileSystemContext context);

        bool? CompareFiles(string targetFile, string updatedFile);
    }
}
