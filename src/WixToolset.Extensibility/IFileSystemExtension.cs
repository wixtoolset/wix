// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    /// <summary>
    /// Interface all file system extensions implement.
    /// </summary>
    public interface IFileSystemExtension
    {
        void Initialize(IFileSystemContext context);

        bool? CompareFiles(string targetFile, string updatedFile);
    }
}
