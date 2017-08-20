// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Rows;

    public interface IBinderFileManager
    {
        IBinderFileManagerCore Core { set; }

        ResolvedCabinet ResolveCabinet(string cabinetPath, IEnumerable<BindFileWithPath> files);

        string ResolveFile(string source, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage);

        string ResolveRelatedFile(string source, string relatedSource, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage);

        string ResolveMedia(MediaRow mediaRow, string mediaLayoutDirectory, string layoutDirectory);

        string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName);

        bool? CompareFiles(string targetFile, string updatedFile);

        bool CopyFile(string source, string destination, bool overwrite);

        bool MoveFile(string source, string destination, bool overwrite);
    }
}
