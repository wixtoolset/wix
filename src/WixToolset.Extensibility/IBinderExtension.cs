// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensibility
{
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Interface all binder extensions implement.
    /// </summary>
    public interface IBinderExtension
    {
        /// <summary>
        /// Called before binding occurs.
        /// </summary>
        void PreBind(IBindContext context);

        /// <summary>
        /// Called after variable resolution occurs.
        /// </summary>
        void AfterResolvedFields(Intermediate intermediate);

        string ResolveFile(string source, string type, SourceLineNumber sourceLineNumbers, BindStage bindStage);

        bool? CompareFiles(string targetFile, string updatedFile);

        bool CopyFile(string source, string destination, bool overwrite);

        bool MoveFile(string source, string destination, bool overwrite);

        /// <summary>
        /// Called after all output changes occur and right before the output is bound into its final format.
        /// </summary>
        void PostBind(BindResult result);
    }
}
